using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PoliApp.Api.Auth;
using PoliApp.Api.Services;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Servicios de aplicación
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<NotificacionService>();
// Integración con Google Calendar mediante iCalendar (sin Google Cloud)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(CalendarioExternoService.ClienteHttp, c =>
{
    c.Timeout = TimeSpan.FromSeconds(8);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("PoliApp/1.0 (+https://www.poliapp.tech)");
});
builder.Services.AddScoped<CalendarioExternoService>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

// Seguridad: autenticación con JWT y autorización por roles (HU01)
var jwtSeccion = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSeccion);
var jwt = jwtSeccion.Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddPolicy("Angular", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Documentación interactiva en /scalar
}

// Agrega a la base existente las columnas de la integración con Google Calendar.
await DbSeeder.ActualizarEsquemaAsync(app.Services);

// Crea el esquema y los datos de demostración (en desarrollo o si se activa SembrarDatosDemo).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SembrarDatosDemo"))
    await DbSeeder.SembrarAsync(app.Services);

app.UseHttpsRedirection();
app.UseDefaultFiles();   // En producción la API también sirve el frontend Angular compilado (wwwroot)
app.UseStaticFiles();
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
