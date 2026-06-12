using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries; // IMPORTANTE: Para usar Point

namespace MascotasTrujillo.API.Models
{
    public class Mascota
    {
        [Key]
        public int Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Especie { get; set; } // Perro, Gato

        [MaxLength(50)]
        public string? Raza { get; set; }

        [MaxLength(30)]
        public string? ColorPrincipal { get; set; }

        public string? RasgosParticulares { get; set; }
        public string? FotoPerfilUrl { get; set; }

        // ==========================================
        // NUEVOS CAMPOS PARA EL MONITOREO GPS / IoT
        // ==========================================

        [MaxLength(100)]
        public string? DispositivoId { get; set; } // IMEI o ID único del collar

        public Point? UltimaUbicacion { get; set; } // Coordenada geográfica exacta (PostGIS)

        public DateTime? UltimaActualizacion { get; set; } // Cuándo mandó señal por última vez
    }
}
