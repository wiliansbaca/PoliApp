using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Api.Auth;
using PoliApp.Api.Services;
using PoliApp.Domain;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Admin;

/// <summary>HU07, HU08 y HU11: panel del administrador para atender reservas y validar pagos.</summary>
[ApiController]
[Authorize(Roles = nameof(Rol.Administrador))]
[Route("api/admin")]
public class AdminReservasController(PoliAppDbContext db, NotificacionService notificaciones) : ControllerBase
{
    private IQueryable<Reserva> ReservasDelComplejo()
    {
        var q = db.Reservas.Include(r => r.Cancha!).ThenInclude(c => c.Complejo).Include(r => r.Pagos).AsQueryable();
        return User.ComplejoId() is int cid ? q.Where(r => r.Cancha!.ComplejoId == cid) : q;
    }

    [HttpGet("panel")]
    public async Task<PanelDto> Panel()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var hace7 = hoy.AddDays(-6);
        var q = ReservasDelComplejo().AsNoTracking();

        var reservasHoy = await q.CountAsync(r => r.Fecha == hoy && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada));
        var pendientes = await q.CountAsync(r => r.Estado == EstadoReserva.Pendiente && r.Fecha >= hoy);
        var pagos = await q.SelectMany(r => r.Pagos).CountAsync(p => p.Estado == EstadoPago.PorValidar);
        var ingresosMes = await q.Where(r => r.Fecha >= inicioMes && r.Fecha <= hoy &&
                                             (r.Estado == EstadoReserva.Confirmada || r.Estado == EstadoReserva.Finalizada))
                                 .SumAsync(r => (decimal?)r.MontoTotal) ?? 0;

        var semana = await q.Where(r => r.Fecha >= hace7 && r.Fecha <= hoy &&
                                        r.Estado != EstadoReserva.Cancelada && r.Estado != EstadoReserva.Rechazada).ToListAsync();
        var canchas = await CanchasDelComplejo().ToListAsync();
        var disponibles = canchas.Sum(c => (c.HoraCierre - c.HoraApertura).TotalHours) * 7;
        var ocupadas = semana.Sum(r => (r.HoraFin - r.HoraInicio).TotalHours);

        return new PanelDto(reservasHoy, pendientes, pagos, ingresosMes, disponibles > 0 ? Math.Round(ocupadas / disponibles * 100, 1) : 0);
    }

    [HttpGet("reservas")]
    public async Task<IEnumerable<ReservaDto>> Reservas([FromQuery] string? estado, [FromQuery] string? desde)
    {
        var q = ReservasDelComplejo().AsNoTracking();
        if (Enum.TryParse<EstadoReserva>(estado, true, out var e)) q = q.Where(r => r.Estado == e);
        var d = string.IsNullOrWhiteSpace(desde) ? DateOnly.FromDateTime(DateTime.Today) : Formatos.Fecha(desde);
        q = q.Where(r => r.Fecha >= d);
        var lista = await q.OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio).Take(200).ToListAsync();
        var ahora = DateTime.Now;
        return lista.Select(r => ReservaDto.De(r, ahora));
    }

    [HttpPatch("reservas/{id:int}/confirmar")]
    public async Task<ActionResult<ReservaDto>> Confirmar(int id)
    {
        var r = await ReservasDelComplejo().Include(x => x.Usuario).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new DomainException("La reserva no existe.");
        r.Confirmar();
        await db.SaveChangesAsync();
        if (r.Usuario is not null) await notificaciones.ReservaConfirmadaAsync(r, r.Usuario.Correo);
        return ReservaDto.De(r, DateTime.Now);
    }

    [HttpPatch("reservas/{id:int}/rechazar")]
    public async Task<ActionResult<ReservaDto>> Rechazar(int id, RechazarDto dto)
    {
        var r = await ReservasDelComplejo().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new DomainException("La reserva no existe.");
        r.Rechazar(dto.Motivo);
        await db.SaveChangesAsync();
        return ReservaDto.De(r, DateTime.Now);
    }

    [HttpGet("pagos")]
    public async Task<IEnumerable<PagoDto>> Pagos([FromQuery] string? estado)
    {
        var cid = User.ComplejoId();
        var q = db.Pagos.AsNoTracking().Include(p => p.Reserva!).ThenInclude(r => r.Cancha).AsQueryable();
        if (cid is int c) q = q.Where(p => p.Reserva!.Cancha!.ComplejoId == c);
        if (Enum.TryParse<EstadoPago>(estado, true, out var e)) q = q.Where(p => p.Estado == e);
        var lista = await q.OrderByDescending(p => p.FechaRegistro).Take(200).ToListAsync();
        return lista.Select(p => new PagoDto(p.Id, p.ReservaId, p.Reserva!.Codigo, p.Reserva.ClienteNombre, p.Reserva.Cancha!.Nombre,
            p.Reserva.Fecha.ToString("yyyy-MM-dd"), p.Metodo, p.NumeroOperacion, p.Monto, p.Estado.ToString(), p.FechaRegistro));
    }

    [HttpPatch("pagos/{id:int}/validar")]
    public async Task<IActionResult> ValidarPago(int id) => await RevisarPago(id, true);

    [HttpPatch("pagos/{id:int}/observar")]
    public async Task<IActionResult> ObservarPago(int id) => await RevisarPago(id, false);

    private async Task<IActionResult> RevisarPago(int id, bool validar)
    {
        var cid = User.ComplejoId();
        var p = await db.Pagos.Include(x => x.Reserva!).ThenInclude(r => r.Cancha!).ThenInclude(c => c.Complejo)
                    .Include(x => x.Reserva!).ThenInclude(r => r.Usuario)
                    .FirstOrDefaultAsync(x => x.Id == id && (cid == null || x.Reserva!.Cancha!.ComplejoId == cid))
                ?? throw new DomainException("El pago no existe.");
        var estabaPendiente = p.Reserva!.Estado == EstadoReserva.Pendiente;
        if (validar) p.Validar(); else p.Observar();
        await db.SaveChangesAsync();
        if (validar && estabaPendiente && p.Reserva.Usuario is not null)
            await notificaciones.ReservaConfirmadaAsync(p.Reserva, p.Reserva.Usuario.Correo);
        return NoContent();
    }

    private IQueryable<Cancha> CanchasDelComplejo()
    {
        var q = db.Canchas.AsNoTracking().Where(c => c.Activa);
        return User.ComplejoId() is int cid ? q.Where(c => c.ComplejoId == cid) : q;
    }
}
