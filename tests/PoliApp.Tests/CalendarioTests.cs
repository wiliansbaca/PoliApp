using PoliApp.Domain;
using PoliApp.Domain.Calendario;
using PoliApp.Domain.Entities;

namespace PoliApp.Tests;

/// <summary>Pruebas de la integración con Google Calendar mediante iCalendar (HU07 y HU08).</summary>
public class CalendarioTests
{
    private static readonly DateTime Dia = new(2026, 12, 1);

    private static string Calendario(params string[] eventos) =>
        "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Google Inc//Google Calendar 70.9054//EN\r\n" +
        string.Concat(eventos.Select(e => "BEGIN:VEVENT\r\n" + e + "\r\nEND:VEVENT\r\n")) +
        "END:VCALENDAR\r\n";

    private static IReadOnlyList<Intervalo> Ocupados(string ics) => ICalendario.LeerOcupados(ics, Dia, Dia.AddDays(1));

    [Fact]
    public void Lee_un_evento_en_UTC_y_lo_convierte_a_hora_de_Lima()
    {
        // 23:00 UTC = 18:00 en Lima (UTC-5)
        var ics = Calendario("UID:a1\r\nDTSTART:20261201T230000Z\r\nDTEND:20261202T000000Z\r\nSUMMARY:Academia");
        var i = Assert.Single(Ocupados(ics));
        Assert.Equal(new DateTime(2026, 12, 1, 18, 0, 0), i.Inicio);
        Assert.Equal(new DateTime(2026, 12, 1, 19, 0, 0), i.Fin);
    }

    [Fact]
    public void Lee_un_evento_con_zona_horaria_de_Lima()
    {
        var ics = Calendario("UID:a2\r\nDTSTART;TZID=America/Lima:20261201T200000\r\nDTEND;TZID=America/Lima:20261201T220000");
        var i = Assert.Single(Ocupados(ics));
        Assert.Equal(new DateTime(2026, 12, 1, 20, 0, 0), i.Inicio);
        Assert.Equal(new DateTime(2026, 12, 1, 22, 0, 0), i.Fin);
    }

    [Fact]
    public void Un_evento_de_todo_el_dia_bloquea_el_dia_completo()
    {
        var ics = Calendario("UID:a3\r\nDTSTART;VALUE=DATE:20261201\r\nDTEND;VALUE=DATE:20261202\r\nSUMMARY:Mantenimiento");
        var i = Assert.Single(Ocupados(ics));
        Assert.True(i.SeCruzaCon(Dia.AddHours(7), Dia.AddHours(8)));
        Assert.True(i.SeCruzaCon(Dia.AddHours(22), Dia.AddHours(23)));
    }

    [Fact]
    public void Ignora_eventos_cancelados_y_los_marcados_como_disponibles()
    {
        var ics = Calendario(
            "UID:a4\r\nDTSTART:20261201T230000Z\r\nDTEND:20261202T000000Z\r\nSTATUS:CANCELLED",
            "UID:a5\r\nDTSTART:20261201T220000Z\r\nDTEND:20261201T230000Z\r\nTRANSP:TRANSPARENT");
        Assert.Empty(Ocupados(ics));
    }

    [Fact]
    public void Expande_un_evento_semanal_y_respeta_las_excepciones()
    {
        // Todos los martes 19:00-20:00 desde el 3 de noviembre; el 1 de diciembre (martes) fue excluido.
        var semanal = "UID:a6\r\nDTSTART;TZID=America/Lima:20261103T190000\r\nDTEND;TZID=America/Lima:20261103T200000\r\n" +
                      "RRULE:FREQ=WEEKLY;BYDAY=TU";
        Assert.Single(Ocupados(Calendario(semanal)));
        Assert.Empty(Ocupados(Calendario(semanal + "\r\nEXDATE;TZID=America/Lima:20261201T190000")));
    }

    [Fact]
    public void Una_regla_con_COUNT_termina_en_la_ocurrencia_indicada()
    {
        // Diario desde el 28 de noviembre, solo 3 veces: 28, 29 y 30; el 1 de diciembre ya no.
        var ics = Calendario("UID:a7\r\nDTSTART:20261128T230000Z\r\nDTEND:20261129T000000Z\r\nRRULE:FREQ=DAILY;COUNT=3");
        Assert.Empty(Ocupados(ics));
    }

    [Fact]
    public void Une_las_lineas_plegadas_e_ignora_las_alarmas()
    {
        var ics = Calendario("UID:a8\r\nDTSTART:2026120\r\n 1T230000Z\r\nDTEND:20261202T000000Z\r\n" +
                             "BEGIN:VALARM\r\nTRIGGER:-PT30M\r\nACTION:DISPLAY\r\nEND:VALARM");
        Assert.Single(Ocupados(ics));
    }

    [Fact]
    public void El_calendario_de_reservas_es_un_iCalendar_valido()
    {
        var inicio = ICalendario.LimaAUtc(new DateTime(2026, 12, 1, 18, 0, 0));
        var ics = ICalendario.GenerarFeed("Poli-App · Valencia · Cancha 1",
            [new EventoCalendario("reserva-1@poliapp.tech", "Juan Pérez (R-000001)", inicio, inicio.AddHours(1),
                "Reserva R-000001\nCelular: 984123456", "Complejo Valencia, Wanchaq", true)],
            new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc));

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.Contains("DTSTART:20261201T230000Z\r\n", ics);
        Assert.Contains("DTEND:20261202T000000Z\r\n", ics);
        Assert.Contains("LOCATION:Complejo Valencia\\, Wanchaq", ics);
        Assert.Contains("STATUS:CONFIRMED", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        // El calendario generado se puede volver a leer
        var i = Assert.Single(ICalendario.LeerOcupados(ics, Dia, Dia.AddDays(1)));
        Assert.Equal(new DateTime(2026, 12, 1, 18, 0, 0), i.Inicio);
    }

    [Theory]
    [InlineData("https://calendar.google.com/calendar/ical/abc%40group.calendar.google.com/private-123/basic.ics", true)]
    [InlineData("http://calendar.google.com/calendar/ical/abc/private-123/basic.ics", false)]
    [InlineData("https://ejemplo.com/calendario.ics", false)]
    [InlineData("https://calendar.google.com/calendar/u/0/r", false)]
    public void Solo_acepta_direcciones_iCal_de_Google_Calendar(string url, bool esperado) =>
        Assert.Equal(esperado, ICalendario.EsDireccionPermitida(url));

    [Fact]
    public void Vincular_una_direccion_no_valida_lanza_error_y_una_vacia_desvincula()
    {
        var cancha = new Cancha { ICalOcupadosUrl = "https://calendar.google.com/calendar/ical/x/private-1/basic.ics" };
        Assert.Throws<DomainException>(() => cancha.VincularCalendario("https://ejemplo.com/a.ics"));
        cancha.VincularCalendario("  ");
        Assert.Null(cancha.ICalOcupadosUrl);
        Assert.NotNull(cancha.CalendarioToken);
    }
}
