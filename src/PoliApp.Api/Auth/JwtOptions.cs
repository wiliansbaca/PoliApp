namespace PoliApp.Api.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "PoliApp";
    public string Audience { get; set; } = "PoliApp.Web";
    /// <summary>Clave HMAC-SHA256 (mínimo 32 caracteres).</summary>
    public string Key { get; set; } = string.Empty;
    public int HorasExpiracion { get; set; } = 8;
}
