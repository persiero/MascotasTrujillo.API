using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.Models
{
    public class FotoReporte
    {
        [Key]
        public long Id { get; set; }

        public long ReporteId { get; set; }
        public Reporte? Reporte { get; set; }

        [Required]
        public string UrlFoto { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
