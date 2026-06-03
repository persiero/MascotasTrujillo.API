using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class ReportarPage : ContentPage
{
    private readonly ApiService _apiService;
    private FileResult? _fotoCapturada; // Aquí guardaremos la foto temporalmente

    public ReportarPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    // Opción A: Cámara
    private async void OnTomarFotoClicked(object? sender, EventArgs e)
    {
        try
        {
            var foto = await MediaPicker.Default.CapturePhotoAsync();
            await ProcesarFoto(foto);
        }
        catch (Exception ex)
        {
            // Utilizamos la variable 'ex' para imprimir el detalle técnico del error
            await DisplayAlertAsync("Error", $"No se pudo abrir la cámara: {ex.Message}", "OK");
        }
    }

    // Opción B: Galería
    private async void OnElegirGaleriaClicked(object? sender, EventArgs e)
    {
        try
        {
            // Usamos el método nuevo (en plural) y extraemos solo la primera imagen
            var fotos = await MediaPicker.Default.PickPhotosAsync();
            var foto = fotos?.FirstOrDefault();

            await ProcesarFoto(foto);
        }
        catch (Exception ex)
        {
            // Utilizamos la variable 'ex' aquí también
            await DisplayAlertAsync("Error", $"No se pudo acceder a la galería: {ex.Message}", "OK");
        }
    }

    // Método único para manejar el resultado
    private async Task ProcesarFoto(FileResult? foto)
    {
        if (foto == null) return;

        _fotoCapturada = foto;

        // Mostramos la previsualización
        var stream = await foto.OpenReadAsync();
        FotoPreview.Source = ImageSource.FromStream(() => stream);

        // Ocultamos ambos botones y habilitamos el envío
        BotonesCaptura.IsVisible = false;
        BtnEnviar.IsEnabled = true;
    }

    private async void OnEnviarClicked(object? sender, EventArgs e)
    {
        if (_fotoCapturada == null) return;

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnEnviar.IsEnabled = false;
        BtnEnviar.Text = "Calculando GPS y enviando...";

        try
        {
            // ¡Obtenemos las coordenadas reales del celular!
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

            if (location != null)
            {
                string descripcion = DescripcionEntry.Text;

                // Enviamos todo a tu API
                bool exito = await _apiService.ReportarAvistamientoAsync(_fotoCapturada, descripcion, location.Latitude, location.Longitude);

                if (exito)
                {
                    await DisplayAlertAsync("¡Éxito!", "La mascota ha sido reportada. Revisa el radar.", "OK");
                    await Navigation.PopAsync(); // Regresamos al Radar
                }
                else
                {
                    await DisplayAlertAsync("Error", "Hubo un problema al subir la foto.", "OK");
                }
            }
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Error GPS", "No se pudo obtener la ubicación. Asegúrate de tener el GPS encendido.", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            BtnEnviar.IsEnabled = true;
            BtnEnviar.Text = "📍 Enviar Reporte con GPS";
        }
    }
}