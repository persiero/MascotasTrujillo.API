using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using SkiaSharp;
using System.Diagnostics;

namespace MascotasTrujillo.App.Views;

public partial class RegistrarMascotaPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Mascota? _mascotaEditar;

    private string _rutaFotoLocal = string.Empty;

    private bool _detallesExpandido = false;
    private bool _saludExpandida = false;
    private bool _gpsExpandido = false;

    private string? _especieSeleccionada;
    private string? _sexoSeleccionado;

    private TaskCompletionSource<bool>? _confirmacionSalirTcs;
    private bool _procesandoSalida = false;

    private bool EsModoEdicion => _mascotaEditar != null;

    public RegistrarMascotaPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        ConfigurarModoVisual();
        ActualizarSeccionesPlegables();
        ActualizarEstiloEspecie();
        ActualizarTextoSexo();
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
        ActualizarEstiloEspecie();
        ActualizarTextoSexo();
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
        SeleccionarEspecie(_mascotaEditar.Especie);
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

        SeleccionarSexo(_mascotaEditar.Sexo);

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

            FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo != null)
            {
                await ProcesarFotoMascotaAsync(photo);
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
            IEnumerable<FileResult> photos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions
                {
                    Title = "Selecciona una foto de tu mascota"
                }
            );

            FileResult? photo = photos?.FirstOrDefault();

            if (photo != null)
            {
                await ProcesarFotoMascotaAsync(photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        }
    }

    private async Task ProcesarFotoMascotaAsync(FileResult foto)
    {
        try
        {
            var resultado = await ImageCompressionService.ComprimirFileResultAsync(
                foto,
                prefijoArchivo: "mascota",
                maxMb: 5,
                tipoRecorte: TipoRecorteImagen.CuadradoCentrado
            );

            _rutaFotoLocal = resultado.RutaLocal;

            FotoMascotaImage.Source = ImageSource.FromStream(
                () => new MemoryStream(resultado.Bytes)
            );
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error con la imagen",
                $"No se pudo preparar la foto para subirla.\n\nDetalle:\n{ex.Message}",
                "OK"
            );
        }
    }

    private void OnEspeciePerroClicked(object sender, EventArgs e)
    {
        SeleccionarEspecie("Perro");
    }

    private void OnEspecieGatoClicked(object sender, EventArgs e)
    {
        SeleccionarEspecie("Gato");
    }

    private void SeleccionarEspecie(string? especie)
    {
        _especieSeleccionada = string.IsNullOrWhiteSpace(especie)
            ? null
            : especie.Trim();

        ActualizarEstiloEspecie();
    }

    private void ActualizarEstiloEspecie()
    {
        Color primary = Color.FromArgb("#5B21E6");
        Color soft = Color.FromArgb("#F8F5FF");
        Color primaryDark = Color.FromArgb("#2B0B98");
        Color border = Color.FromArgb("#D8CCFF");

        void Aplicar(Button boton, bool seleccionado)
        {
            boton.BackgroundColor = seleccionado ? primary : soft;
            boton.TextColor = seleccionado ? Colors.White : primaryDark;
            boton.BorderColor = seleccionado ? primary : border;
            boton.BorderWidth = seleccionado ? 0 : 1;
        }

        Aplicar(BtnEspeciePerro, _especieSeleccionada == "Perro");
        Aplicar(BtnEspecieGato, _especieSeleccionada == "Gato");
    }

    private async void OnAbrirSexoSelectorClicked(object sender, EventArgs e)
    {
        SexoOverlay.IsVisible = true;
        SexoOverlay.Opacity = 0;

        await SexoOverlay.FadeToAsync(1, 150);
    }

    private async void OnCerrarSexoOverlayClicked(object sender, EventArgs e)
    {
        await CerrarSexoOverlayAsync();
    }

    private async void OnCerrarSexoOverlayTapped(object sender, TappedEventArgs e)
    {
        await CerrarSexoOverlayAsync();
    }

    private async void OnSexoMachoClicked(object sender, EventArgs e)
    {
        SeleccionarSexo("Macho");
        await CerrarSexoOverlayAsync();
    }

    private async void OnSexoHembraClicked(object sender, EventArgs e)
    {
        SeleccionarSexo("Hembra");
        await CerrarSexoOverlayAsync();
    }

    private async void OnSexoNoEspecificadoClicked(object sender, EventArgs e)
    {
        SeleccionarSexo("No especificado");
        await CerrarSexoOverlayAsync();
    }

    private void SeleccionarSexo(string? sexo)
    {
        _sexoSeleccionado = string.IsNullOrWhiteSpace(sexo)
            ? null
            : sexo.Trim();

        ActualizarTextoSexo();
    }

    private void ActualizarTextoSexo()
    {
        BtnSexoSelector.Text = string.IsNullOrWhiteSpace(_sexoSeleccionado)
            ? "Seleccionar sexo"
            : $"Sexo: {_sexoSeleccionado}";
    }

    private async Task CerrarSexoOverlayAsync()
    {
        await SexoOverlay.FadeToAsync(0, 120);
        SexoOverlay.IsVisible = false;
        SexoOverlay.Opacity = 0;
    }

    private async void OnGuardarMascotaClicked(object sender, EventArgs e)
    {
        string nombre = NombreEntry.Text?.Trim() ?? string.Empty;
        string especie = _especieSeleccionada?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlertAsync("Dato requerido", "El nombre de la mascota es obligatorio.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(especie))
        {
            await DisplayAlertAsync("Dato requerido", "Selecciona la especie de la mascota.", "OK");
            return;
        }

        string? sexoSeleccionado = _sexoSeleccionado;

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
                    string mensaje = string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "No se pudo actualizar la mascota. Revisa los datos ingresados e inténtalo nuevamente."
                        : resultado.Mensaje;

                    await DisplayAlertAsync("Error al actualizar", mensaje, "OK");
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
                    string mensaje = string.IsNullOrWhiteSpace(resultado.Mensaje)
                        ? "No se pudo registrar la mascota. Revisa los datos ingresados e inténtalo nuevamente."
                        : resultado.Mensaje;

                    await DisplayAlertAsync("Error de registro", mensaje, "OK");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync(
                "Error de conexión",
                $"No se pudo conectar con el servidor.\n\nDetalle técnico:\n{ex.Message}",
                "OK"
            );
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync(
                "Tiempo agotado",
                "El servidor tardó demasiado en responder. Verifica tu conexión a Internet e inténtalo nuevamente.",
                "OK"
            );
        }
        catch (Exception ex)
        {
            string accion = EsModoEdicion ? "actualizar" : "registrar";

            await DisplayAlertAsync(
                $"Error al {accion}",
                $"Ocurrió un error inesperado al {accion} la mascota.\n\nDetalle técnico:\n{ex.Message}",
                "OK"
            );
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
        string especie = Normalizar(_especieSeleccionada);
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

        string sexoSeleccionado = Normalizar(_sexoSeleccionado);

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
                LblMensajeSalirSinGuardar.Text = EsModoEdicion
                    ? "¿Deseas volver? Los cambios realizados no se guardarán."
                    : "¿Deseas volver? Los datos ingresados no se guardarán.";

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
            if (SexoOverlay.IsVisible)
            {
                await CerrarSexoOverlayAsync();
                return;
            }

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