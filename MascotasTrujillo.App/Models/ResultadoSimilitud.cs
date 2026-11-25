namespace MascotasTrujillo.App.Models
{
    public class ResultadoSimilitud
    {
        public long ReporteId { get; set; }

        public string TituloReporte { get; set; } = string.Empty;

        public string? NombreMascota { get; set; }

        public string? Especie { get; set; }

        public string? FotoOriginalUrl { get; set; }

        public bool Exito { get; set; }

        public double PorcentajeSimilitud { get; set; }

        public string NivelSimilitud { get; set; } = string.Empty;

        public string Titulo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public string Metodo { get; set; } = string.Empty;

        public string Advertencia { get; set; } = string.Empty;
    }
}