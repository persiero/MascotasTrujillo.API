using System;
using System.Collections.Generic;
using System.Text;

namespace MascotasTrujillo.App.Models
{
    public class Avistamiento
    {
        public long Id { get; set; }

        public long ReporteId { get; set; }
        public long UsuarioId { get; set; }

        public string? ReporteTitulo { get; set; }

        public string? Descripcion { get; set; }
        public string? DireccionReferencia { get; set; }

        public DateTime FechaAvistamiento { get; set; }

        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public string? FotoUrl { get; set; }

        public string FotoMostrar =>
            string.IsNullOrWhiteSpace(FotoUrl)
                ? "https://cdn-icons-png.flaticon.com/512/616/616408.png"
                : FotoUrl;
    }
}
