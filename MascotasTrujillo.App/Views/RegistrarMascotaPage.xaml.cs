using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RegistrarMascotaPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Mascota? _mascotaEditar;

    private string _rutaFotoLocal = string.Empty;

    private bool EsModoEdicion => _mascotaEditar != null;

    public RegistrarMascotaPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    public RegistrarMascotaPage(ApiService apiService, Mascota mascotaEditar)
    {
        InitializeComponent();

        _apiService = apiService;
        _mascotaEditar = mascotaEditar;

        CargarDatosParaEdicion();
    }

    private void CargarDatosParaEdicion()
    {
        if (_mascotaEditar == null)
            return;

        Title = "Editar Mascota";

        NombreEntry.Text = _mascotaEditar.Nombre;
        EspecieEntry.Text = _mascotaEditar.Especie;
        RazaEntry.Text = _mascotaEditar.Raza;
        ColorEntry.Text = _mascotaEditar.ColorPrincipal;
        EdadEntry.Text = _mascotaEditar.EdadAproximada;
        RasgosEditor.Text = _mascotaEditar.RasgosParticulares;

        EnfermedadesEditor.Text = _mascotaEditar.Enfermedades;
        DiscapacidadesEditor.Text = _mascotaEditar.Discapacidades;
        TratamientosEditor.Text = _mascotaEditar.Tratamientos;
        NecesidadesEspecialesEditor.Text = _mascotaEditar.NecesidadesEspeciales;
        ObservacionesSaludEditor.Text = _mascotaEditar.ObservacionesSalud;

        DispositivoIdEntry.Text = _mascotaEditar.DispositivoId;

        if (!string.IsNullOrWhiteSpace(_mascotaEditar.FotoPerfilUrl))
        {
            FotoMascotaImage.Source = _mascotaEditar.FotoPerfilUrl;
        }

        if (!string.IsNullOrWhiteSpace(_mascotaEditar.Sexo))
        {
            int index = SexoPicker.Items.IndexOf(_mascotaEditar.Sexo);

            if (index >= 0)
            {
                SexoPicker.SelectedIndex = index;
            }
        }

        BtnGuardar.Text = "Actualizar mascota";
    }

    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
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
            await DisplayAlertAsync("Error", $"No se pudo tomar la foto: {ex.Message}", "OK");
        }
    }

    private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
    {
        try
        {
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
            await DisplayAlertAsync("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        }
    }

    private async void OnGuardarMascotaClicked(object sender, EventArgs e)
    {
        string nombre = NombreEntry.Text?.Trim() ?? string.Empty;
        string especie = EspecieEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlertAsync("Dato requerido", "El nombre de la mascota es obligatorio.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(especie))
        {
            await DisplayAlertAsync("Dato requerido", "La especie de la mascota es obligatoria.", "OK");
            return;
        }

        string? sexoSeleccionado = SexoPicker.SelectedIndex >= 0
            ? SexoPicker.Items[SexoPicker.SelectedIndex]
            : null;

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnGuardar.IsEnabled = false;
        BtnGuardar.Text = EsModoEdicion ? "Actualizando..." : "Guardando...";

        try
        {
            if (EsModoEdicion && _mascotaEditar != null)
            {
                var resultado = await _apiService.ActualizarMascotaAsync(
                    mascotaId: _mascotaEditar.Id,
                    nombre: nombre,
                    especie: especie,
                    raza: RazaEntry.Text?.Trim() ?? string.Empty,
                    color: ColorEntry.Text?.Trim() ?? string.Empty,
                    sexo: sexoSeleccionado ?? string.Empty,
                    edadAproximada: EdadEntry.Text?.Trim() ?? string.Empty,
                    rasgos: RasgosEditor.Text?.Trim() ?? string.Empty,

                    enfermedades: EnfermedadesEditor.Text?.Trim() ?? string.Empty,
                    discapacidades: DiscapacidadesEditor.Text?.Trim() ?? string.Empty,
                    tratamientos: TratamientosEditor.Text?.Trim() ?? string.Empty,
                    necesidadesEspeciales: NecesidadesEspecialesEditor.Text?.Trim() ?? string.Empty,
                    observacionesSalud: ObservacionesSaludEditor.Text?.Trim() ?? string.Empty,

                    dispositivoId: DispositivoIdEntry.Text?.Trim() ?? string.Empty,
                    rutaFotoLocal: _rutaFotoLocal
                );

                if (resultado.Exito)
                {
                    await DisplayAlertAsync("¡Éxito!", "Mascota actualizada correctamente.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlertAsync("Error al actualizar", resultado.Mensaje, "OK");
                }
            }
            else
            {
                var resultado = await _apiService.RegistrarMascotaAsync(
                    nombre: nombre,
                    especie: especie,
                    raza: RazaEntry.Text?.Trim() ?? string.Empty,
                    color: ColorEntry.Text?.Trim() ?? string.Empty,
                    sexo: sexoSeleccionado ?? string.Empty,
                    edadAproximada: EdadEntry.Text?.Trim() ?? string.Empty,
                    rasgos: RasgosEditor.Text?.Trim() ?? string.Empty,

                    enfermedades: EnfermedadesEditor.Text?.Trim() ?? string.Empty,
                    discapacidades: DiscapacidadesEditor.Text?.Trim() ?? string.Empty,
                    tratamientos: TratamientosEditor.Text?.Trim() ?? string.Empty,
                    necesidadesEspeciales: NecesidadesEspecialesEditor.Text?.Trim() ?? string.Empty,
                    observacionesSalud: ObservacionesSaludEditor.Text?.Trim() ?? string.Empty,

                    dispositivoId: DispositivoIdEntry.Text?.Trim() ?? string.Empty,
                    rutaFotoLocal: _rutaFotoLocal
                );

                if (resultado.Exito)
                {
                    await DisplayAlertAsync("¡Éxito!", "Mascota registrada correctamente.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlertAsync("Error de registro", resultado.Mensaje, "OK");
                }
            }
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            BtnGuardar.IsEnabled = true;
            BtnGuardar.Text = EsModoEdicion ? "Actualizar mascota" : "Guardar mascota";
        }
    }
}