using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> opts)
            : base(opts) { }

        public DbSet<DataPoint> DataPoints { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }
        public DbSet<Activo> Activos { get; set; }
        public DbSet<Divisa> Divisas { get; set; }

        protected override void OnModelCreating(ModelBuilder model)
        {
            // Configuración DataPoint
            model.Entity<DataPoint>(entity =>
            {
                entity.HasKey(dp => dp.Id);

                entity.Property(dp => dp.Status)
                    .HasConversion<string>();

                entity.OwnsOne(dp => dp.File, file =>
                {
                    file.Property(f => f.FileName)
                        .HasMaxLength(255)
                        .IsRequired();

                    file.Property(f => f.SizeInBytes)
                        .IsRequired();

                    file.Property(f => f.ContentType)
                        .HasMaxLength(100)
                        .IsRequired();
                });

                entity.HasMany(dp => dp.Movements)
                    .WithOne(m => m.DataPoint)
                    .HasForeignKey(m => m.DataPointId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(dp => dp.CreatedAt);
                entity.HasIndex(dp => dp.Status);
            });

            // Configuración Movimiento
            model.Entity<Movimiento>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Tipo)
                    .HasConversion<string>();

                entity.Property(m => m.Moneda)
                    .HasConversion<string>();

                entity.Property(m => m.Broker)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(m => m.Ticker)
                    .HasMaxLength(20);

                entity.Property(m => m.Precio)
                    .HasPrecision(18, 4);

                entity.Property(m => m.Comision)
                    .HasPrecision(18, 4);

                entity.Property(m => m.IvaComision)
                    .HasPrecision(18, 4);

                entity.Property(m => m.OtrosImpuestos)
                    .HasPrecision(18, 4);

                entity.Property(m => m.MontoTotal)
                    .HasPrecision(18, 4);

                entity.Property(m => m.Observaciones)
                    .HasMaxLength(1000);

                entity.HasIndex(m => m.NumeroMovimiento);
                entity.HasIndex(m => m.Broker);
                entity.HasIndex(m => m.Ticker);
                entity.HasIndex(m => m.FechaConcertacion);
                entity.HasIndex(m => m.Tipo);
            });

            // Configuración Activo
            model.Entity<Activo>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Tipo)
                    .HasConversion<string>();

                entity.Property(a => a.Ticker)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(a => a.Nombre)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(a => a.PrecioPromedio)
                    .HasPrecision(18, 4);

                entity.HasIndex(a => a.Ticker).IsUnique();
                entity.HasIndex(a => a.Tipo);
            });

            // Configuración Divisa
            model.Entity<Divisa>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Tipo)
                    .HasConversion<string>();

                entity.Property(d => d.Codigo)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(d => d.Simbolo)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(d => d.Descripcion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(d => d.Cantidad)
                    .HasPrecision(18, 4);

                entity.HasIndex(d => d.Codigo).IsUnique();
                entity.HasIndex(d => d.Tipo);
            });
        }
    }
}