using System;
using System.Collections.Generic;
using System.Text;

namespace MascotasTrujillo.App.Models
{
    public class Mascota
    {
        public long Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Especie { get; set; }
        public string? Raza { get; set; }
        public string? ColorPrincipal { get; set; }
        public string? Sexo { get; set; }
        public string? EdadAproximada { get; set; }
        public string? RasgosParticulares { get; set; }

        public string? Enfermedades { get; set; }
        public string? Discapacidades { get; set; }
        public string? Tratamientos { get; set; }
        public string? NecesidadesEspeciales { get; set; }
        public string? ObservacionesSalud { get; set; }

        public bool TieneInformacionSalud =>
            !string.IsNullOrWhiteSpace(Enfermedades) ||
            !string.IsNullOrWhiteSpace(Discapacidades) ||
            !string.IsNullOrWhiteSpace(Tratamientos) ||
            !string.IsNullOrWhiteSpace(NecesidadesEspeciales) ||
            !string.IsNullOrWhiteSpace(ObservacionesSalud);

        public string? FotoPerfilUrl { get; set; }

        public string? DispositivoId { get; set; }
        public DateTime? UltimaActualizacion { get; set; }

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public string EstadoGpsTexto =>
            string.IsNullOrWhiteSpace(DispositivoId)
                ? "Sin GPS asociado"
                : $"GPS: {DispositivoId}";
    }
}
