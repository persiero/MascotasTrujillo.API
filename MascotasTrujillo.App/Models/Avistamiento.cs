using System;
using System.Collections.Generic;
using System.Text;

namespace MascotasTrujillo.App.Models
{
    public class Avistamiento
    {
        public int Id { get; set; }
        public string FotoUrl { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public double DistanciaMetros { get; set; } // ¡El cálculo mágico de PostGIS!
    }
}
