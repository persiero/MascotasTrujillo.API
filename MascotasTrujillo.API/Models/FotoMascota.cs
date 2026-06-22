using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.Models
{
    public class FotoMascota
    {
        [Key]
        public long Id { get; set; }

        public long MascotaId { get; set; }
        public Mascota? Mascota { get; set; }

        [Required]
        public string UrlFoto { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; } = false;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
