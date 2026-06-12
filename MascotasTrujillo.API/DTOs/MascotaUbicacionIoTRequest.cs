using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.DTOs
{
    public class MascotaUbicacionIoTRequest
    {
        [Required]
        public string DispositivoId { get; set; } = string.Empty; // Identifica de quién es el collar

        [Required]
        public double Latitud { get; set; }

        [Required]
        public double Longitud { get; set; }
    }
}
