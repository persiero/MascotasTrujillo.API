namespace MascotasTrujillo.App.Helpers
{
    public static class FechaHoraHelper
    {
        public static DateTime? ConvertirALocal(DateTime? fechaUtc)
        {
            if (!fechaUtc.HasValue)
                return null;

            return ConvertirALocal(fechaUtc.Value);
        }

        public static DateTime ConvertirALocal(DateTime fechaUtc)
        {
            if (fechaUtc.Kind == DateTimeKind.Local)
                return fechaUtc;

            if (fechaUtc.Kind == DateTimeKind.Utc)
                return fechaUtc.ToLocalTime();

            // Si la API/BD devuelve DateTime sin zona horaria,
            // asumimos que viene guardado en UTC.
            return DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc).ToLocalTime();
        }

        public static string FormatearFechaHoraLocal(
            DateTime? fechaUtc,
            string textoSinFecha = "Sin fecha")
        {
            DateTime? fechaLocal = ConvertirALocal(fechaUtc);

            if (!fechaLocal.HasValue)
                return textoSinFecha;

            return fechaLocal.Value.ToString("dd/MM/yyyy HH:mm");
        }

        public static string FormatearFechaHoraLocal(DateTime fechaUtc)
        {
            DateTime fechaLocal = ConvertirALocal(fechaUtc);
            return fechaLocal.ToString("dd/MM/yyyy HH:mm");
        }

        public static string FormatearFechaLocal(
            DateTime? fechaUtc,
            string textoSinFecha = "Sin fecha")
        {
            DateTime? fechaLocal = ConvertirALocal(fechaUtc);

            if (!fechaLocal.HasValue)
                return textoSinFecha;

            return fechaLocal.Value.ToString("dd/MM/yyyy");
        }

        public static string FormatearHoraLocal(
            DateTime? fechaUtc,
            string textoSinFecha = "Sin hora")
        {
            DateTime? fechaLocal = ConvertirALocal(fechaUtc);

            if (!fechaLocal.HasValue)
                return textoSinFecha;

            return fechaLocal.Value.ToString("HH:mm");
        }
    }
}