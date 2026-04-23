using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.DTOs
{
    public class AvistamientoCreateDTO
    {        
        [Required]
        public IFormFile Foto { get; set; } = null!;

        public string? Descripcion { get; set; }

        [Required]
        public double Latitud { get; set; }

        [Required]
        public double Longitud { get; set; }
    }
}
