using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.Models
{
    public class DispositivoGps
    {
        [Key]
        public long Id { get; set; }

        public long MascotaId { get; set; }
        public Mascota? Mascota { get; set; }

        [Required, MaxLength(100)]
        public string CodigoDispositivo { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NombreDispositivo { get; set; }

        [MaxLength(20)]
        public string EstadoConexion { get; set; } = "Desconectado";

        public bool Activo { get; set; } = true;

        public DateTime FechaAsociacion { get; set; } = DateTime.UtcNow;

        public List<UbicacionGps> Ubicaciones { get; set; } = new();
    }
}
