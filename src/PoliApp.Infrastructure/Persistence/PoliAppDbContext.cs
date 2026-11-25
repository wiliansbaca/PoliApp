using Microsoft.EntityFrameworkCore;
using PoliApp.Domain.Entities;

namespace PoliApp.Infrastructure.Persistence;

public class PoliAppDbContext(DbContextOptions<PoliAppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Complejo> Complejos => Set<Complejo>();
    public DbSet<Cancha> Canchas => Set<Cancha>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.Nombres).HasMaxLength(80).IsRequired();
            e.Property(x => x.Apellidos).HasMaxLength(80).IsRequired();
            e.Property(x => x.Correo).HasMaxLength(120).IsRequired();
            e.Property(x => x.Celular).HasMaxLength(15).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.Rol).HasConversion<byte>();
            e.HasIndex(x => x.Correo).IsUnique();
            e.HasOne(x => x.Complejo).WithMany().HasForeignKey(x => x.ComplejoId);
            e.Ignore(x => x.NombreCompleto);
        });

        mb.Entity<Complejo>(e =>
        {
            e.ToTable("Complejos");
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(200).IsRequired();
            e.Property(x => x.Distrito).HasMaxLength(60).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(15);
            e.Property(x => x.GoogleCalendarId).HasMaxLength(200);
        });

        mb.Entity<Cancha>(e =>
        {
            e.ToTable("Canchas");
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.Property(x => x.Tipo).HasMaxLength(40).IsRequired();
            e.Property(x => x.PrecioHora).HasPrecision(10, 2);
            e.Property(x => x.ICalOcupadosUrl).HasMaxLength(500);
            e.Property(x => x.CalendarioToken).HasMaxLength(32);
            e.HasOne(x => x.Complejo).WithMany(c => c.Canchas).HasForeignKey(x => x.ComplejoId);
        });

        mb.Entity<Reserva>(e =>
        {
            e.ToTable("Reservas");
            e.Property(x => x.ClienteNombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.ClienteTelefono).HasMaxLength(15).IsRequired();
            e.Property(x => x.MontoTotal).HasPrecision(10, 2);
            e.Property(x => x.MotivoRechazo).HasMaxLength(200);
            e.Property(x => x.Estado).HasConversion<byte>();
            e.HasOne(x => x.Cancha).WithMany(c => c.Reservas).HasForeignKey(x => x.CanchaId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasIndex(x => new { x.CanchaId, x.Fecha });
            e.Ignore(x => x.Codigo);
            e.Ignore(x => x.Inicio);
            e.Ignore(x => x.EstaActiva);
        });

        mb.Entity<Pago>(e =>
        {
            e.ToTable("Pagos");
            e.Property(x => x.Metodo).HasMaxLength(10).IsRequired();
            e.Property(x => x.NumeroOperacion).HasMaxLength(30).IsRequired();
            e.Property(x => x.Monto).HasPrecision(10, 2);
            e.Property(x => x.Estado).HasConversion<byte>();
            e.HasOne(x => x.Reserva).WithMany(r => r.Pagos).HasForeignKey(x => x.ReservaId);
        });
    }
}
