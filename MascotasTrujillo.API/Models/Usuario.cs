using Microsoft.AspNetCore.Identity;

namespace MascotasTrujillo.API.Models
{
    public class Usuario : IdentityUser<long>
    {
        // Solo agregamos los campos extra que Identity no trae por defecto
        public string NombreCompleto { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool EstaActivo { get; set; } = true;

        public string? FotoPerfilUrl { get; set; }

        public string? CodigoRecuperacionPassword { get; set; }
        public DateTime? CodigoRecuperacionExpira { get; set; }

        // Relaciones
        public List<Mascota> Mascotas { get; set; } = new();
        public List<Reporte> Reportes { get; set; } = new();
        public List<Avistamiento> Avistamientos { get; set; } = new();
    }
}
