using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Api.Services;
using PoliApp.Domain.Calendario;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Controllers;

/// <summary>HU04: consulta pública de canchas y su disponibilidad.</summary>
[ApiController]
[Route("api/canchas")]
public class CanchasController(PoliAppDbContext db, CalendarioExternoService calendarios) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CanchaDto>> Listar([FromQuery] string? tipo, [FromQuery] string? distrito)
    {
        var q = db.Canchas.AsNoTracking().Include(c => c.Complejo).Where(c => c.Activa);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(c => c.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(distrito)) q = q.Where(c => c.Complejo!.Distrito == distrito);
        var lista = await q.OrderBy(c => c.Complejo!.Nombre).ThenBy(c => c.Nombre).ToListAsync();
        return lista.Select(CanchaDto.De);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CanchaDto>> Obtener(int id)
    {
        var c = await db.Canchas.AsNoTracking().Include(x => x.Complejo).FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? NotFound() : CanchaDto.De(c);
    }

    [HttpGet("filtros")]
    public async Task<object> Filtros() => new
    {
        Tipos = await db.Canchas.Where(c => c.Activa).Select(c => c.Tipo).Distinct().OrderBy(t => t).ToListAsync(),
        Distritos = await db.Complejos.Select(c => c.Distrito).Distinct().OrderBy(d => d).ToListAsync()
    };

    /// <summary>Bloques de una hora libres y ocupados de cada cancha en una fecha.</summary>
    [HttpGet("disponibilidad")]
    public async Task<IEnumerable<DisponibilidadDto>> Disponibilidad(
        [FromQuery] string fecha, [FromQuery] string? tipo, [FromQuery] string? distrito, [FromQuery] int? canchaId)
    {
        var dia = Formatos.Fecha(fecha);
        var q = db.Canchas.AsNoTracking().Include(c => c.Complejo).Where(c => c.Activa);
        if (canchaId is int id) q = q.Where(c => c.Id == id);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(c => c.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(distrito)) q = q.Where(c => c.Complejo!.Distrito == distrito);
        var canchas = await q.OrderBy(c => c.Complejo!.Nombre).ThenBy(c => c.Nombre).ToListAsync();

        var ids = canchas.Select(c => c.Id).ToList();
        var reservas = await db.Reservas.AsNoTracking()
            .Where(r => ids.Contains(r.CanchaId) && r.Fecha == dia &&
                        (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .ToListAsync();

        // Horarios que el administrador bloqueó en el calendario de Google vinculado a cada cancha.
        var bloqueos = (await Task.WhenAll(canchas.Select(async c => (c.Id, Ocupados: await calendarios.OcupadosAsync(c.ICalOcupadosUrl, dia)))))
            .ToDictionary(x => x.Id, x => x.Ocupados);

        var ahora = DateTime.Now;
        return canchas.Select(c => new DisponibilidadDto(
            CanchaDto.De(c),
            c.BloquesHorarios().Select(h => new BloqueDto(
                h.ToString("HH:mm"),
                dia.ToDateTime(h) > ahora &&
                !reservas.Any(r => r.CanchaId == c.Id && r.SeCruzaCon(dia, h, h.AddHours(1))) &&
                !bloqueos[c.Id].Any(b => b.SeCruzaCon(dia.ToDateTime(h), dia.ToDateTime(h).AddHours(1))))).ToList()));
    }

    /// <summary>
    /// Calendario iCalendar (.ics) con las reservas de la cancha. El administrador lo agrega en Google Calendar
    /// («Otros calendarios → Desde URL») y ve sus reservas sin instalar nada. Lo protege un código secreto.
    /// </summary>
    [HttpGet("{id:int}/calendario.ics")]
    public async Task<IActionResult> CalendarioReservas(int id, [FromQuery] string? token)
    {
        var c = await db.Canchas.AsNoTracking().Include(x => x.Complejo).FirstOrDefaultAsync(x => x.Id == id);
        if (c?.CalendarioToken is null || string.IsNullOrEmpty(token) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(c.CalendarioToken)))
            return NotFound();

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var desde = hoy.AddDays(-30);
        var hasta = hoy.AddDays(120);
        var reservas = await db.Reservas.AsNoTracking()
            .Where(r => r.CanchaId == id && r.Fecha >= desde && r.Fecha <= hasta &&
                        (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio)
            .ToListAsync();

        var lugar = c.Complejo is null ? null : $"{c.Complejo.Nombre}, {c.Complejo.Direccion}, {c.Complejo.Distrito}, Cusco";
        var eventos = reservas.Select(r => new EventoCalendario(
            $"reserva-{r.Id}@poliapp.tech",
            r.Estado == EstadoReserva.Confirmada ? $"{r.ClienteNombre} ({r.Codigo})" : $"[Pendiente] {r.ClienteNombre} ({r.Codigo})",
            ICalendario.LimaAUtc(r.Fecha.ToDateTime(r.HoraInicio)),
            ICalendario.LimaAUtc(r.Fecha.ToDateTime(r.HoraFin)),
            $"Reserva {r.Codigo} en Poli-App\nEstado: {r.Estado}\nCliente: {r.ClienteNombre}\nCelular: {r.ClienteTelefono}\nMonto: S/ {r.MontoTotal:0.00}",
            lugar,
            r.Estado == EstadoReserva.Confirmada));

        var ics = ICalendario.GenerarFeed($"Poli-App · {c.Complejo?.Nombre} · {c.Nombre}", eventos, DateTime.UtcNow);
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8", $"poliapp-cancha-{c.Id}.ics");
    }
}
