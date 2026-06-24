using System.Text.Json.Serialization;

namespace MascotasTrujillo.App.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expiracion")]
        public DateTime Expiracion { get; set; }

        [JsonPropertyName("usuarioId")]
        public long UsuarioId { get; set; }

        [JsonPropertyName("nombreCompleto")]
        public string? NombreCompleto { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }
    }
}
