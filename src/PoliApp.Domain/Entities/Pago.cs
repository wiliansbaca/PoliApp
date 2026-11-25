namespace PoliApp.Domain.Entities;

public enum EstadoPago : byte
{
    PorValidar = 0,
    Validado = 1,
    Observado = 2
}

/// <summary>Pago en línea de una reserva mediante billetera digital (Yape o Plin).</summary>
public class Pago
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public string Metodo { get; set; } = "Yape";
    public string NumeroOperacion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.PorValidar;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Reserva? Reserva { get; set; }

    public void Validar()
    {
        if (Estado != EstadoPago.PorValidar) throw new DomainException("El pago ya fue revisado.");
        Estado = EstadoPago.Validado;
        if (Reserva is { Estado: EstadoReserva.Pendiente }) Reserva.Confirmar();
    }

    public void Observar()
    {
        if (Estado != EstadoPago.PorValidar) throw new DomainException("El pago ya fue revisado.");
        Estado = EstadoPago.Observado;
    }
}
