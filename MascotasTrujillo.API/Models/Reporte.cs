using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace MascotasTrujillo.API.Models
{
    public class Reporte
    {
        [Key]
        public long Id { get; set; }

        public long UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public long? MascotaId { get; set; }
        public Mascota? Mascota { get; set; }

        public short TipoReporteId { get; set; }
        public TipoReporte? TipoReporte { get; set; }

        public short EstadoReporteId { get; set; } = 1;
        public EstadoReporte? EstadoReporte { get; set; }

        [Required, MaxLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        // Datos referenciales para reportes comunitarios independientes
        [MaxLength(100)]
        public string? NombreMascotaReferencial { get; set; }

        [MaxLength(30)]
        public string? EspecieReferencial { get; set; }

        [MaxLength(50)]
        public string? RazaReferencial { get; set; }

        [MaxLength(30)]
        public string? ColorReferencial { get; set; }

        [MaxLength(20)]
        public string? SexoReferencial { get; set; }

        public Point Ubicacion { get; set; } = null!;

        public string? DireccionReferencia { get; set; }

        public DateTime FechaReporte { get; set; } = DateTime.UtcNow;

        public DateTime? FechaResolucion { get; set; }

        public bool Visible { get; set; } = true;

        // Relaciones
        public List<FotoReporte> Fotos { get; set; } = new();
        public List<Avistamiento> Avistamientos { get; set; } = new();
    }
}