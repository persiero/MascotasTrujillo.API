using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace MascotasTrujillo.API.Models
{
    public class Avistamiento
    {
        [Key]
        public int Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; } // Quién lo reporta

        public string FotoUrl { get; set; } = string.Empty; // Asumimos que la foto es obligatoria// URL de Amazon/Cloudflare R2
        public string? ThumbnailUrl { get; set; }

        public string? Descripcion { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        // ESTE ES EL CAMPO GEOGRÁFICO
        public Point Ubicacion { get; set; } = null!; // null! le dice al compilador: "Tranquilo, yo me encargo de que esto nunca sea nulo al guardar"
    }
}
