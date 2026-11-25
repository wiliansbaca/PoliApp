using Microsoft.Extensions.Caching.Memory;
using PoliApp.Domain;
using PoliApp.Domain.Calendario;

namespace PoliApp.Api.Services;

/// <summary>
/// Lee los calendarios de Google Calendar vinculados a las canchas (dirección secreta en formato iCal) para
/// conocer los horarios que el administrador bloqueó directamente en Google Calendar.
/// Las descargas se guardan 5 minutos en memoria y un fallo de Google nunca impide consultar o reservar.
/// </summary>
public class CalendarioExternoService(IHttpClientFactory http, IMemoryCache cache, ILogger<CalendarioExternoService> log)
{
    public const string ClienteHttp = "ical";
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(5);

    /// <summary>Horarios bloqueados en el calendario de la cancha para un día (hora de Lima).</summary>
    public async Task<IReadOnlyList<Intervalo>> OcupadosAsync(string? url, DateOnly dia, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return [];
        var ics = await DescargarAsync(url, ct);
        if (ics is null) return [];
        var desde = dia.ToDateTime(TimeOnly.MinValue);
        try
        {
            return ICalendario.LeerOcupados(ics, desde, desde.AddDays(1));
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo interpretar el calendario vinculado a una cancha.");
            return [];
        }
    }

    /// <summary>Comprueba que la dirección sea de Google Calendar y que se pueda leer antes de guardarla.</summary>
    public async Task ValidarAsync(string url, CancellationToken ct = default)
    {
        if (!ICalendario.EsDireccionPermitida(url))
            throw new DomainException("Pega la «dirección secreta en formato iCal» de Google Calendar (empieza con https://calendar.google.com y termina en .ics).");
        cache.Remove(Clave(url));
        if (await DescargarAsync(url, ct) is null)
            throw new DomainException("No se pudo leer ese calendario. Verifica que copiaste la dirección secreta en formato iCal completa.");
    }

    public void Olvidar(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url)) cache.Remove(Clave(url));
    }

    private async Task<string?> DescargarAsync(string url, CancellationToken ct)
    {
        if (cache.TryGetValue(Clave(url), out string? guardado)) return guardado;
        try
        {
            var texto = await http.CreateClient(ClienteHttp).GetStringAsync(url, ct);
            if (!texto.Contains("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase)) return null;
            cache.Set(Clave(url), texto, Vigencia);
            return texto;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            log.LogWarning(ex, "No se pudo descargar el calendario vinculado a una cancha.");
            return null;
        }
    }

    private static string Clave(string url) => "ical:" + url.Trim();
}
