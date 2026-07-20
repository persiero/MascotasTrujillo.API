using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MascotasTrujillo.API.DTOs
{
    public class PerfilUsuarioDTO
    {
        public long Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? FotoPerfilUrl { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class ActualizarPerfilDTO
    {
        [Required]
        [MaxLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }
    }

    public class CambiarPasswordDTO
    {
        [Required]
        public string PasswordActual { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string PasswordNuevo { get; set; } = string.Empty;

        [Required]
        public string ConfirmarPasswordNuevo { get; set; } = string.Empty;
    }

    public class ActualizarFotoPerfilDTO
    {
        [Required]
        public IFormFile Foto { get; set; } = null!;
    }
}