using Microsoft.AspNetCore.Identity;

namespace MascotasTrujillo.API.Models
{
    public class Usuario : IdentityUser
    {
        // Solo agregamos los campos extra que Identity no trae por defecto
        public string NombreCompleto { get; set; } = String.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relaciones
        public List<Mascota> Mascotas { get; set; } = new List<Mascota>();
        public List<Avistamiento> Avistamientos { get; set; } = new List<Avistamiento>();
    }
}
