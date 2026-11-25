using System.Net;
using System.Net.Mail;
using PoliApp.Domain.Entities;

namespace PoliApp.Api.Services;

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Puerto { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public string Remitente { get; set; } = "reservas@poliapp.pe";
}

/// <summary>
/// Envía el correo de confirmación de la reserva (HU08). Si no hay servidor SMTP configurado
/// (entorno de desarrollo), el mensaje se registra en el log en lugar de enviarse.
/// </summary>
public class NotificacionService(Microsoft.Extensions.Options.IOptions<SmtpOptions> opciones, ILogger<NotificacionService> log)
{
    private readonly SmtpOptions _smtp = opciones.Value;

    public async Task ReservaConfirmadaAsync(Reserva r, string correo)
    {
        var asunto = $"Poli-App: tu reserva {r.Codigo} está confirmada";
        var cuerpo = $"""
            <h2>¡Tu reserva está confirmada!</h2>
            <p>Hola {WebUtility.HtmlEncode(r.ClienteNombre)},</p>
            <p><b>{WebUtility.HtmlEncode(r.Cancha?.Complejo?.Nombre)} · {WebUtility.HtmlEncode(r.Cancha?.Nombre)}</b><br/>
            {r.Fecha:dd/MM/yyyy} de {r.HoraInicio:HH\:mm} a {r.HoraFin:HH\:mm}<br/>
            Monto: S/ {r.MontoTotal:0.00}</p>
            <p>Recuerda que puedes cancelar o reprogramar hasta {Reserva.HorasMinimasParaModificar} horas antes.</p>
            """;

        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            log.LogInformation("Correo (simulado) a {Correo}: {Asunto}", correo, asunto);
            return;
        }

        using var cliente = new SmtpClient(_smtp.Host, _smtp.Puerto)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_smtp.Usuario, _smtp.Clave)
        };
        using var msg = new MailMessage(_smtp.Remitente, correo, asunto, cuerpo) { IsBodyHtml = true };
        try { await cliente.SendMailAsync(msg); }
        catch (Exception ex) { log.LogWarning(ex, "No se pudo enviar el correo a {Correo}", correo); }
    }
}
