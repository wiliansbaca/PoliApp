using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Api.Auth;
using PoliApp.Api.Services;
using PoliApp.Domain;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Controllers;

/// <summary>HU05, HU06 y HU11: reservas y pagos del deportista autenticado.</summary>
[ApiController]
[Authorize]
[Route("api/reservas")]
public class ReservasController(PoliAppDbContext db, CalendarioExternoService calendarios) : ControllerBase
{
    [HttpGet("mias")]
    public async Task<IEnumerable<ReservaDto>> MisReservas()
    {
        var uid = User.UsuarioId();
        var lista = await db.Reservas.AsNoTracking()
            .Include(r => r.Cancha!).ThenInclude(c => c.Complejo)
            .Include(r => r.Pagos)
            .Where(r => r.UsuarioId == uid)
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraInicio)
            .ToListAsync();
        var ahora = DateTime.Now;
        return lista.Select(r => ReservaDto.De(r, ahora));
    }

    [HttpPost]
    public async Task<ActionResult<ReservaDto>> Crear(CrearReservaDto dto)
    {
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId()) ?? throw new DomainException("Usuario no encontrado.");
        var cancha = await db.Canchas.Include(c => c.Complejo).FirstOrDefaultAsync(c => c.Id == dto.CanchaId && c.Activa)
                     ?? throw new DomainException("La cancha no existe o no está activa.");
        var (fecha, inicio, fin) = await ValidarHorario(cancha, dto.Fecha, dto.HoraInicio, dto.HoraFin, null);

        var reserva = new Reserva
        {
            Cancha = cancha,
            CanchaId = cancha.Id,
            UsuarioId = usuario.Id,
            ClienteNombre = usuario.NombreCompleto,
            ClienteTelefono = usuario.Celular,
            Fecha = fecha,
            HoraInicio = inicio,
            HoraFin = fin,
            MontoTotal = cancha.CalcularMonto(inicio, fin)
        };
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(MisReservas), ReservaDto.De(reserva, DateTime.Now));
    }

    [HttpPatch("{id:int}/cancelar")]
    public async Task<ActionResult<ReservaDto>> Cancelar(int id)
    {
        var r = await ObtenerPropia(id);
        r.Cancelar(DateTime.Now);
        await db.SaveChangesAsync();
        return ReservaDto.De(r, DateTime.Now);
    }

    [HttpPatch("{id:int}/reprogramar")]
    public async Task<ActionResult<ReservaDto>> Reprogramar(int id, ReprogramarDto dto)
    {
        var r = await ObtenerPropia(id);
        var (fecha, inicio, fin) = await ValidarHorario(r.Cancha!, dto.Fecha, dto.HoraInicio, dto.HoraFin, r.Id);
        r.Reprogramar(fecha, inicio, fin, r.Cancha!.CalcularMonto(inicio, fin), DateTime.Now);
        await db.SaveChangesAsync();
        return ReservaDto.De(r, DateTime.Now);
    }

    [HttpPost("{id:int}/pago")]
    public async Task<ActionResult<ReservaDto>> RegistrarPago(int id, RegistrarPagoDto dto)
    {
        var r = await ObtenerPropia(id);
        if (!r.EstaActiva) throw new DomainException("Solo se pueden pagar reservas pendientes o confirmadas.");
        if (r.Pagos.Any(p => p.Estado != EstadoPago.Observado))
            throw new DomainException("Esta reserva ya tiene un pago registrado.");
        r.Pagos.Add(new Pago { Metodo = dto.Metodo, NumeroOperacion = dto.NumeroOperacion, Monto = r.MontoTotal });
        await db.SaveChangesAsync();
        return ReservaDto.De(r, DateTime.Now);
    }

    private async Task<Reserva> ObtenerPropia(int id)
    {
        var uid = User.UsuarioId();
        return await db.Reservas
                   .Include(r => r.Cancha!).ThenInclude(c => c.Complejo)
                   .Include(r => r.Pagos)
                   .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == uid)
               ?? throw new DomainException("La reserva no existe.");
    }

    /// <summary>Regla de negocio de HU05: horario dentro de la atención, futuro y sin cruces.</summary>
    private async Task<(DateOnly, TimeOnly, TimeOnly)> ValidarHorario(Cancha cancha, string f, string hi, string hf, int? excluirId)
    {
        var fecha = Formatos.Fecha(f);
        var inicio = Formatos.Hora(hi);
        var fin = Formatos.Hora(hf);

        if (!cancha.AtiendeEn(inicio, fin))
            throw new DomainException($"La cancha atiende de {cancha.HoraApertura:HH:mm} a {cancha.HoraCierre:HH:mm}.");
        if (fecha.ToDateTime(inicio) <= DateTime.Now)
            throw new DomainException("No se puede reservar en un horario pasado.");

        var mismoDia = await db.Reservas.AsNoTracking()
            .Where(r => r.CanchaId == cancha.Id && r.Fecha == fecha && r.Id != (excluirId ?? 0))
            .ToListAsync();
        if (mismoDia.Any(r => r.SeCruzaCon(fecha, inicio, fin)))
            throw new DomainException("Ese horario ya está ocupado. Elige otro horario disponible.");

        var bloqueos = await calendarios.OcupadosAsync(cancha.ICalOcupadosUrl, fecha);
        if (bloqueos.Any(b => b.SeCruzaCon(fecha.ToDateTime(inicio), fecha.ToDateTime(fin))))
            throw new DomainException("El complejo bloqueó ese horario en su calendario. Elige otro horario disponible.");

        return (fecha, inicio, fin);
    }
}
