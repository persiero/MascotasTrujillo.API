using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using Microsoft.Maui.Devices.Sensors;

namespace MascotasTrujillo.App.Views;

public partial class RegistrarAvistamientoPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Reporte _reporte;
    private FileResult? _fotoCapturada;

    private double? _latitudAvistamiento;
    private double? _longitudAvistamiento;

    private TaskCompletionSource<bool>? _confirmacionSalirTcs;
    private bool _procesandoSalida = false;

    private readonly double _latitudTrujillo = -8.1118;
    private readonly double _longitudTrujillo = -79.0287;
        

    private async void OnUsarGpsActualClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10)
                )
            );

            if (location == null)
            {
                await DisplayAlertAsync("GPS", "No se pudo obtener tu ubicación actual.", "OK");
                return;
            }

            SeleccionarUbicacionAvistamiento(
                location.Latitude,
                location.Longitude,
                "Se usará tu ubicación GPS actual para el avistamiento."
            );
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("GPS", $"No se pudo obtener la ubicación: {ex.Message}", "OK");
        }
    }

    private async void OnSeleccionarMapaClicked(object sender, EventArgs e)
    {
        double latitudInicial = _latitudAvistamiento ?? _latitudTrujillo;
        double longitudInicial = _longitudAvistamiento ?? _longitudTrujillo;

        await Navigation.PushAsync(
            new SeleccionarUbicacionPage(
                latitudInicial,
                longitudInicial,
                (latitud, longitud) =>
                {
                    SeleccionarUbicacionAvistamiento(
                        latitud,
                        longitud,
                        "Ubicación seleccionada manualmente en el mapa."
                    );
                },
                "Seleccionar ubicación del avistamiento"
            )
        );
    }

    private void SeleccionarUbicacionAvistamiento(double latitud, double longitud, string mensaje)
    {
        _latitudAvistamiento = latitud;
        _longitudAvistamiento = longitud;

        LblUbicacionSeleccionada.Text =
            $"{mensaje}\nLat. {latitud:F6}, Long. {longitud:F6}";
    }

    public RegistrarAvistamientoPage(ApiService apiService, Reporte reporte)
    {
        InitializeComponent();

        _apiService = apiService;
        _reporte = reporte;

        LblReporteTitulo.Text = $"Avistamiento para: {_reporte.Titulo}";
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

        try
        {
            var resultado = await ImageCompressionService.ComprimirFileResultAsync(
                foto,
                prefijoArchivo: "avistamiento",
                maxMb: 5
            );

            // Reemplazamos la foto original por la comprimida.
            _fotoCapturada = new FileResult(resultado.RutaLocal, "image/jpeg");

            FotoPreview.Source = ImageSource.FromStream(
                () => new MemoryStream(resultado.Bytes)
            );

            BotonesCaptura.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error con la imagen",
                $"No se pudo preparar la foto del avistamiento.\n\nDetalle:\n{ex.Message}",
                "OK"
            );
        }
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
        BtnGuardarAvistamiento.Text = "Publicando avistamiento...";

        try
        {            

            if (!_latitudAvistamiento.HasValue || !_longitudAvistamiento.HasValue)
            {
                await DisplayAlertAsync(
                    "Ubicación requerida",
                    "Selecciona la ubicación del avistamiento usando tu GPS o tocando el mapa.",
                    "OK"
                );

                return;
            }

            var resultado = await _apiService.RegistrarAvistamientoAsync(
                reporteId: _reporte.Id,
                descripcion: descripcion,
                latitud: _latitudAvistamiento.Value,
                longitud: _longitudAvistamiento.Value,
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
            BtnGuardarAvistamiento.Text = "📌 Publicar avistamiento";
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await IntentarVolverAsync();
    }

    private async Task IntentarVolverAsync()
    {
        if (_procesandoSalida)
            return;

        _procesandoSalida = true;

        try
        {
            if (HayDatosSinGuardar())
            {
                bool salir = await MostrarConfirmacionSalirAsync();

                if (!salir)
                    return;
            }

            await VolverAsync();
        }
        finally
        {
            _procesandoSalida = false;
        }
    }

    private async Task<bool> MostrarConfirmacionSalirAsync()
    {
        if (SalirSinGuardarOverlay.IsVisible)
            return false;

        _confirmacionSalirTcs = new TaskCompletionSource<bool>();

        SalirSinGuardarOverlay.IsVisible = true;
        SalirSinGuardarOverlay.Opacity = 0;

        await SalirSinGuardarOverlay.FadeToAsync(1, 150);

        return await _confirmacionSalirTcs.Task;
    }

    private async Task CerrarConfirmacionSalirAsync(bool confirmarSalida)
    {
        await SalirSinGuardarOverlay.FadeToAsync(0, 120);

        SalirSinGuardarOverlay.IsVisible = false;
        SalirSinGuardarOverlay.Opacity = 0;

        _confirmacionSalirTcs?.TrySetResult(confirmarSalida);
        _confirmacionSalirTcs = null;
    }

    private async void OnCancelarSalirClicked(object sender, EventArgs e)
    {
        await CerrarConfirmacionSalirAsync(false);
    }

    private async void OnConfirmarSalirClicked(object sender, EventArgs e)
    {
        await CerrarConfirmacionSalirAsync(true);
    }

    private async void OnCancelarSalirOverlayTapped(object sender, TappedEventArgs e)
    {
        await CerrarConfirmacionSalirAsync(false);
    }

    private bool HayDatosSinGuardar()
    {
        return !string.IsNullOrWhiteSpace(DescripcionEditor.Text) ||
               !string.IsNullOrWhiteSpace(DireccionReferenciaEntry.Text) ||
               _latitudAvistamiento.HasValue ||
               _longitudAvistamiento.HasValue ||
               _fotoCapturada != null;
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

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (SalirSinGuardarOverlay.IsVisible)
            {
                await CerrarConfirmacionSalirAsync(false);
                return;
            }

            await IntentarVolverAsync();
        });

        return true;
    }
}