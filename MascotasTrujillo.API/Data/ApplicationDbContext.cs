using MascotasTrujillo.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MascotasTrujillo.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- ESTAS SON TUS TABLAS ---
        public DbSet<Mascota> Mascotas { get; set; }
        public DbSet<Avistamiento> Avistamientos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Esta es la línea mágica que le dice a EF Core que use PostGIS
            modelBuilder.HasPostgresExtension("postgis");

            base.OnModelCreating(modelBuilder);
        }
    }
}
