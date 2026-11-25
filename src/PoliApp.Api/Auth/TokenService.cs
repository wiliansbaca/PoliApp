using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PoliApp.Domain.Entities;

namespace PoliApp.Api.Auth;

/// <summary>Emite el token JWT con la identidad y el rol del usuario (HU01).</summary>
public class TokenService(IOptions<JwtOptions> opciones)
{
    private readonly JwtOptions _jwt = opciones.Value;

    public (string Token, DateTime Expira) Emitir(Usuario u)
    {
        var expira = DateTime.UtcNow.AddHours(_jwt.HorasExpiracion);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, u.Correo),
            new("name", u.NombreCompleto),
            new("role", u.Rol.ToString())
        };
        if (u.ComplejoId is int complejoId)
            claims.Add(new Claim("complejo", complejoId.ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expira,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)), SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }
}
