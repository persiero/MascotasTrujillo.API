using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace MascotasTrujillo.API.Models
{
    public class Avistamiento
    {
        [Key]
        public long Id { get; set; }

        public long ReporteId { get; set; }
        public Reporte? Reporte { get; set; }

        public long UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string? Descripcion { get; set; }

        public Point Ubicacion { get; set; } = null!;

        public string? DireccionReferencia { get; set; }

        public DateTime FechaAvistamiento { get; set; } = DateTime.UtcNow;

        public bool Visible { get; set; } = true;

        public List<FotoAvistamiento> Fotos { get; set; } = new();
    }
}
