using System.ComponentModel.DataAnnotations;

namespace MascotasTrujillo.API.Models
{
    public class TipoReporte
    {
        [Key]
        public short Id { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public List<Reporte> Reportes { get; set; } = new();
    }
}
