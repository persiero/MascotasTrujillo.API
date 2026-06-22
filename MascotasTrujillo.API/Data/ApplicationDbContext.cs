using MascotasTrujillo.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MascotasTrujillo.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario, IdentityRole<long>, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Mascota> Mascotas { get; set; }
        public DbSet<InformacionSaludMascota> InformacionSaludMascotas { get; set; }
        public DbSet<FotoMascota> FotosMascotas { get; set; }

        public DbSet<TipoReporte> TiposReportes { get; set; }
        public DbSet<EstadoReporte> EstadosReportes { get; set; }
        public DbSet<Reporte> Reportes { get; set; }
        public DbSet<FotoReporte> FotosReportes { get; set; }

        public DbSet<Avistamiento> Avistamientos { get; set; }
        public DbSet<FotoAvistamiento> FotosAvistamientos { get; set; }

        public DbSet<DispositivoGps> DispositivosGps { get; set; }
        public DbSet<UbicacionGps> UbicacionesGps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("postgis");

            base.OnModelCreating(modelBuilder);

            // Campos geográficos PostGIS
            modelBuilder.Entity<Reporte>()
                .Property(r => r.Ubicacion)
                .HasColumnType("geography (point, 4326)");

            modelBuilder.Entity<Avistamiento>()
                .Property(a => a.Ubicacion)
                .HasColumnType("geography (point, 4326)");

            modelBuilder.Entity<UbicacionGps>()
                .Property(u => u.Ubicacion)
                .HasColumnType("geography (point, 4326)");

            // Relación Usuario - Mascotas
            modelBuilder.Entity<Mascota>()
                .HasOne(m => m.Usuario)
                .WithMany(u => u.Mascotas)
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario - Reportes
            modelBuilder.Entity<Reporte>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reportes)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Mascota - Reportes
            modelBuilder.Entity<Reporte>()
                .HasOne(r => r.Mascota)
                .WithMany(m => m.Reportes)
                .HasForeignKey(r => r.MascotaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relación Reporte - Avistamientos
            modelBuilder.Entity<Avistamiento>()
                .HasOne(a => a.Reporte)
                .WithMany(r => r.Avistamientos)
                .HasForeignKey(a => a.ReporteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación Usuario - Avistamientos
            modelBuilder.Entity<Avistamiento>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Avistamientos)
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Información de salud: 1 a 1
            modelBuilder.Entity<InformacionSaludMascota>()
                .HasOne(i => i.Mascota)
                .WithOne(m => m.InformacionSalud)
                .HasForeignKey<InformacionSaludMascota>(i => i.MascotaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Una mascota no debe tener más de un reporte activo de pérdida
            modelBuilder.Entity<Reporte>()
                .HasIndex(r => r.MascotaId)
                .IsUnique()
                .HasFilter("\"MascotaId\" IS NOT NULL AND \"TipoReporteId\" = 1 AND \"EstadoReporteId\" = 1");

            // Índices espaciales
            modelBuilder.Entity<Reporte>()
                .HasIndex(r => r.Ubicacion)
                .HasMethod("gist");

            modelBuilder.Entity<Avistamiento>()
                .HasIndex(a => a.Ubicacion)
                .HasMethod("gist");

            modelBuilder.Entity<UbicacionGps>()
                .HasIndex(u => u.Ubicacion)
                .HasMethod("gist");

            // Datos iniciales
            modelBuilder.Entity<TipoReporte>().HasData(
                new TipoReporte { Id = 1, Nombre = "Perdida" },
                new TipoReporte { Id = 2, Nombre = "Encontrada" }
            );

            modelBuilder.Entity<EstadoReporte>().HasData(
                new EstadoReporte { Id = 1, Nombre = "Activo" },
                new EstadoReporte { Id = 2, Nombre = "Resuelto" },
                new EstadoReporte { Id = 3, Nombre = "Suspendido" }
            );
        }
    }
}
