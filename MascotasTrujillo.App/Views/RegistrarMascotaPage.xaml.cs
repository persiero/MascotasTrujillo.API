using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RegistrarMascotaPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Mascota? _mascotaEditar;

    private string _rutaFotoLocal = string.Empty;

    private bool _detallesExpandido = false;
    private bool _saludExpandida = false;
    private bool _gpsExpandido = false;

    private bool EsModoEdicion => _mascotaEditar != null;

    public RegistrarMascotaPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        ConfigurarModoVisual();
        ActualizarSeccionesPlegables();
    }

    public RegistrarMascotaPage(ApiService apiService, Mascota mascotaEditar)
    {
        InitializeComponent();

        _apiService = apiService;
        _mascotaEditar = mascotaEditar;

        CargarDatosParaEdicion();
        ConfigurarModoVisual();

        _detallesExpandido = true;
        _saludExpandida = TieneDatosSalud();
        _gpsExpandido = !string.IsNullOrWhiteSpace(_mascotaEditar.DispositivoId);

        ActualizarSeccionesPlegables();
    }

    private bool TieneDatosSalud()
    {
        if (_mascotaEditar == null)
            return false;

        return !string.IsNullOrWhiteSpace(_mascotaEditar.Enfermedades) ||
               !string.IsNullOrWhiteSpace(_mascotaEditar.Discapacidades) ||
               !string.IsNullOrWhiteSpace(_mascotaEditar.Tratamientos) ||
               !string.IsNullOrWhiteSpace(_mascotaEditar.NecesidadesEspeciales) ||
               !string.IsNullOrWhiteSpace(_mascotaEditar.ObservacionesSalud);
    }

    private void CargarDatosParaEdicion()
    {
        if (_mascotaEditar == null)
            return;
               
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
            BtnGuardar.Text = EsModoEdicion ? "💾 Actualizar mascota" : "💾 Guardar mascota";
        }
    }

    private void ConfigurarModoVisual()
    {
        if (EsModoEdicion)
        {
            Title = "Editar Mascota";

            LblTituloHeader.Text = "Editar mascota";
            LblSubtituloHeader.Text = "Actualiza los datos de tu mascota";

            LblIconoFormulario.Text = "✏️";
            LblTituloFormulario.Text = "Editar mascota";
            LblSubtituloFormulario.Text = "Actualiza sus datos principales.";

            BtnGuardar.Text = "💾 Actualizar mascota";
        }
        else
        {
            Title = "Registrar Mascota";

            LblTituloHeader.Text = "Registrar mascota";
            LblSubtituloHeader.Text = "Completa los datos de tu mascota";

            LblIconoFormulario.Text = "🐾";
            LblTituloFormulario.Text = "Registrar mascota";
            LblSubtituloFormulario.Text = "Agrega sus datos básicos.";

            BtnGuardar.Text = "💾 Guardar mascota";
        }
    }

    private void OnToggleDetallesClicked(object sender, EventArgs e)
    {
        _detallesExpandido = !_detallesExpandido;
        ActualizarSeccionesPlegables();
    }

    private void OnToggleSaludClicked(object sender, EventArgs e)
    {
        _saludExpandida = !_saludExpandida;
        ActualizarSeccionesPlegables();
    }

    private void OnToggleGpsClicked(object sender, EventArgs e)
    {
        _gpsExpandido = !_gpsExpandido;
        ActualizarSeccionesPlegables();
    }

    private void ActualizarSeccionesPlegables()
    {
        ActualizarSeccion(
            DetallesContainer,
            BtnToggleDetalles,
            _detallesExpandido
        );

        ActualizarSeccion(
            SaludContainer,
            BtnToggleSalud,
            _saludExpandida
        );

        ActualizarSeccion(
            GpsContainer,
            BtnToggleGps,
            _gpsExpandido
        );
    }

    private void ActualizarSeccion(VisualElement contenedor, Button boton, bool expandido)
    {
        contenedor.IsVisible = expandido;

        boton.Text = expandido ? "Ocultar" : "Mostrar";
        boton.BackgroundColor = expandido
            ? Color.FromArgb("#5B21E6")
            : Color.FromArgb("#EEE7FF");

        boton.TextColor = expandido
            ? Colors.White
            : Color.FromArgb("#2B0B98");
    }
    
    private bool HayDatosSinGuardar()
    {
        string nombre = Normalizar(NombreEntry.Text);
        string especie = Normalizar(EspecieEntry.Text);
        string raza = Normalizar(RazaEntry.Text);
        string color = Normalizar(ColorEntry.Text);
        string edad = Normalizar(EdadEntry.Text);
        string rasgos = Normalizar(RasgosEditor.Text);

        string enfermedades = Normalizar(EnfermedadesEditor.Text);
        string discapacidades = Normalizar(DiscapacidadesEditor.Text);
        string tratamientos = Normalizar(TratamientosEditor.Text);
        string necesidades = Normalizar(NecesidadesEspecialesEditor.Text);
        string observaciones = Normalizar(ObservacionesSaludEditor.Text);

        string dispositivoId = Normalizar(DispositivoIdEntry.Text);

        string sexoSeleccionado = SexoPicker.SelectedIndex >= 0
            ? Normalizar(SexoPicker.Items[SexoPicker.SelectedIndex])
            : string.Empty;

        // Si tomó o seleccionó una nueva foto, hay cambios.
        if (!string.IsNullOrWhiteSpace(_rutaFotoLocal))
            return true;

        // Modo registro: basta con detectar si empezó a llenar algo.
        if (!EsModoEdicion || _mascotaEditar == null)
        {
            return !string.IsNullOrWhiteSpace(nombre) ||
                   !string.IsNullOrWhiteSpace(especie) ||
                   !string.IsNullOrWhiteSpace(raza) ||
                   !string.IsNullOrWhiteSpace(color) ||
                   !string.IsNullOrWhiteSpace(edad) ||
                   !string.IsNullOrWhiteSpace(rasgos) ||
                   !string.IsNullOrWhiteSpace(enfermedades) ||
                   !string.IsNullOrWhiteSpace(discapacidades) ||
                   !string.IsNullOrWhiteSpace(tratamientos) ||
                   !string.IsNullOrWhiteSpace(necesidades) ||
                   !string.IsNullOrWhiteSpace(observaciones) ||
                   !string.IsNullOrWhiteSpace(dispositivoId) ||
                   !string.IsNullOrWhiteSpace(sexoSeleccionado);
        }

        // Modo edición: compara lo actual contra los datos originales.
        return !SonIguales(nombre, _mascotaEditar.Nombre) ||
               !SonIguales(especie, _mascotaEditar.Especie) ||
               !SonIguales(raza, _mascotaEditar.Raza) ||
               !SonIguales(color, _mascotaEditar.ColorPrincipal) ||
               !SonIguales(edad, _mascotaEditar.EdadAproximada) ||
               !SonIguales(rasgos, _mascotaEditar.RasgosParticulares) ||
               !SonIguales(enfermedades, _mascotaEditar.Enfermedades) ||
               !SonIguales(discapacidades, _mascotaEditar.Discapacidades) ||
               !SonIguales(tratamientos, _mascotaEditar.Tratamientos) ||
               !SonIguales(necesidades, _mascotaEditar.NecesidadesEspeciales) ||
               !SonIguales(observaciones, _mascotaEditar.ObservacionesSalud) ||
               !SonIguales(dispositivoId, _mascotaEditar.DispositivoId) ||
               !SonIguales(sexoSeleccionado, _mascotaEditar.Sexo);
    }

    private static string Normalizar(string? valor)
    {
        return valor?.Trim() ?? string.Empty;
    }

    private static bool SonIguales(string? valorActual, string? valorOriginal)
    {
        return string.Equals(
            Normalizar(valorActual),
            Normalizar(valorOriginal),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        if (HayDatosSinGuardar())
        {
            string mensaje = EsModoEdicion
                ? "¿Deseas volver? Los cambios realizados no se guardarán."
                : "¿Deseas volver? Los datos ingresados no se guardarán.";

            bool salir = await DisplayAlertAsync(
                "Salir sin guardar",
                mensaje,
                "Sí, volver",
                "Cancelar"
            );

            if (!salir)
                return;
        }

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