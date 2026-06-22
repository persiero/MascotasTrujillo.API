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
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnEnviar.IsEnabled = false;
        BtnEnviar.Text = "Calculando GPS y enviando...";

        try
        {
            if (TipoReportePicker.SelectedIndex == -1)
            {
                await DisplayAlertAsync("Dato requerido", "Selecciona el tipo de reporte.", "OK");
                return;
            }

            string titulo = TituloEntry.Text?.Trim() ?? string.Empty;
            string descripcion = DescripcionEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(titulo))
            {
                await DisplayAlertAsync("Dato requerido", "Ingresa un título para el reporte.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                await DisplayAlertAsync("Dato requerido", "Ingresa una descripción para el reporte.", "OK");
                return;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
            );

            if (location == null)
            {
                await DisplayAlertAsync("Error GPS", "No se pudo obtener la ubicación actual.", "OK");
                return;
            }

            short tipoReporteId = TipoReportePicker.SelectedIndex == 0
                ? (short)1   // Mascota perdida
                : (short)2;  // Mascota encontrada

            string? sexoSeleccionado = SexoPicker.SelectedIndex >= 0
                ? SexoPicker.Items[SexoPicker.SelectedIndex]
                : null;

            var resultado = await _apiService.CrearReporteAsync(
                mascotaId: null,
                tipoReporteId: tipoReporteId,
                titulo: titulo,
                descripcion: descripcion,
                latitud: location.Latitude,
                longitud: location.Longitude,
                direccionReferencia: DireccionReferenciaEntry.Text,
                foto: _fotoCapturada,
                nombreMascotaReferencial: NombreMascotaEntry.Text,
                especieReferencial: EspecieEntry.Text,
                razaReferencial: RazaEntry.Text,
                colorReferencial: ColorEntry.Text,
                sexoReferencial: sexoSeleccionado
            );

            if (resultado.Exito)
            {
                await DisplayAlertAsync("¡Éxito!", "El reporte ha sido registrado correctamente.", "OK");
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
                $"No se pudo registrar el reporte: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            BtnEnviar.IsEnabled = true;
            BtnEnviar.Text = "📍 Enviar reporte con GPS";
        }
    }
}