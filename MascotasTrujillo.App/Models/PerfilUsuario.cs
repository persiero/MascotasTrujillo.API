namespace MascotasTrujillo.App.Models
{
    public class PerfilUsuario
    {
        public long Id { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? FotoPerfilUrl { get; set; }

        public DateTime FechaRegistro { get; set; }

        public string FotoMostrar =>
            string.IsNullOrWhiteSpace(FotoPerfilUrl)
                ? "https://cdn-icons-png.flaticon.com/512/149/149071.png"
                : FotoPerfilUrl;
    }
}