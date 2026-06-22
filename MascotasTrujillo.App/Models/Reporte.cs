using System;
using System.Collections.Generic;
using System.Text;

namespace MascotasTrujillo.App.Models
{
    public class Reporte
    {
        public long Id { get; set; }

        public long UsuarioId { get; set; }
        public long? MascotaId { get; set; }

        public string? TipoReporte { get; set; }
        public string? EstadoReporte { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public string? NombreMascotaReferencial { get; set; }
        public string? EspecieReferencial { get; set; }
        public string? RazaReferencial { get; set; }
        public string? ColorReferencial { get; set; }
        public string? SexoReferencial { get; set; }

        public string? FotoUrl { get; set; }

        public DateTime FechaReporte { get; set; }
        public DateTime? FechaResolucion { get; set; }

        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public double DistanciaMetros { get; set; }
    }
}
