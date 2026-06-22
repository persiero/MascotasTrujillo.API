using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;


namespace MascotasTrujillo.App.Views;

public partial class DetalleReportePage : ContentPage
{
    private readonly Reporte _reporteActual;

    private readonly ApiService _apiService;

    public DetalleReportePage(ApiService apiService, Reporte reporte)
    {
        InitializeComponent();

        _apiService = apiService;
        _reporteActual = reporte;
        BindingContext = reporte;

        CargarDatos();
    }

    private bool PuedeRegistrarAvistamiento()
    {
        string tipo = _reporteActual.TipoReporte?.Trim() ?? string.Empty;
        string estado = _reporteActual.EstadoReporte?.Trim() ?? string.Empty;

        bool esReportePerdida =
            tipo.Equals("Perdida", StringComparison.OrdinalIgnoreCase) ||
            tipo.Equals("Mascota perdida", StringComparison.OrdinalIgnoreCase) ||
            tipo.Contains("perdid", StringComparison.OrdinalIgnoreCase);

        bool estaActivo =
            estado.Equals("Activo", StringComparison.OrdinalIgnoreCase);

        return esReportePerdida && estaActivo;
    }

    private void CargarDatos()
    {
        if (string.IsNullOrWhiteSpace(_reporteActual.FotoUrl))
        {
            FotoReporteImage.Source = "https://cdn-icons-png.flaticon.com/512/616/616408.png";
        }

        if (_reporteActual.DistanciaMetros > 0)
        {
            LblDistancia.Text = $"A {_reporteActual.DistanciaMetros:N0} metros de tu ubicación.";
        }
        else
        {
            LblDistancia.Text = "Ubicación registrada en el reporte.";
        }

        LblNombreMascota.Text = TextoONoRegistrado(_reporteActual.NombreMascotaReferencial);
        LblEspecie.Text = TextoONoRegistrado(_reporteActual.EspecieReferencial);
        LblRaza.Text = TextoONoRegistrado(_reporteActual.RazaReferencial);
        LblColor.Text = TextoONoRegistrado(_reporteActual.ColorReferencial);
        LblSexo.Text = TextoONoRegistrado(_reporteActual.SexoReferencial);

        bool puedeRegistrarAvistamiento = PuedeRegistrarAvistamiento();

        BtnRegistrarAvistamiento.IsVisible = puedeRegistrarAvistamiento;
        LblAvistamientoNoDisponible.IsVisible = !puedeRegistrarAvistamiento;

        DisplayAlert(
            "DEBUG REPORTE",
            $"Tipo: {_reporteActual.TipoReporte}\nEstado: {_reporteActual.EstadoReporte}",
            "OK"
        );
    }



    private string TextoONoRegistrado(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor;
    }

    private async void OnVerUbicacionClicked(object sender, EventArgs e)
    {
        try
        {
            string url = $"https://www.google.com/maps/search/?api=1&query={_reporteActual.Latitud},{_reporteActual.Longitud}";
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la ubicación en el mapa.", "OK");
        }
    }

    private async void OnRegistrarAvistamientoClicked(object sender, EventArgs e)
    {
        if (!PuedeRegistrarAvistamiento())
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes registrar avistamientos en reportes activos de mascotas perdidas.",
                "OK"
            );

            return;
        }

        await Navigation.PushAsync(new RegistrarAvistamientoPage(_apiService, _reporteActual));
    }

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        try
        {
            // Número temporal para pruebas.
            // Luego lo ideal será traer el teléfono del usuario que creó el reporte desde la API.
            string numeroTelefono = "51915391298";

            string mensaje = $"¡Hola! Vi tu reporte '{_reporteActual.Titulo}' en Mascotas Trujillo. ¿Me puedes brindar más información?";

            string mensajeCodificado = Uri.EscapeDataString(mensaje);
            string url = $"https://wa.me/{numeroTelefono}?text={mensajeCodificado}";

            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudo abrir WhatsApp. Verifica si está instalado en este dispositivo.",
                "OK"
            );
        }
    }

}