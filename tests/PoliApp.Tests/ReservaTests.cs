using PoliApp.Domain;
using PoliApp.Domain.Entities;

namespace PoliApp.Tests;

/// <summary>Pruebas derivadas de los escenarios de aceptación de HU05 y HU06.</summary>
public class ReservaTests
{
    private static readonly DateOnly Dia = new(2026, 12, 1);
    private static readonly DateTime Ahora = new(2026, 11, 30, 10, 0, 0);

    private static Reserva Nueva(int hIni, int hFin, EstadoReserva estado = EstadoReserva.Pendiente) => new()
    {
        Fecha = Dia,
        HoraInicio = new TimeOnly(hIni, 0),
        HoraFin = new TimeOnly(hFin, 0),
        Estado = estado
    };

    [Theory]
    [InlineData(18, 19, true)]   // mismo horario
    [InlineData(17, 19, true)]   // lo envuelve
    [InlineData(18, 20, true)]   // se superpone al final
    [InlineData(19, 20, false)]  // empieza justo cuando termina la otra
    [InlineData(17, 18, false)]  // termina justo cuando empieza la otra
    public void SeCruzaCon_detecta_superposiciones(int ini, int fin, bool esperado)
    {
        var existente = Nueva(18, 19);
        Assert.Equal(esperado, existente.SeCruzaCon(Dia, new TimeOnly(ini, 0), new TimeOnly(fin, 0)));
    }

    [Fact]
    public void Una_reserva_cancelada_no_bloquea_el_horario()
    {
        var cancelada = Nueva(18, 19, EstadoReserva.Cancelada);
        Assert.False(cancelada.SeCruzaCon(Dia, new TimeOnly(18, 0), new TimeOnly(19, 0)));
    }

    [Fact]
    public void Otra_fecha_no_se_cruza()
    {
        var r = Nueva(18, 19);
        Assert.False(r.SeCruzaCon(Dia.AddDays(1), new TimeOnly(18, 0), new TimeOnly(19, 0)));
    }

    [Fact]
    public void Cancelar_con_anticipacion_suficiente_cambia_el_estado()
    {
        var r = Nueva(18, 19);
        r.Cancelar(Ahora);
        Assert.Equal(EstadoReserva.Cancelada, r.Estado);
    }

    [Fact]
    public void No_se_puede_cancelar_con_menos_de_6_horas()
    {
        var r = Nueva(18, 19);
        var tarde = Dia.ToDateTime(new TimeOnly(13, 0));
        Assert.Throws<DomainException>(() => r.Cancelar(tarde));
    }

    [Fact]
    public void Reprogramar_actualiza_horario_y_vuelve_a_pendiente()
    {
        var r = Nueva(18, 19, EstadoReserva.Confirmada);
        r.Reprogramar(Dia.AddDays(1), new TimeOnly(20, 0), new TimeOnly(21, 0), 70m, Ahora);
        Assert.Equal(new TimeOnly(20, 0), r.HoraInicio);
        Assert.Equal(EstadoReserva.Pendiente, r.Estado);
        Assert.Equal(70m, r.MontoTotal);
    }

    [Fact]
    public void Solo_se_confirman_reservas_pendientes()
    {
        var r = Nueva(18, 19, EstadoReserva.Cancelada);
        Assert.Throws<DomainException>(r.Confirmar);
    }

    [Fact]
    public void Codigo_tiene_formato_R_seis_digitos()
    {
        var r = Nueva(18, 19);
        r.Id = 245;
        Assert.Equal("R-000245", r.Codigo);
    }
}
