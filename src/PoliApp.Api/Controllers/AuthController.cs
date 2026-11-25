using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoliApp.Api.Auth;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Controllers;

/// <summary>HU01: registro e inicio de sesión con token JWT.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(PoliAppDbContext db, IPasswordHasher<Usuario> hasher, TokenService tokens) : ControllerBase
{
    [HttpPost("registro")]
    public async Task<ActionResult<SesionDto>> Registrar(RegistroDto dto)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo))
            return Problem("Ya existe una cuenta con ese correo.", statusCode: 409);

        var u = new Usuario
        {
            Nombres = dto.Nombres.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            Correo = correo,
            Celular = dto.Celular.Trim(),
            Rol = Rol.Deportista
        };
        u.PasswordHash = hasher.HashPassword(u, dto.Password);
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return Sesion(u);
    }

    [HttpPost("login")]
    public async Task<ActionResult<SesionDto>> Login(LoginDto dto)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Correo == correo);
        if (u is null || hasher.VerifyHashedPassword(u, u.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Problem("Correo o contraseña incorrectos.", statusCode: 401);
        return Sesion(u);
    }

    [Authorize]
    [HttpGet("yo")]
    public async Task<ActionResult<SesionDto>> Yo()
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        return u is null ? Unauthorized() : Sesion(u);
    }

    private SesionDto Sesion(Usuario u)
    {
        var (token, expira) = tokens.Emitir(u);
        return new SesionDto(token, expira, u.Id, u.NombreCompleto, u.Correo, u.Rol.ToString(), u.ComplejoId);
    }
}
