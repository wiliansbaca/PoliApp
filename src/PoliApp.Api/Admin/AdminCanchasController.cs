using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Api.Auth;
using PoliApp.Api.Services;
using PoliApp.Domain;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Admin;

/// <summary>HU02 y HU03: gestión de complejos, canchas, horarios y tarifas.</summary>
[ApiController]
[Authorize(Roles = nameof(Rol.Administrador))]
[Route("api/admin")]
public class AdminCanchasController(PoliAppDbContext db, CalendarioExternoService calendarios) : ControllerBase
{
    [HttpGet("complejos")]
    public async Task<IEnumerable<ComplejoDto>> Complejos()
    {
        var q = db.Complejos.AsNoTracking();
        if (User.ComplejoId() is int cid) q = q.Where(c => c.Id == cid);
        return await q.OrderBy(c => c.Nombre)
            .Select(c => new ComplejoDto(c.Id, c.Nombre, c.Direccion, c.Distrito, c.Telefono, c.Canchas.Count))
            .ToListAsync();
    }

    [HttpPut("complejos/{id:int}")]
    public async Task<ActionResult<ComplejoDto>> EditarComplejo(int id, GuardarComplejoDto dto)
    {
        VerificarComplejo(id);
        var c = await db.Complejos.Include(x => x.Canchas).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new DomainException("El complejo no existe.");
        c.Nombre = dto.Nombre.Trim();
        c.Direccion = dto.Direccion.Trim();
        c.Distrito = dto.Distrito.Trim();
        c.Telefono = dto.Telefono?.Trim();
        await db.SaveChangesAsync();
        return new ComplejoDto(c.Id, c.Nombre, c.Direccion, c.Distrito, c.Telefono, c.Canchas.Count);
    }

    [HttpGet("canchas")]
    public async Task<IEnumerable<CanchaDto>> Canchas()
    {
        var q = db.Canchas.AsNoTracking().Include(c => c.Complejo).AsQueryable();
        if (User.ComplejoId() is int cid) q = q.Where(c => c.ComplejoId == cid);
        var lista = await q.OrderBy(c => c.Nombre).ToListAsync();
        return lista.Select(CanchaDto.De);
    }

    [HttpPost("canchas")]
    public async Task<ActionResult<CanchaDto>> CrearCancha(GuardarCanchaDto dto)
    {
        VerificarComplejo(dto.ComplejoId);
        var cancha = new Cancha { ComplejoId = dto.ComplejoId, Nombre = dto.Nombre.Trim(), Tipo = dto.Tipo.Trim(), Activa = dto.Activa };
        cancha.ConfigurarHorario(Formatos.Hora(dto.HoraApertura), Formatos.Hora(dto.HoraCierre), dto.PrecioHora);
        db.Canchas.Add(cancha);
        await db.SaveChangesAsync();
        await db.Entry(cancha).Reference(c => c.Complejo).LoadAsync();
        return CanchaDto.De(cancha);
    }

    [HttpPut("canchas/{id:int}")]
    public async Task<ActionResult<CanchaDto>> EditarCancha(int id, GuardarCanchaDto dto)
    {
        var c = await ObtenerCancha(id);
        c.Nombre = dto.Nombre.Trim();
        c.Tipo = dto.Tipo.Trim();
        c.Activa = dto.Activa;
        c.ConfigurarHorario(Formatos.Hora(dto.HoraApertura), Formatos.Hora(dto.HoraCierre), dto.PrecioHora);
        await db.SaveChangesAsync();
        return CanchaDto.De(c);
    }

    [HttpPut("canchas/{id:int}/horario")]
    public async Task<ActionResult<CanchaDto>> ConfigurarHorario(int id, HorarioTarifaDto dto)
    {
        var c = await ObtenerCancha(id);
        c.ConfigurarHorario(Formatos.Hora(dto.HoraApertura), Formatos.Hora(dto.HoraCierre), dto.PrecioHora);
        await db.SaveChangesAsync();
        return CanchaDto.De(c);
    }

    /// <summary>Datos de la integración con Google Calendar de una cancha (genera el enlace .ics la primera vez).</summary>
    [HttpGet("canchas/{id:int}/calendario")]
    public async Task<CalendarioCanchaDto> Calendario(int id)
    {
        var c = await ObtenerCancha(id);
        if (c.CalendarioToken is null)
        {
            c.GenerarTokenCalendario();
            await db.SaveChangesAsync();
        }
        return CalendarioDto(c);
    }

    /// <summary>Vincula (o desvincula, si llega vacía) la dirección secreta iCal del calendario de bloqueos.</summary>
    [HttpPut("canchas/{id:int}/calendario")]
    public async Task<CalendarioCanchaDto> VincularCalendario(int id, VincularCalendarioDto dto)
    {
        var c = await ObtenerCancha(id);
        var url = string.IsNullOrWhiteSpace(dto.IcalOcupadosUrl) ? null : dto.IcalOcupadosUrl.Trim();
        if (url is not null) await calendarios.ValidarAsync(url);
        calendarios.Olvidar(c.ICalOcupadosUrl);
        c.VincularCalendario(url);
        await db.SaveChangesAsync();
        return CalendarioDto(c);
    }

    /// <summary>Genera un nuevo enlace .ics (el anterior deja de funcionar), por ejemplo si se compartió por error.</summary>
    [HttpPost("canchas/{id:int}/calendario/renovar-enlace")]
    public async Task<CalendarioCanchaDto> RenovarEnlace(int id)
    {
        var c = await ObtenerCancha(id);
        c.GenerarTokenCalendario();
        await db.SaveChangesAsync();
        return CalendarioDto(c);
    }

    private CalendarioCanchaDto CalendarioDto(Cancha c)
    {
        // Detrás de Azure App Service la API recibe HTTP interno; el enlace público siempre es HTTPS.
        var esquema = Request.Host.Host is "localhost" or "127.0.0.1" ? Request.Scheme : "https";
        var url = $"{esquema}://{Request.Host}/api/canchas/{c.Id}/calendario.ics?token={c.CalendarioToken}";
        return new CalendarioCanchaDto(c.Id, c.Nombre, c.ICalOcupadosUrl, url);
    }

    private async Task<Cancha> ObtenerCancha(int id)
    {
        var c = await db.Canchas.Include(x => x.Complejo).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new DomainException("La cancha no existe.");
        VerificarComplejo(c.ComplejoId);
        return c;
    }

    private void VerificarComplejo(int complejoId)
    {
        if (User.ComplejoId() is int cid && cid != complejoId)
            throw new DomainException("No puede administrar canchas de otro complejo.");
    }
}
