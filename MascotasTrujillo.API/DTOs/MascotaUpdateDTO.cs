using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MascotasTrujillo.API.DTOs
{
    public class MascotaUpdateDTO
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

        public string? Enfermedades { get; set; }
        public string? Discapacidades { get; set; }
        public string? Tratamientos { get; set; }
        public string? NecesidadesEspeciales { get; set; }
        public string? ObservacionesSalud { get; set; }

        public IFormFile? Foto { get; set; }
    }
}