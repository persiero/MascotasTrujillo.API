using MascotasTrujillo.App.Helpers;
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

        public string? NombreContacto { get; set; }
        public string? TelefonoContacto { get; set; }

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

        public DateTime FechaReporteLocal =>
            FechaHoraHelper.ConvertirALocal(FechaReporte);

        public DateTime? FechaResolucionLocal =>
            FechaHoraHelper.ConvertirALocal(FechaResolucion);

        public string FechaReporteTexto =>
            FechaHoraHelper.FormatearFechaHoraLocal(FechaReporte);

        public string FechaResolucionTexto =>
            FechaHoraHelper.FormatearFechaHoraLocal(FechaResolucion, "Sin fecha de resolución");

        public string FechaResumen =>
            FechaHoraHelper.FormatearFechaHoraLocal(FechaReporte);

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

        public string TipoReporteFondo
        {
            get
            {
                string tipo = TipoReporte?.Trim() ?? string.Empty;

                if (tipo.Contains("perdid", StringComparison.OrdinalIgnoreCase))
                    return "#FEE2E2";

                if (tipo.Contains("encontrad", StringComparison.OrdinalIgnoreCase))
                    return "#DCFCE7";

                return "#EEF2FF";
            }
        }

        public string TipoReporteTexto
        {
            get
            {
                string tipo = TipoReporte?.Trim() ?? string.Empty;

                if (tipo.Contains("perdid", StringComparison.OrdinalIgnoreCase))
                    return "#991B1B";

                if (tipo.Contains("encontrad", StringComparison.OrdinalIgnoreCase))
                    return "#0F766E";

                return "#2B0B98";
            }
        }

        public string EstadoReporteFondo
        {
            get
            {
                string estado = EstadoReporte?.Trim() ?? string.Empty;

                if (estado.Equals("Activo", StringComparison.OrdinalIgnoreCase))
                    return "#DCFCE7";

                if (estado.Equals("Resuelto", StringComparison.OrdinalIgnoreCase))
                    return "#E0F2FE";

                if (estado.Equals("Suspendido", StringComparison.OrdinalIgnoreCase))
                    return "#FEF3C7";

                return "#EEF2FF";
            }
        }

        public string EstadoReporteTexto
        {
            get
            {
                string estado = EstadoReporte?.Trim() ?? string.Empty;

                if (estado.Equals("Activo", StringComparison.OrdinalIgnoreCase))
                    return "#0F766E";

                if (estado.Equals("Resuelto", StringComparison.OrdinalIgnoreCase))
                    return "#0369A1";

                if (estado.Equals("Suspendido", StringComparison.OrdinalIgnoreCase))
                    return "#92400E";

                return "#2B0B98";
            }
        }

        public string FotoMostrar =>
            string.IsNullOrWhiteSpace(FotoUrl)
                ? "https://cdn-icons-png.flaticon.com/512/616/616408.png"
                : FotoUrl;

    }
}
