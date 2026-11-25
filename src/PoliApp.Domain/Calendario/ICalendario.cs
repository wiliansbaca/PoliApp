using System.Globalization;
using System.Text;

namespace PoliApp.Domain.Calendario;

/// <summary>Intervalo ocupado en hora local de Lima.</summary>
public readonly record struct Intervalo(DateTime Inicio, DateTime Fin)
{
    public bool SeCruzaCon(DateTime inicio, DateTime fin) => Inicio < fin && Fin > inicio;
}

/// <summary>Evento que Poli-App publica en el calendario de una cancha (horas en UTC).</summary>
public record EventoCalendario(string Uid, string Titulo, DateTime InicioUtc, DateTime FinUtc,
    string? Descripcion, string? Lugar, bool Confirmado);

/// <summary>
/// Integración con Google Calendar mediante el formato estándar iCalendar (RFC 5545), sin Google Cloud:
/// lee la "dirección secreta en formato iCal" de un calendario de Google para conocer los horarios que el
/// administrador bloqueó y genera el calendario .ics de reservas al que el administrador se suscribe.
/// </summary>
public static class ICalendario
{
    private const int MaxOcurrencias = 5000;

    public static TimeZoneInfo ZonaLima { get; } = BuscarZona("America/Lima") ?? BuscarZona("SA Pacific Standard Time")
        ?? TimeZoneInfo.CreateCustomTimeZone("America/Lima", TimeSpan.FromHours(-5), "Lima", "Lima");

    public static DateTime LimaAUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), ZonaLima);

    public static DateTime UtcALima(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaLima);

    /// <summary>Solo se aceptan direcciones iCal de Google Calendar por HTTPS (evita leer direcciones arbitrarias).</summary>
    public static bool EsDireccionPermitida(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var u)
        && u.Scheme == Uri.UriSchemeHttps
        && u.Host.Equals("calendar.google.com", StringComparison.OrdinalIgnoreCase)
        && u.AbsolutePath.EndsWith(".ics", StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ lectura

    /// <summary>Intervalos ocupados (hora de Lima) que se cruzan con [desde, hasta).</summary>
    public static IReadOnlyList<Intervalo> LeerOcupados(string ics, DateTime desde, DateTime hasta)
    {
        var eventos = LeerEventos(ics);
        // Instancias modificadas de eventos repetitivos (RECURRENCE-ID): reemplazan a la ocurrencia original.
        var modificadas = eventos.Where(e => e.RecurrenceId is not null)
            .Select(e => (e.Uid, e.RecurrenceId!.Value)).ToHashSet();

        var resultado = new List<Intervalo>();
        foreach (var e in eventos)
        {
            if (e.Cancelado || e.Transparente || e.Inicio is null) continue;
            var inicio = e.Inicio.Value;
            var duracion = e.Fin is DateTime f ? f - inicio : e.Duracion ?? (e.TodoElDia ? TimeSpan.FromDays(1) : TimeSpan.Zero);
            if (duracion <= TimeSpan.Zero) continue;

            foreach (var ocurrencia in Ocurrencias(e, inicio, hasta))
            {
                if (e.Excepciones.Contains(ocurrencia)) continue;
                if (e.RecurrenceId is null && e.Regla is not null && modificadas.Contains((e.Uid, ocurrencia))) continue;
                var intervalo = new Intervalo(ocurrencia, ocurrencia + duracion);
                if (intervalo.SeCruzaCon(desde, hasta)) resultado.Add(intervalo);
            }
        }
        return resultado.OrderBy(i => i.Inicio).ToList();
    }

    private sealed class EventoLeido
    {
        public string Uid = "";
        public DateTime? Inicio;
        public DateTime? Fin;
        public TimeSpan? Duracion;
        public bool TodoElDia;
        public bool Cancelado;
        public bool Transparente;
        public string? Regla;
        public DateTime? RecurrenceId;
        public HashSet<DateTime> Excepciones = [];
    }

    private static List<EventoLeido> LeerEventos(string ics)
    {
        var texto = ics.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n ", "").Replace("\n\t", "");
        var eventos = new List<EventoLeido>();
        EventoLeido? actual = null;
        var anidado = 0; // VALARM u otros componentes dentro del VEVENT

        foreach (var linea in texto.Split('\n'))
        {
            if (linea.Length == 0) continue;
            var dosPuntos = IndiceDosPuntos(linea);
            if (dosPuntos < 0) continue;
            var cabecera = linea[..dosPuntos];
            var valor = linea[(dosPuntos + 1)..];
            var partes = cabecera.Split(';');
            var nombre = partes[0].ToUpperInvariant();
            var parametros = new Dictionary<string, string>();
            foreach (var p in partes.Skip(1).Select(x => x.Split('=', 2)).Where(x => x.Length == 2))
                parametros[p[0].ToUpperInvariant()] = p[1].Trim('"');

            if (nombre == "BEGIN")
            {
                if (valor.Equals("VEVENT", StringComparison.OrdinalIgnoreCase) && actual is null) actual = new EventoLeido();
                else if (actual is not null) anidado++;
                continue;
            }
            if (nombre == "END")
            {
                if (actual is null) continue;
                if (anidado > 0) { anidado--; continue; }
                if (valor.Equals("VEVENT", StringComparison.OrdinalIgnoreCase)) { eventos.Add(actual); actual = null; }
                continue;
            }
            if (actual is null || anidado > 0) continue;

            switch (nombre)
            {
                case "UID": actual.Uid = valor; break;
                case "DTSTART":
                    actual.Inicio = LeerFecha(valor, parametros, out actual.TodoElDia);
                    break;
                case "DTEND": actual.Fin = LeerFecha(valor, parametros, out _); break;
                case "DURATION": actual.Duracion = LeerDuracion(valor); break;
                case "STATUS": actual.Cancelado = valor.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase); break;
                case "TRANSP": actual.Transparente = valor.Equals("TRANSPARENT", StringComparison.OrdinalIgnoreCase); break;
                case "RRULE": actual.Regla = valor; break;
                case "RECURRENCE-ID": actual.RecurrenceId = LeerFecha(valor, parametros, out _); break;
                case "EXDATE":
                    foreach (var v in valor.Split(','))
                        if (LeerFecha(v, parametros, out _) is DateTime ex) actual.Excepciones.Add(ex);
                    break;
            }
        }
        return eventos;
    }

    /// <summary>Posición de los dos puntos que separan el nombre del valor (ignora los que van entre comillas).</summary>
    private static int IndiceDosPuntos(string linea)
    {
        var comillas = false;
        for (var i = 0; i < linea.Length; i++)
        {
            if (linea[i] == '"') comillas = !comillas;
            else if (linea[i] == ':' && !comillas) return i;
        }
        return -1;
    }

    /// <summary>Convierte un valor DATE o DATE-TIME de iCalendar a hora local de Lima.</summary>
    private static DateTime? LeerFecha(string valor, IReadOnlyDictionary<string, string> parametros, out bool soloFecha)
    {
        valor = valor.Trim();
        soloFecha = valor.Length == 8 || (parametros.TryGetValue("VALUE", out var tipo) && tipo.Equals("DATE", StringComparison.OrdinalIgnoreCase));
        if (soloFecha)
            return DateTime.TryParseExact(valor[..Math.Min(8, valor.Length)], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d : null;

        var utc = valor.EndsWith('Z');
        if (!DateTime.TryParseExact(valor.TrimEnd('Z'), "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return null;
        if (utc) return UtcALima(fecha);
        if (parametros.TryGetValue("TZID", out var tzid) && BuscarZona(tzid) is TimeZoneInfo zona && zona.Id != ZonaLima.Id)
        {
            try { return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified), zona, ZonaLima); }
            catch (ArgumentException) { return fecha; } // hora inexistente por cambio de horario
        }
        return fecha; // hora "flotante" o ya en America/Lima
    }

    /// <summary>Duración ISO 8601 de iCalendar: P1D, PT1H30M, P2W, etc.</summary>
    private static TimeSpan? LeerDuracion(string valor)
    {
        var m = System.Text.RegularExpressions.Regex.Match(valor.Trim(),
            @"^([+-])?P(?:(\d+)W)?(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?)?$");
        if (!m.Success) return null;
        int N(int g) => m.Groups[g].Success ? int.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture) : 0;
        var t = new TimeSpan(N(2) * 7 + N(3), N(4), N(5), N(6));
        return m.Groups[1].Value == "-" ? -t : t;
    }

    /// <summary>Inicios de cada ocurrencia hasta <paramref name="hasta"/> (reglas DAILY, WEEKLY, MONTHLY y YEARLY).</summary>
    private static IEnumerable<DateTime> Ocurrencias(EventoLeido e, DateTime inicio, DateTime hasta)
    {
        if (e.Regla is null || e.RecurrenceId is not null)
        {
            yield return inicio;
            yield break;
        }

        var regla = new Dictionary<string, string>();
        foreach (var p in e.Regla.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=', 2)).Where(x => x.Length == 2))
            regla[p[0].ToUpperInvariant()] = p[1].ToUpperInvariant();
        var frecuencia = regla.GetValueOrDefault("FREQ", "");
        var intervalo = int.TryParse(regla.GetValueOrDefault("INTERVAL"), out var iv) && iv > 0 ? iv : 1;
        int? cuenta = int.TryParse(regla.GetValueOrDefault("COUNT"), out var c) ? c : null;
        DateTime? hastaRegla = null;
        if (regla.TryGetValue("UNTIL", out var u))
        {
            hastaRegla = LeerFecha(u, new Dictionary<string, string>(), out var untilEsFecha);
            if (untilEsFecha && hastaRegla is DateTime hr) hastaRegla = hr.AddDays(1).AddTicks(-1); // UNTIL de tipo fecha incluye todo el día
        }
        var dias = regla.TryGetValue("BYDAY", out var bd)
            ? bd.Split(',').Where(d => d.Length >= 2).Select(d => DiaSemana(d[^2..])).Where(d => d is not null).Select(d => d!.Value).ToHashSet()
            : null;

        var emitidas = 0;
        for (var periodo = 0; periodo < MaxOcurrencias; periodo++)
        {
            IEnumerable<DateTime> candidatas;
            switch (frecuencia)
            {
                case "DAILY":
                    candidatas = new[] { inicio.AddDays((double)periodo * intervalo) };
                    break;
                case "WEEKLY":
                    var lunes = inicio.Date.AddDays(-(((int)inicio.DayOfWeek + 6) % 7)).AddDays(7.0 * periodo * intervalo);
                    IEnumerable<DayOfWeek> diasSemana = dias is { Count: > 0 } ? dias : new[] { inicio.DayOfWeek };
                    candidatas = diasSemana.Select(d => lunes.AddDays(((int)d + 6) % 7) + inicio.TimeOfDay).OrderBy(x => x).ToList();
                    break;
                case "MONTHLY":
                    var mes = inicio.AddMonths(periodo * intervalo);
                    candidatas = mes.Day == inicio.Day ? new[] { mes } : Array.Empty<DateTime>();
                    break;
                case "YEARLY":
                    var anio = inicio.AddYears(periodo * intervalo);
                    candidatas = anio.Day == inicio.Day ? new[] { anio } : Array.Empty<DateTime>();
                    break;
                default:
                    yield return inicio;
                    yield break;
            }

            foreach (var o in candidatas)
            {
                if (o < inicio) continue;
                if (o >= hasta || (hastaRegla is DateTime lim && o > lim) || (cuenta is int n && emitidas >= n)) yield break;
                if (frecuencia == "DAILY" && dias is { Count: > 0 } && !dias.Contains(o.DayOfWeek)) continue;
                emitidas++;
                yield return o;
            }
        }
    }

    private static DayOfWeek? DiaSemana(string d) => d switch
    {
        "MO" => DayOfWeek.Monday, "TU" => DayOfWeek.Tuesday, "WE" => DayOfWeek.Wednesday, "TH" => DayOfWeek.Thursday,
        "FR" => DayOfWeek.Friday, "SA" => DayOfWeek.Saturday, "SU" => DayOfWeek.Sunday, _ => null
    };

    private static TimeZoneInfo? BuscarZona(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return null; }
    }

    // ------------------------------------------------------------------ generación

    /// <summary>Calendario iCalendar con las reservas de una cancha (Google Calendar se suscribe a esta dirección).</summary>
    public static string GenerarFeed(string nombreCalendario, IEnumerable<EventoCalendario> eventos, DateTime ahoraUtc)
    {
        var sb = new StringBuilder();
        void L(string linea) => sb.Append(Plegar(linea)).Append("\r\n");

        L("BEGIN:VCALENDAR");
        L("VERSION:2.0");
        L("PRODID:-//Poli-App//Reservas de canchas//ES");
        L("CALSCALE:GREGORIAN");
        L("METHOD:PUBLISH");
        L("X-WR-CALNAME:" + Escapar(nombreCalendario));
        L("X-WR-TIMEZONE:America/Lima");
        L("REFRESH-INTERVAL;VALUE=DURATION:PT15M");
        L("X-PUBLISHED-TTL:PT15M");
        foreach (var e in eventos)
        {
            L("BEGIN:VEVENT");
            L("UID:" + e.Uid);
            L("DTSTAMP:" + Utc(ahoraUtc));
            L("DTSTART:" + Utc(e.InicioUtc));
            L("DTEND:" + Utc(e.FinUtc));
            L("SUMMARY:" + Escapar(e.Titulo));
            if (!string.IsNullOrWhiteSpace(e.Descripcion)) L("DESCRIPTION:" + Escapar(e.Descripcion));
            if (!string.IsNullOrWhiteSpace(e.Lugar)) L("LOCATION:" + Escapar(e.Lugar));
            L("STATUS:" + (e.Confirmado ? "CONFIRMED" : "TENTATIVE"));
            L("END:VEVENT");
        }
        L("END:VCALENDAR");
        return sb.ToString();
    }

    private static string Utc(DateTime d) => d.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    public static string Escapar(string texto) => texto
        .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
        .Replace("\r\n", "\\n").Replace("\n", "\\n");

    /// <summary>Pliega las líneas a 75 octetos como exige RFC 5545 (sin partir caracteres UTF-8).</summary>
    private static string Plegar(string linea)
    {
        if (Encoding.UTF8.GetByteCount(linea) <= 75) return linea;
        var sb = new StringBuilder();
        var octetos = 0;
        var limite = 75;
        foreach (var ch in linea.EnumerateRunes())
        {
            var n = ch.Utf8SequenceLength;
            if (octetos + n > limite)
            {
                sb.Append("\r\n ");
                octetos = 1;
                limite = 75;
            }
            sb.Append(ch.ToString());
            octetos += n;
        }
        return sb.ToString();
    }
}
