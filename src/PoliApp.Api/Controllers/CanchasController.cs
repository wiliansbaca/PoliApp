using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Controllers;

/// <summary>HU04: consulta pública de canchas y su disponibilidad.</summary>
[ApiController]
[Route("api/canchas")]
public class CanchasController(PoliAppDbContext db) : ControllerBase
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

        var ahora = DateTime.Now;
        return canchas.Select(c => new DisponibilidadDto(
            CanchaDto.De(c),
            c.BloquesHorarios().Select(h => new BloqueDto(
                h.ToString("HH:mm"),
                dia.ToDateTime(h) > ahora &&
                !reservas.Any(r => r.CanchaId == c.Id && r.SeCruzaCon(dia, h, h.AddHours(1))))).ToList()));
    }
}
