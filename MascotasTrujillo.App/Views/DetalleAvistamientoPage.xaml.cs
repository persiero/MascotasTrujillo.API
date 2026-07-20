using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class DetalleAvistamientoPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly long _avistamientoId;

    private Avistamiento? _avistamiento;

    public DetalleAvistamientoPage(ApiService apiService, long avistamientoId)
    {
        InitializeComponent();

        _apiService = apiService;
        _avistamientoId = avistamientoId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDetalleAsync();
    }

    private async Task CargarDetalleAsync()
    {
        try
        {
            var detalle = await _apiService.ObtenerDetalleAvistamientoAsync(_avistamientoId);

            if (detalle == null)
            {
                await DisplayAlertAsync(
                    "No disponible",
                    "No se pudo cargar el detalle del avistamiento o no tienes permiso para verlo.",
                    "OK"
                );

                await Navigation.PopAsync();
                return;
            }

            _avistamiento = detalle;

            LblReporteTitulo.Text = string.IsNullOrWhiteSpace(detalle.ReporteTitulo)
                ? "Información registrada por la comunidad."
                : $"Reporte: {detalle.ReporteTitulo}";

            FotoAvistamientoImage.Source = detalle.FotoMostrar;

            LblDescripcion.Text = detalle.DescripcionMostrar;
            LblDireccion.Text = detalle.ResumenUbicacion;
            LblFecha.Text = detalle.FechaResumen;
            LblCoordenadas.Text = $"Lat. {detalle.Latitud:F6}, Long. {detalle.Longitud:F6}";

            LblNombreContacto.Text = detalle.ContactoMostrar;
            LblTelefonoContacto.Text = detalle.TelefonoMostrar;

            BtnWhatsAppAvistamiento.IsVisible = detalle.PuedeContactar;
            LblContactoNoDisponible.IsVisible = !detalle.PuedeContactar;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo cargar el detalle: {ex.Message}",
                "OK"
            );
        }
    }

    private async void OnVerUbicacionClicked(object sender, EventArgs e)
    {
        if (_avistamiento == null)
            return;

        try
        {
            string url = $"https://www.google.com/maps/search/?api=1&query={_avistamiento.Latitud},{_avistamiento.Longitud}";
            await Launcher.Default.OpenAsync(url);
        }
        catch
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudo abrir la ubicación en el mapa.",
                "OK"
            );
        }
    }

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        if (_avistamiento == null)
            return;

        if (!_avistamiento.PuedeContactar)
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo el dueño del reporte puede contactar al usuario que registró el avistamiento.",
                "OK"
            );

            return;
        }

        try
        {
            string numeroTelefono = LimpiarNumeroWhatsapp(_avistamiento.TelefonoContacto);

            if (string.IsNullOrWhiteSpace(numeroTelefono))
            {
                await DisplayAlertAsync(
                    "Teléfono no disponible",
                    "El usuario que registró este avistamiento no tiene un número de WhatsApp registrado.",
                    "OK"
                );

                return;
            }

            string nombreContacto = string.IsNullOrWhiteSpace(_avistamiento.NombreContacto)
                ? "el usuario"
                : _avistamiento.NombreContacto;

            string mensaje =
                $"¡Hola {nombreContacto}! Soy el dueño del reporte \"{_avistamiento.ReporteTitulo}\" en Mascotas Trujillo. Gracias por registrar un avistamiento. ¿Podrías brindarme más detalles?";

            string mensajeCodificado = Uri.EscapeDataString(mensaje);
            string url = $"https://wa.me/{numeroTelefono}?text={mensajeCodificado}";

            await Launcher.Default.OpenAsync(url);
        }
        catch
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudo abrir WhatsApp. Verifica si está instalado en este dispositivo.",
                "OK"
            );
        }
    }

    private string LimpiarNumeroWhatsapp(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return string.Empty;

        string numeroLimpio = new string(
            telefono.Where(char.IsDigit).ToArray()
        );

        if (string.IsNullOrWhiteSpace(numeroLimpio))
            return string.Empty;

        if (numeroLimpio.Length == 9)
            numeroLimpio = "51" + numeroLimpio;

        return numeroLimpio;
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await VolverAsync();
    }

    private async Task VolverAsync()
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
            return;
        }

        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }
}