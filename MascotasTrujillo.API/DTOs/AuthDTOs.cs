using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.DTOs
{
    public class RegistroDTO
    {
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? Telefono { get; set; }
    }

    public class LoginDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
