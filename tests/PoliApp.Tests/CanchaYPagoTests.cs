using PoliApp.Domain;
using PoliApp.Domain.Entities;

namespace PoliApp.Tests;

/// <summary>Pruebas de HU03 (horarios y tarifas) y HU11 (pago en línea).</summary>
public class CanchaYPagoTests
{
    [Fact]
    public void Monto_se_calcula_por_horas_y_tarifa()
    {
        var c = new Cancha { PrecioHora = 70m };
        Assert.Equal(140m, c.CalcularMonto(new TimeOnly(18, 0), new TimeOnly(20, 0)));
    }

    [Fact]
    public void Bloques_horarios_respetan_apertura_y_cierre()
    {
        var c = new Cancha { HoraApertura = new TimeOnly(17, 0), HoraCierre = new TimeOnly(20, 0) };
        Assert.Equal(new[] { new TimeOnly(17, 0), new TimeOnly(18, 0), new TimeOnly(19, 0) }, c.BloquesHorarios());
    }

    [Fact]
    public void No_atiende_fuera_de_horario()
    {
        var c = new Cancha { HoraApertura = new TimeOnly(7, 0), HoraCierre = new TimeOnly(23, 0) };
        Assert.False(c.AtiendeEn(new TimeOnly(6, 0), new TimeOnly(7, 0)));
        Assert.True(c.AtiendeEn(new TimeOnly(22, 0), new TimeOnly(23, 0)));
    }

    [Fact]
    public void Configurar_horario_invalido_lanza_error()
    {
        var c = new Cancha();
        Assert.Throws<DomainException>(() => c.ConfigurarHorario(new TimeOnly(20, 0), new TimeOnly(8, 0), 60m));
        Assert.Throws<DomainException>(() => c.ConfigurarHorario(new TimeOnly(8, 0), new TimeOnly(20, 0), 0m));
    }

    [Fact]
    public void Validar_pago_confirma_la_reserva_pendiente()
    {
        var r = new Reserva { Estado = EstadoReserva.Pendiente };
        var p = new Pago { Reserva = r, Monto = 70m };
        p.Validar();
        Assert.Equal(EstadoPago.Validado, p.Estado);
        Assert.Equal(EstadoReserva.Confirmada, r.Estado);
    }

    [Fact]
    public void Un_pago_revisado_no_se_puede_revisar_otra_vez()
    {
        var p = new Pago { Reserva = new Reserva() };
        p.Observar();
        Assert.Throws<DomainException>(p.Validar);
    }
}
