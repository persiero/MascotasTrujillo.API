using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // IMPORTANTE: Para el manejo de archivos

namespace MascotasTrujillo.API.DTOs
{
    public class MascotaCreateDTO
    {
        [Required, MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Especie { get; set; }

        [MaxLength(50)]
        public string? Raza { get; set; }

        [MaxLength(30)]
        public string? ColorPrincipal { get; set; }

        public string? RasgosParticulares { get; set; }

        // NUEVO: Para vincular el hardware desde el registro inicial
        [MaxLength(100)]
        public string? DispositivoId { get; set; }

        // NUEVO: Archivo de imagen de la mascota para procesar en R2StorageService
        public IFormFile? Foto { get; set; }
    }
}
