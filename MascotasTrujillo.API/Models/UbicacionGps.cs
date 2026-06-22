using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace MascotasTrujillo.API.Models
{
    public class UbicacionGps
    {
        [Key]
        public long Id { get; set; }

        public long DispositivoGpsId { get; set; }
        public DispositivoGps? DispositivoGps { get; set; }

        public Point Ubicacion { get; set; } = null!;

        public decimal? Bateria { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
