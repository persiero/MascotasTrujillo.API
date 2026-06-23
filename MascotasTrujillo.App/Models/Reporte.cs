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

        public string? FotoUrl { get; set; }

        public DateTime FechaReporte { get; set; }
        public DateTime? FechaResolucion { get; set; }

        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public double DistanciaMetros { get; set; }

        public bool EstaActivo =>
            string.Equals(EstadoReporte, "Activo", StringComparison.OrdinalIgnoreCase);

        public bool EstaResuelto =>
            string.Equals(EstadoReporte, "Resuelto", StringComparison.OrdinalIgnoreCase);

        public bool EstaSuspendido =>
            string.Equals(EstadoReporte, "Suspendido", StringComparison.OrdinalIgnoreCase);

        public bool PuedeResolver => EstaActivo;

        public bool PuedeSuspender => EstaActivo;

        public bool PuedeReactivar => EstaSuspendido;
    }
}
