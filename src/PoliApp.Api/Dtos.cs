using System.ComponentModel.DataAnnotations;
using PoliApp.Domain.Entities;

namespace PoliApp.Api;

// ---------- Autenticación (HU01) ----------
public record RegistroDto(
    [Required, MaxLength(80)] string Nombres,
    [Required, MaxLength(80)] string Apellidos,
    [Required, EmailAddress, MaxLength(120)] string Correo,
    [Required, RegularExpression(@"^9\d{8}$", ErrorMessage = "El celular debe tener 9 dígitos y empezar con 9.")] string Celular,
    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")] string Password);

public record LoginDto([Required, EmailAddress] string Correo, [Required] string Password);

public record SesionDto(string Token, DateTime Expira, int Id, string Nombre, string Correo, string Rol, int? ComplejoId);

// ---------- Canchas y disponibilidad (HU02, HU03, HU04) ----------
public record ComplejoDto(int Id, string Nombre, string Direccion, string Distrito, string? Telefono, int Canchas);

public record GuardarComplejoDto(
    [Required, MaxLength(120)] string Nombre,
    [Required, MaxLength(200)] string Direccion,
    [Required, MaxLength(60)] string Distrito,
    [MaxLength(15)] string? Telefono);

public record CanchaDto(int Id, int ComplejoId, string ComplejoNombre, string Distrito, string Nombre, string Tipo,
    decimal PrecioHora, string HoraApertura, string HoraCierre, bool Activa)
{
    public static CanchaDto De(Cancha c) => new(c.Id, c.ComplejoId, c.Complejo?.Nombre ?? "", c.Complejo?.Distrito ?? "",
        c.Nombre, c.Tipo, c.PrecioHora, c.HoraApertura.ToString("HH:mm"), c.HoraCierre.ToString("HH:mm"), c.Activa);
}

public record GuardarCanchaDto(
    [Required] int ComplejoId,
    [Required, MaxLength(80)] string Nombre,
    [Required, MaxLength(40)] string Tipo,
    [Range(1, 9999)] decimal PrecioHora,
    [Required] string HoraApertura,
    [Required] string HoraCierre,
    bool Activa = true);

public record HorarioTarifaDto([Required] string HoraApertura, [Required] string HoraCierre, [Range(1, 9999)] decimal PrecioHora);

public record BloqueDto(string Hora, bool Libre);

public record DisponibilidadDto(CanchaDto Cancha, IReadOnlyList<BloqueDto> Bloques);

public record HorarioOcupadoDto(string HoraInicio, string HoraFin);

// ---------- Google Calendar (HU07, HU08) ----------
public record CalendarioCanchaDto(int CanchaId, string Cancha, string? IcalOcupadosUrl, string UrlSuscripcion);

public record VincularCalendarioDto([MaxLength(500)] string? IcalOcupadosUrl);

// ---------- Reservas (HU05, HU06, HU07) ----------
public record ReservaDto(
    int Id, string Codigo, int CanchaId, string CanchaNombre, string ComplejoNombre, string ClienteNombre, string ClienteTelefono,
    string Fecha, string HoraInicio, string HoraFin, decimal MontoTotal, string Estado, string? MotivoRechazo,
    bool PuedeModificarse, string? EstadoPago)
{
    public static ReservaDto De(Reserva r, DateTime ahora) => new(
        r.Id, r.Codigo, r.CanchaId, r.Cancha?.Nombre ?? "", r.Cancha?.Complejo?.Nombre ?? "", r.ClienteNombre, r.ClienteTelefono,
        r.Fecha.ToString("yyyy-MM-dd"), r.HoraInicio.ToString("HH:mm"), r.HoraFin.ToString("HH:mm"), r.MontoTotal,
        r.Estado.ToString(), r.MotivoRechazo, r.PuedeModificarse(ahora),
        r.Pagos.OrderByDescending(p => p.Id).Select(p => p.Estado.ToString()).FirstOrDefault());
}

public record CrearReservaDto([Required] int CanchaId, [Required] string Fecha, [Required] string HoraInicio, [Required] string HoraFin);

public record ReprogramarDto([Required] string Fecha, [Required] string HoraInicio, [Required] string HoraFin);

public record RechazarDto([MaxLength(200)] string? Motivo);

// ---------- Pagos (HU11) ----------
public record RegistrarPagoDto(
    [Required, RegularExpression("^(Yape|Plin)$", ErrorMessage = "Método no válido.")] string Metodo,
    [Required, RegularExpression(@"^\d{6,12}$", ErrorMessage = "El número de operación debe tener entre 6 y 12 dígitos.")] string NumeroOperacion);

public record PagoDto(int Id, int ReservaId, string CodigoReserva, string Cliente, string Cancha, string Fecha,
    string Metodo, string NumeroOperacion, decimal Monto, string Estado, DateTime FechaRegistro);

// ---------- Reportes (HU09) ----------
public record ReporteCanchaDto(int CanchaId, string Cancha, int Reservas, double HorasReservadas, double HorasDisponibles, double Ocupacion, decimal Ingresos);

public record ReporteDto(string Desde, string Hasta, int TotalReservas, decimal TotalIngresos, double OcupacionPromedio,
    IReadOnlyList<ReporteCanchaDto> PorCancha, IReadOnlyList<IngresoDiaDto> PorDia);

public record IngresoDiaDto(string Fecha, decimal Ingresos, int Reservas);

public record PanelDto(int ReservasHoy, int PendientesPorAtender, int PagosPorValidar, decimal IngresosMes, double OcupacionSemana);

/// <summary>Conversión de cadenas "yyyy-MM-dd" / "HH:mm" con mensajes claros.</summary>
public static class Formatos
{
    public static DateOnly Fecha(string s) =>
        DateOnly.TryParse(s, out var f) ? f : throw new PoliApp.Domain.DomainException("Fecha inválida, use el formato yyyy-MM-dd.");

    public static TimeOnly Hora(string s) =>
        TimeOnly.TryParse(s, out var h) ? h : throw new PoliApp.Domain.DomainException("Hora inválida, use el formato HH:mm.");
}
