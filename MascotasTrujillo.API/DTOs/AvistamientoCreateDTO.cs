using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.DTOs
{
    public class AvistamientoCreateDTO
    {
        [Required]
        public long ReporteId { get; set; }

        public string? Descripcion { get; set; }

        [Required]
        public double Latitud { get; set; }

        [Required]
        public double Longitud { get; set; }

        public string? DireccionReferencia { get; set; }

        public IFormFile? Foto { get; set; }
    }
}
