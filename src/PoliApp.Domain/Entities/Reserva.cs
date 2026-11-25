namespace PoliApp.Domain.Entities;

public enum EstadoReserva : byte
{
    Pendiente = 0,
    Confirmada = 1,
    Cancelada = 2,
    Rechazada = 3,
    Finalizada = 4
}

public class Reserva
{
    /// <summary>Horas mínimas de anticipación para cancelar o reprogramar.</summary>
    public const int HorasMinimasParaModificar = 6;

    public int Id { get; set; }
    public int CanchaId { get; set; }
    public int? UsuarioId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public decimal MontoTotal { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Cancha? Cancha { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public string Codigo => $"R-{Id:D6}";
    public DateTime Inicio => Fecha.ToDateTime(HoraInicio);
    public bool EstaActiva => Estado is EstadoReserva.Pendiente or EstadoReserva.Confirmada;

    /// <summary>Indica si esta reserva se cruza con el intervalo [inicio, fin) del mismo día.</summary>
    public bool SeCruzaCon(DateOnly fecha, TimeOnly inicio, TimeOnly fin) =>
        EstaActiva && Fecha == fecha && HoraInicio < fin && HoraFin > inicio;

    public bool PuedeModificarse(DateTime ahora) =>
        EstaActiva && Inicio - ahora >= TimeSpan.FromHours(HorasMinimasParaModificar);

    public void Confirmar()
    {
        if (Estado != EstadoReserva.Pendiente)
            throw new DomainException("Solo se pueden confirmar reservas pendientes.");
        Estado = EstadoReserva.Confirmada;
    }

    public void Rechazar(string? motivo)
    {
        if (Estado != EstadoReserva.Pendiente)
            throw new DomainException("Solo se pueden rechazar reservas pendientes.");
        Estado = EstadoReserva.Rechazada;
        MotivoRechazo = motivo;
    }

    public void Cancelar(DateTime ahora)
    {
        if (!PuedeModificarse(ahora))
            throw new DomainException($"Solo se puede cancelar hasta {HorasMinimasParaModificar} horas antes del inicio del turno.");
        Estado = EstadoReserva.Cancelada;
    }

    public void Reprogramar(DateOnly fecha, TimeOnly inicio, TimeOnly fin, decimal monto, DateTime ahora)
    {
        if (!PuedeModificarse(ahora))
            throw new DomainException($"Solo se puede reprogramar hasta {HorasMinimasParaModificar} horas antes del inicio del turno.");
        if (fecha.ToDateTime(inicio) <= ahora)
            throw new DomainException("El nuevo horario debe ser futuro.");
        Fecha = fecha;
        HoraInicio = inicio;
        HoraFin = fin;
        MontoTotal = monto;
        Estado = EstadoReserva.Pendiente;
    }
}
