using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.Models
{
    public class InformacionSaludMascota
    {
        [Key]
        public long Id { get; set; }

        public long MascotaId { get; set; }
        public Mascota? Mascota { get; set; }

        public string? Enfermedades { get; set; }
        public string? Discapacidades { get; set; }
        public string? Tratamientos { get; set; }
        public string? NecesidadesEspeciales { get; set; }
        public string? Observaciones { get; set; }
    }
}
