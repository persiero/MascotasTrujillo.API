namespace MascotasTrujillo.App.Models
{
    public class UbicacionGpsHistorial
    {
        public long Id { get; set; }

        public long DispositivoGpsId { get; set; }

        public string? CodigoDispositivo { get; set; }

        public decimal? Bateria { get; set; }

        public DateTime FechaRegistro { get; set; }

        public double Latitud { get; set; }

        public double Longitud { get; set; }

        public string FechaTexto =>
            FechaRegistro == default
                ? "Fecha no registrada"
                : $"📅 {FechaRegistro:dd/MM/yyyy HH:mm}";

        public string BateriaTexto =>
            Bateria.HasValue
                ? $"🔋 Batería: {Bateria.Value:0}%"
                : "🔋 Batería no registrada";

        public string CoordenadasTexto =>
            $"Lat. {Latitud:F6}, Long. {Longitud:F6}";

        public string CodigoDispositivoTexto =>
            string.IsNullOrWhiteSpace(CodigoDispositivo)
                ? "Dispositivo GPS"
                : $"GPS: {CodigoDispositivo}";
    }
}