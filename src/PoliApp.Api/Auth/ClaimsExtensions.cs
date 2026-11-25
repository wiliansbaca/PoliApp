using System.Security.Claims;

namespace PoliApp.Api.Auth;

public static class ClaimsExtensions
{
    public static int UsuarioId(this ClaimsPrincipal u) => int.Parse(u.FindFirstValue("sub")!);

    /// <summary>Complejo que administra el usuario; null si es un administrador general.</summary>
    public static int? ComplejoId(this ClaimsPrincipal u) =>
        int.TryParse(u.FindFirstValue("complejo"), out var id) ? id : null;
}
