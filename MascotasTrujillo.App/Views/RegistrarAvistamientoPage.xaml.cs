using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RegistrarAvistamientoPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Reporte _reporte;
    private FileResult? _fotoCapturada;

    public RegistrarAvistamientoPage(ApiService apiService, Reporte reporte)
    {
        InitializeComponent();

        _apiService = apiService;
        _reporte = reporte;

        LblReporteTitulo.Text = $"Reporte: {_reporte.Titulo}";
    }

    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync("No disponible", "La cámara no está soportada en este dispositivo.", "OK");
                return;
            }

            var foto = await MediaPicker.Default.CapturePhotoAsync();
            await ProcesarFoto(foto);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo abrir la cámara: {ex.Message}", "OK");
        }
    }

    private async void OnElegirGaleriaClicked(object sender, EventArgs e)
    {
        try
        {
            var fotos = await MediaPicker.Default.PickPhotosAsync();
            var foto = fotos?.FirstOrDefault();

            await ProcesarFoto(foto);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo acceder a la galería: {ex.Message}", "OK");
        }
    }

    private async Task ProcesarFoto(FileResult? foto)
    {
        if (foto == null)
            return;

        _fotoCapturada = foto;

        var stream = await foto.OpenReadAsync();
        FotoPreview.Source = ImageSource.FromStream(() => stream);

        BotonesCaptura.IsVisible = false;
    }

    private async void OnGuardarAvistamientoClicked(object sender, EventArgs e)
    {
        string descripcion = DescripcionEditor.Text?.Trim() ?? string.Empty;
        string direccionReferencia = DireccionReferenciaEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            await DisplayAlertAsync("Dato requerido", "Ingresa una descripción del avistamiento.", "OK");
            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnGuardarAvistamiento.IsEnabled = false;
        BtnGuardarAvistamiento.Text = "Obteniendo GPS y guardando...";

        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
            );

            if (location == null)
            {
                await DisplayAlertAsync("Error GPS", "No se pudo obtener tu ubicación actual.", "OK");
                return;
            }

            var resultado = await _apiService.RegistrarAvistamientoAsync(
                reporteId: _reporte.Id,
                descripcion: descripcion,
                latitud: location.Latitude,
                longitud: location.Longitude,
                direccionReferencia: direccionReferencia,
                foto: _fotoCapturada
            );

            if (resultado.Exito)
            {
                await DisplayAlertAsync(
                    "¡Gracias!",
                    "Tu avistamiento fue registrado correctamente.",
                    "OK"
                );

                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", resultado.Mensaje, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo registrar el avistamiento: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            BtnGuardarAvistamiento.IsEnabled = true;
            BtnGuardarAvistamiento.Text = "📍 Guardar avistamiento";
        }
    }
}