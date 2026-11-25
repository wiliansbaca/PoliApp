namespace PoliApp.Domain.Entities;

public enum Rol : byte
{
    Deportista = 0,
    Administrador = 1
}

public class Usuario
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Rol Rol { get; set; } = Rol.Deportista;

    /// <summary>Complejo que administra (solo para administradores).</summary>
    public int? ComplejoId { get; set; }
    public Complejo? Complejo { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
