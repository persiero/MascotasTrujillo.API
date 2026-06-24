namespace MascotasTrujillo.App.Models
{
    public class PerfilUsuario
    {
        public long Id { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}