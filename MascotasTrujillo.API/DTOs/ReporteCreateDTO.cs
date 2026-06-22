using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MascotasTrujillo.API.DTOs
{
    public class ReporteCreateDTO
    {
        public long? MascotaId { get; set; }

        [Required]
        public short TipoReporteId { get; set; } // 1 = Perdida, 2 = Encontrada

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

        [Required]
        public double Latitud { get; set; }

        [Required]
        public double Longitud { get; set; }

        public string? DireccionReferencia { get; set; }

        public IFormFile? Foto { get; set; }
    }
}
