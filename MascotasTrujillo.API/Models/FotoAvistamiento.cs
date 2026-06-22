using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.Models
{
    public class FotoAvistamiento
    {
        [Key]
        public long Id { get; set; }

        public long AvistamientoId { get; set; }
        public Avistamiento? Avistamiento { get; set; }

        [Required]
        public string UrlFoto { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
