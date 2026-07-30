using MascotasTrujillo.App.Helpers;
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

        public DateTime? UltimaActualizacionLocal =>
            FechaHoraHelper.ConvertirALocal(UltimaActualizacion);

        public string UltimaActualizacionTexto =>
            FechaHoraHelper.FormatearFechaHoraLocal(UltimaActualizacion, "Sin actualización GPS");

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public string EstadoGpsTexto =>
            string.IsNullOrWhiteSpace(DispositivoId)
                ? "Sin GPS asociado"
                : $"GPS: {DispositivoId}";

        public string FotoMostrar =>
    string.IsNullOrWhiteSpace(FotoPerfilUrl)
        ? "https://cdn-icons-png.flaticon.com/512/616/616408.png"
        : FotoPerfilUrl;

        public bool TieneUbicacionGps =>
            Latitud.HasValue && Longitud.HasValue;

        public string EstadoGpsVisual
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DispositivoId))
                    return "Sin GPS";

                if (TieneUbicacionGps && BateriaGps.HasValue)
                    return $"GPS activo · {BateriaGps.Value:0}%";

                if (TieneUbicacionGps)
                    return "GPS activo";

                return "GPS sin ubicación";
            }
        }

        public string EstadoGpsFondo =>
            string.IsNullOrWhiteSpace(DispositivoId)
                ? "#F1F5F9"
                : TieneUbicacionGps
                    ? "#DCFCE7"
                    : "#FEF3C7";

        public string EstadoGpsColor =>
            string.IsNullOrWhiteSpace(DispositivoId)
                ? "#64748B"
                : TieneUbicacionGps
                    ? "#0F766E"
                    : "#92400E";

        public string? EstadoConexionGps { get; set; }

        public decimal? BateriaGps { get; set; }

        public string UltimaUbicacionTexto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DispositivoId))
                    return "Esta mascota no tiene un collar GPS asociado.";

                if (!TieneUbicacionGps)
                    return "El collar GPS está asociado, pero aún no registra ubicación.";

                string bateriaTexto = BateriaGps.HasValue
                    ? $" · Batería: {BateriaGps.Value:0}%"
                    : string.Empty;

                if (UltimaActualizacion.HasValue)
                    return $"Última ubicación GPS: {UltimaActualizacionTexto}{bateriaTexto}";

                return $"Última ubicación GPS registrada{bateriaTexto}";
            }
        }

    }
}
