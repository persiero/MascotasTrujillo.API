using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RegistrarMascotaPage : ContentPage
{
    private readonly ApiService _apiService;
    private string _rutaFotoLocal = string.Empty;

    public RegistrarMascotaPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    // Opcion A: Capturar Foto con la Cámara Real del Celular
    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                // SOLUCIÓN 1: Agregamos el "?" para indicar que puede ser nulo
                FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo != null)
                {
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                    using Stream sourceStream = await photo.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);
                    await sourceStream.CopyToAsync(localFileStream);

                    _rutaFotoLocal = localFilePath;
                    FotoMascotaImage.Source = ImageSource.FromFile(_rutaFotoLocal);
                }
            }
            else
            {
                await DisplayAlertAsync("No disponible", "La cámara no está soportada en este dispositivo.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al capturar foto: {ex.Message}");
        }
    }

    // Opcion B: Seleccionar Foto desde la Galería de Imágenes
    private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            // SOLUCIÓN 2: Usamos el nuevo método plural y extraemos solo la primera foto
            IEnumerable<FileResult> photos = await MediaPicker.Default.PickPhotosAsync();
            FileResult? photo = photos?.FirstOrDefault();

            if (photo != null)
            {
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(localFilePath);
                await sourceStream.CopyToAsync(localFileStream);

                _rutaFotoLocal = localFilePath;
                FotoMascotaImage.Source = ImageSource.FromFile(_rutaFotoLocal);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al seleccionar foto: {ex.Message}");
        }
    }

    // Procesar y enviar el registro completo al Servidor
    private async void OnGuardarMascotaClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NombreEntry.Text))
        {
            await DisplayAlertAsync("Atención", "El nombre de la mascota es obligatorio.", "OK");
            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        var resultado = await _apiService.RegistrarMascotaAsync(
            NombreEntry.Text.Trim(),
            EspecieEntry.Text?.Trim() ?? string.Empty,
            RazaEntry.Text?.Trim() ?? string.Empty,
            ColorEntry.Text?.Trim() ?? string.Empty,
            RasgosEditor.Text?.Trim() ?? string.Empty,
            DispositivoIdEntry.Text?.Trim() ?? string.Empty,
            _rutaFotoLocal
        );

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (resultado.Exito)
        {
            await DisplayAlertAsync("¡Éxito!", "Mascota registrada correctamente.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlertAsync("Error de Registro", resultado.Mensaje, "OK");
        }
    }
}