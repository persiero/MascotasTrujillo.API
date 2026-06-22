using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // IMPORTANTE: Para el manejo de archivos

namespace MascotasTrujillo.API.DTOs
{
    public class MascotaCreateDTO
    {
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

        [MaxLength(100)]
        public string? DispositivoId { get; set; }

        public IFormFile? Foto { get; set; }
    }
}
