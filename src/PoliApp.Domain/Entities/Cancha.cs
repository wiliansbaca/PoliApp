namespace PoliApp.Domain.Entities;

public class Cancha
{
    public int Id { get; set; }
    public int ComplejoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal PrecioHora { get; set; }
    public TimeOnly HoraApertura { get; set; } = new(7, 0);
    public TimeOnly HoraCierre { get; set; } = new(23, 0);
    public bool Activa { get; set; } = true;

    public Complejo? Complejo { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    /// <summary>Bloques de una hora en los que la cancha atiende.</summary>
    public IEnumerable<TimeOnly> BloquesHorarios()
    {
        for (var h = HoraApertura; h < HoraCierre; h = h.AddHours(1))
        {
            yield return h;
            if (h.AddHours(1) < h) yield break; // evita desbordar la medianoche
        }
    }

    public bool AtiendeEn(TimeOnly inicio, TimeOnly fin) => inicio >= HoraApertura && fin <= HoraCierre && fin > inicio;

    public decimal CalcularMonto(TimeOnly inicio, TimeOnly fin) =>
        Math.Round(PrecioHora * (decimal)(fin - inicio).TotalHours, 2);

    public void ConfigurarHorario(TimeOnly apertura, TimeOnly cierre, decimal precioHora)
    {
        if (cierre <= apertura) throw new DomainException("La hora de cierre debe ser mayor que la de apertura.");
        if (precioHora <= 0) throw new DomainException("La tarifa debe ser mayor que cero.");
        HoraApertura = apertura;
        HoraCierre = cierre;
        PrecioHora = precioHora;
    }
}
