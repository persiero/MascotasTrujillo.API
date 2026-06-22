using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries; // IMPORTANTE: Para usar Point

namespace MascotasTrujillo.API.Models
{
    public class Mascota
    {
        [Key]
        public long Id { get; set; }

        public long UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Especie { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Raza { get; set; }

        [MaxLength(30)]
        public string? ColorPrincipal { get; set; }

        [MaxLength(20)]
        public string? Sexo { get; set; }

        [MaxLength(50)]
        public string? EdadAproximada { get; set; }

        public string? RasgosParticulares { get; set; }

        public bool EstaActiva { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relaciones
        public InformacionSaludMascota? InformacionSalud { get; set; }
        public List<FotoMascota> Fotos { get; set; } = new();
        public List<Reporte> Reportes { get; set; } = new();
        public List<DispositivoGps> DispositivosGps { get; set; } = new();
    }
}
