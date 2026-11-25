namespace PoliApp.Domain.Entities;

/// <summary>Complejo deportivo (local) que agrupa una o varias canchas sintéticas.</summary>
public class Complejo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    /// <summary>Calendario de Google donde se sincronizan las reservas (integración prevista).</summary>
    public string? GoogleCalendarId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Cancha> Canchas { get; set; } = new List<Cancha>();
}
