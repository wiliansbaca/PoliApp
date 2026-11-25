using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PoliApp.Domain.Entities;
using PoliApp.Infrastructure.Persistence;

namespace PoliApp.Api.Services;

/// <summary>Carga usuarios y reservas de prueba la primera vez que se ejecuta la API en desarrollo.</summary>
public static class DbSeeder
{
    public static async Task SembrarAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PoliAppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        // En una base vacía (p. ej. Azure SQL recién creada) se generan las tablas desde el modelo.
        await db.Database.EnsureCreatedAsync();

        if (!await db.Complejos.AnyAsync())
        {
            var valencia = new Complejo { Nombre = "Complejo Deportivo Valencia", Direccion = "Av. de la Cultura s/n", Distrito = "Wanchaq", Telefono = "984000000" };
            var caminoReal = new Complejo { Nombre = "Camino Real", Direccion = "Urb. Camino Real B-4", Distrito = "San Sebastián", Telefono = "984111222" };
            var coripata = new Complejo { Nombre = "Coripata Sur", Direccion = "Av. Velasco Astete 120", Distrito = "Wanchaq", Telefono = "984333444" };
            db.Complejos.AddRange(valencia, caminoReal, coripata);
            db.Canchas.AddRange(
                new Cancha { Complejo = valencia, Nombre = "Cancha 1", Tipo = "Fútbol 7", PrecioHora = 70, HoraApertura = new(7, 0), HoraCierre = new(23, 0) },
                new Cancha { Complejo = valencia, Nombre = "Cancha 2", Tipo = "Fútbol 7", PrecioHora = 70, HoraApertura = new(7, 0), HoraCierre = new(23, 0) },
                new Cancha { Complejo = caminoReal, Nombre = "Cancha 1", Tipo = "Fútbol 6", PrecioHora = 60, HoraApertura = new(8, 0), HoraCierre = new(23, 0) },
                new Cancha { Complejo = caminoReal, Nombre = "Cancha 2", Tipo = "Fútbol 8", PrecioHora = 80, HoraApertura = new(8, 0), HoraCierre = new(23, 0) },
                new Cancha { Complejo = coripata, Nombre = "Cancha 1", Tipo = "Fútbol 7", PrecioHora = 65, HoraApertura = new(9, 0), HoraCierre = new(22, 0) });
            await db.SaveChangesAsync();
        }

        if (await db.Usuarios.AnyAsync()) return;

        var complejo = await db.Complejos.OrderBy(c => c.Id).FirstOrDefaultAsync();
        if (complejo is null) return;

        var admin = new Usuario { Nombres = "Paul", Apellidos = "Valencia", Correo = "admin@poliapp.pe", Celular = "984000000", Rol = Rol.Administrador, ComplejoId = complejo.Id };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123*");
        var juan = new Usuario { Nombres = "Juan", Apellidos = "Pérez Quispe", Correo = "jperez@gmail.com", Celular = "984123456" };
        juan.PasswordHash = hasher.HashPassword(juan, "Juan123*");
        db.Usuarios.AddRange(admin, juan);
        await db.SaveChangesAsync();

        var canchas = await db.Canchas.Where(c => c.ComplejoId == complejo.Id).OrderBy(c => c.Id).ToListAsync();
        if (canchas.Count == 0 || await db.Reservas.AnyAsync()) return;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        string[] clientes = ["Carlos Huamán", "Academia Talentos", "Luis Mamani", "Rosa Quispe", "Diego Ccahuana"];
        var rnd = new Random(7);
        var lista = new List<Reserva>();

        // Historial de los últimos 14 días (para reportes) y próximos 3 días (para el panel).
        for (var d = -14; d <= 3; d++)
        {
            var fecha = hoy.AddDays(d);
            foreach (var c in canchas)
            {
                foreach (var hora in new[] { 17, 18, 19, 20, 21 })
                {
                    if (rnd.NextDouble() > 0.55) continue;
                    var ini = new TimeOnly(hora, 0);
                    var esJuan = rnd.NextDouble() < 0.15;
                    var r = new Reserva
                    {
                        CanchaId = c.Id,
                        UsuarioId = esJuan ? juan.Id : null,
                        ClienteNombre = esJuan ? juan.NombreCompleto : clientes[rnd.Next(clientes.Length)],
                        ClienteTelefono = esJuan ? juan.Celular : $"9{rnd.Next(10000000, 99999999)}",
                        Fecha = fecha,
                        HoraInicio = ini,
                        HoraFin = ini.AddHours(1),
                        MontoTotal = c.PrecioHora,
                        Estado = d < 0 ? (rnd.NextDouble() < 0.1 ? EstadoReserva.Cancelada : EstadoReserva.Finalizada)
                                       : (rnd.NextDouble() < 0.5 ? EstadoReserva.Pendiente : EstadoReserva.Confirmada)
                    };
                    lista.Add(r);
                }
            }
        }
        db.Reservas.AddRange(lista);
        await db.SaveChangesAsync();

        // Un pago de Yape por validar para mostrar el flujo de HU11.
        var pendiente = lista.FirstOrDefault(r => r.Estado == EstadoReserva.Pendiente);
        if (pendiente is not null)
        {
            db.Pagos.Add(new Pago { ReservaId = pendiente.Id, Metodo = "Yape", NumeroOperacion = "12345678", Monto = pendiente.MontoTotal });
            await db.SaveChangesAsync();
        }
    }
}
