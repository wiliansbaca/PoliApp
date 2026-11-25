using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Api.Auth;
using PoliApp.Domain;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Admin;

/// <summary>HU09: reportes de ocupación e ingresos por cancha.</summary>
[ApiController]
[Authorize(Roles = nameof(Rol.Administrador))]
[Route("api/admin/reportes")]
public class ReportesController(PoliAppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ReporteDto> Generar([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        var h = string.IsNullOrWhiteSpace(hasta) ? DateOnly.FromDateTime(DateTime.Today) : Formatos.Fecha(hasta);
        var d = string.IsNullOrWhiteSpace(desde) ? h.AddDays(-13) : Formatos.Fecha(desde);
        if (d > h) throw new DomainException("La fecha inicial no puede ser mayor que la final.");
        var dias = h.DayNumber - d.DayNumber + 1;

        var canchas = db.Canchas.AsNoTracking().Where(c => c.Activa);
        if (User.ComplejoId() is int cid) canchas = canchas.Where(c => c.ComplejoId == cid);
        var listaCanchas = await canchas.OrderBy(c => c.Nombre).ToListAsync();
        var ids = listaCanchas.Select(c => c.Id).ToList();

        var reservas = await db.Reservas.AsNoTracking()
            .Where(r => ids.Contains(r.CanchaId) && r.Fecha >= d && r.Fecha <= h &&
                        (r.Estado == EstadoReserva.Confirmada || r.Estado == EstadoReserva.Finalizada))
            .ToListAsync();

        var porCancha = listaCanchas.Select(c =>
        {
            var rs = reservas.Where(r => r.CanchaId == c.Id).ToList();
            var horas = rs.Sum(r => (r.HoraFin - r.HoraInicio).TotalHours);
            var disp = (c.HoraCierre - c.HoraApertura).TotalHours * dias;
            return new ReporteCanchaDto(c.Id, c.Nombre, rs.Count, horas, disp,
                disp > 0 ? Math.Round(horas / disp * 100, 1) : 0, rs.Sum(r => r.MontoTotal));
        }).ToList();

        var porDia = Enumerable.Range(0, dias).Select(i => d.AddDays(i)).Select(f =>
        {
            var rs = reservas.Where(r => r.Fecha == f).ToList();
            return new IngresoDiaDto(f.ToString("yyyy-MM-dd"), rs.Sum(r => r.MontoTotal), rs.Count);
        }).ToList();

        var totalDisp = porCancha.Sum(x => x.HorasDisponibles);
        return new ReporteDto(d.ToString("yyyy-MM-dd"), h.ToString("yyyy-MM-dd"), reservas.Count, reservas.Sum(r => r.MontoTotal),
            totalDisp > 0 ? Math.Round(porCancha.Sum(x => x.HorasReservadas) / totalDisp * 100, 1) : 0, porCancha, porDia);
    }
}
