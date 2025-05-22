using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

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
            //// Configuraciones Fluent API si las necesitas
            //model.Entity<DataPoint>()
            //     .HasKey(dp => dp.Id);
            //// …
        }
    }
}
