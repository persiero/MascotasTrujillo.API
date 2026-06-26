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

        public bool EsDuenoReporte { get; set; }

        public bool EsAutorAvistamiento { get; set; }

        public bool PuedeVerDetalle { get; set; }

        public bool PuedeContactar { get; set; }

        public string ResumenUbicacion =>
            string.IsNullOrWhiteSpace(DireccionReferencia)
                ? $"Lat. {Latitud:F5}, Long. {Longitud:F5}"
                : DireccionReferencia;

        public string FechaResumen =>
            FechaAvistamiento == default
                ? "Fecha no registrada"
                : $"📅 {FechaAvistamiento:dd/MM/yyyy HH:mm}";

        public string DetalleChipTexto =>
            PuedeVerDetalle
                ? "Detalle disponible"
                : "Detalle restringido";

        public string DetalleChipFondo =>
            PuedeVerDetalle
                ? "#DCFCE7"
                : "#F1F5F9";

        public string DetalleChipColor =>
            PuedeVerDetalle
                ? "#0F766E"
                : "#64748B";

        public string? NombreContacto { get; set; }

        public string? TelefonoContacto { get; set; }

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(Descripcion)
                ? "Sin descripción registrada."
                : Descripcion;

        public string ContactoMostrar =>
            string.IsNullOrWhiteSpace(NombreContacto)
                ? "Usuario no disponible"
                : NombreContacto;

        public string TelefonoMostrar =>
            string.IsNullOrWhiteSpace(TelefonoContacto)
                ? "Teléfono no registrado"
                : TelefonoContacto;

    }
}
