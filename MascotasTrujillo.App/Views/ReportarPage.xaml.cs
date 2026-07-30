using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace MascotasTrujillo.App.Views;

public partial class ReportarPage : ContentPage
{
    private readonly ApiService _apiService;

    private FileResult? _fotoCapturada;
    private List<Mascota> _misMascotas = new();
    private Mascota? _mascotaSeleccionada;
    private int _tipoReporteSeleccionado = -1; // 0 = perdida, 1 = encontrada
    private bool _datosMascotaExpandido = false;

    private TaskCompletionSource<bool>? _confirmacionSalirTcs;
    private bool _procesandoSalida = false;

    private string? _especieSeleccionada;
    private string? _sexoSeleccionado;

    private double? _latitudReporte;
    private double? _longitudReporte;

    private readonly double _latitudTrujillo = -8.1118;
    private readonly double _longitudTrujillo = -79.0287;


    public ReportarPage(ApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        BtnEnviar.IsEnabled = true;

        ActualizarEstiloTipoReporte();
        ActualizarTextosSegunTipo();
        ActualizarEstadoSeccionMascota();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarMisMascotasAsync();
    }

    private void OnToggleDatosMascotaClicked(object sender, EventArgs e)
    {
        _datosMascotaExpandido = !_datosMascotaExpandido;
        ActualizarEstadoSeccionMascota();
    }

    private void ActualizarEstadoSeccionMascota()
    {
        MascotaCamposContainer.IsVisible = _datosMascotaExpandido;

        BtnToggleDatosMascota.Text = _datosMascotaExpandido
            ? "Ocultar"
            : "Mostrar";

        BtnToggleDatosMascota.BackgroundColor = _datosMascotaExpandido
            ? Color.FromArgb("#5B21E6")
            : Color.FromArgb("#EEE7FF");

        BtnToggleDatosMascota.TextColor = _datosMascotaExpandido
            ? Colors.White
            : Color.FromArgb("#2B0B98");
    }

    private async Task CargarMisMascotasAsync()
    {
        try
        {
            var mascotas = await _apiService.GetMisMascotasAsync();

            _misMascotas = mascotas ?? new List<Mascota>();
            MisMascotasPicker.ItemsSource = _misMascotas;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Aviso",
                $"No se pudieron cargar tus mascotas registradas: {ex.Message}",
                "OK"
            );
        }
    }

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

            SeleccionarUbicacionReporte(
                location.Latitude,
                location.Longitude,
                "Se usará tu ubicación GPS actual para el reporte."
            );
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("GPS", $"No se pudo obtener la ubicación: {ex.Message}", "OK");
        }
    }

    private async void OnSeleccionarMapaClicked(object sender, EventArgs e)
    {
        double latitudInicial = _latitudReporte ?? _latitudTrujillo;
        double longitudInicial = _longitudReporte ?? _longitudTrujillo;

        await Navigation.PushAsync(
            new SeleccionarUbicacionPage(
                latitudInicial,
                longitudInicial,
                (latitud, longitud) =>
                {
                    SeleccionarUbicacionReporte(
                        latitud,
                        longitud,
                        "Ubicación seleccionada manualmente en el mapa."
                    );
                },
                "Seleccionar ubicación del reporte"
            )
        );
    }

    private void SeleccionarUbicacionReporte(double latitud, double longitud, string mensaje)
    {
        _latitudReporte = latitud;
        _longitudReporte = longitud;

        LblUbicacionSeleccionada.Text =
            $"{mensaje}\nLat. {latitud:F6}, Long. {longitud:F6}";
    }

    private void OnTipoPerdidaClicked(object sender, EventArgs e)
    {
        SeleccionarTipoReporte(0);
    }

    private void OnTipoEncontradaClicked(object sender, EventArgs e)
    {
        SeleccionarTipoReporte(1);
    }

    private void SeleccionarTipoReporte(int tipo)
    {
        _tipoReporteSeleccionado = tipo;

        ActualizarEstiloTipoReporte();
        ProcesarCambioTipoReporte();
    }

    private void ProcesarCambioTipoReporte()
    {
        bool esMascotaPerdida = _tipoReporteSeleccionado == 0;
        bool esMascotaEncontrada = _tipoReporteSeleccionado == 1;

        ActualizarTextosSegunTipo();

        SeleccionMascotaBorder.IsVisible = esMascotaPerdida;

        if (esMascotaPerdida)
        {
            LimpiarDatosMascota();
            HabilitarCamposMascota(false);

            _fotoCapturada = null;
            BotonesCaptura.IsVisible = true;
            FotoPreview.Source = null;

            LblResumenMascotaSeleccionada.Text =
                "Selecciona una mascota registrada.";

            _datosMascotaExpandido = false;
        }

        if (esMascotaEncontrada)
        {
            _mascotaSeleccionada = null;
            MisMascotasPicker.SelectedIndex = -1;

            LimpiarDatosMascota();
            HabilitarCamposMascota(true);

            FotoPreview.Source = null;
            BotonesCaptura.IsVisible = true;
            _fotoCapturada = null;

            _datosMascotaExpandido = true;
        }

        ActualizarEstadoSeccionMascota();
    }

    private void ActualizarEstiloTipoReporte()
    {
        Color primary = Color.FromArgb("#5B21E6");
        Color soft = Color.FromArgb("#F8F5FF");
        Color primaryDark = Color.FromArgb("#2B0B98");
        Color border = Color.FromArgb("#D8CCFF");

        void AplicarEstado(Button boton, bool seleccionado)
        {
            boton.BackgroundColor = seleccionado ? primary : soft;
            boton.TextColor = seleccionado ? Colors.White : primaryDark;
            boton.BorderColor = seleccionado ? primary : border;
            boton.BorderWidth = seleccionado ? 0 : 1;
        }

        AplicarEstado(BtnTipoPerdida, _tipoReporteSeleccionado == 0);
        AplicarEstado(BtnTipoEncontrada, _tipoReporteSeleccionado == 1);
    }

    private void OnMascotaRegistradaSeleccionada(object sender, EventArgs e)
    {
        _mascotaSeleccionada = MisMascotasPicker.SelectedItem as Mascota;

        if (_mascotaSeleccionada == null)
            return;

        NombreMascotaEntry.Text = _mascotaSeleccionada.Nombre;
        SeleccionarEspecie(_mascotaSeleccionada.Especie);
        RazaEntry.Text = _mascotaSeleccionada.Raza;
        ColorEntry.Text = _mascotaSeleccionada.ColorPrincipal;

        SeleccionarSexo(_mascotaSeleccionada.Sexo);

        if (!string.IsNullOrWhiteSpace(_mascotaSeleccionada.FotoPerfilUrl))
        {
            FotoPreview.Source = _mascotaSeleccionada.FotoPerfilUrl;
            BotonesCaptura.IsVisible = false;
        }

        if (!string.IsNullOrWhiteSpace(_mascotaSeleccionada.FotoPerfilUrl))
        {
            LblAyudaFotoReporte.Text =
                "Se usará la foto principal de la mascota seleccionada.";
        }
        else
        {
            LblAyudaFotoReporte.Text =
                "Esta mascota no tiene foto registrada. Puedes agregar una foto para el reporte.";
            BotonesCaptura.IsVisible = true;
        }

        LblResumenMascotaSeleccionada.Text =
            $"Mascota seleccionada: {_mascotaSeleccionada.Nombre}. Sus datos se usarán automáticamente en el reporte.";

        _datosMascotaExpandido = false;
        ActualizarEstadoSeccionMascota();
    }

    private void HabilitarCamposMascota(bool habilitar)
    {
        NombreMascotaEntry.IsReadOnly = !habilitar;
        RazaEntry.IsReadOnly = !habilitar;
        ColorEntry.IsReadOnly = !habilitar;

        BtnEspeciePerro.IsEnabled = habilitar;
        BtnEspecieGato.IsEnabled = habilitar;
        BtnSexoSelector.IsEnabled = habilitar;

        Color fondoEditable = Color.FromArgb("#F8F5FF");
        Color fondoBloqueado = Color.FromArgb("#EEE7FF");
        Color textoEditable = Color.FromArgb("#1F2340");
        Color textoBloqueado = Color.FromArgb("#64748B");

        void AplicarCampo(Border campo, Entry entry)
        {
            campo.BackgroundColor = habilitar ? fondoEditable : fondoBloqueado;
            entry.TextColor = habilitar ? textoEditable : textoBloqueado;
            entry.BackgroundColor = Colors.Transparent;
        }

        AplicarCampo(NombreMascotaField, NombreMascotaEntry);
        AplicarCampo(RazaField, RazaEntry);
        AplicarCampo(ColorField, ColorEntry);

        EspecieField.BackgroundColor = habilitar ? fondoEditable : fondoBloqueado;
        SexoField.BackgroundColor = habilitar ? fondoEditable : fondoBloqueado;

        BtnSexoSelector.TextColor = habilitar ? textoEditable : textoBloqueado;

        ActualizarEstiloEspecie();
    }

    private void LimpiarDatosMascota()
    {
        NombreMascotaEntry.Text = string.Empty;
        RazaEntry.Text = string.Empty;
        ColorEntry.Text = string.Empty;

        _especieSeleccionada = null;
        _sexoSeleccionado = null;

        ActualizarEstiloEspecie();
        ActualizarTextoSexo();
    }

    private async void OnTomarFotoClicked(object? sender, EventArgs e)
    {
        try
        {
            var foto = await MediaPicker.Default.CapturePhotoAsync();
            await ProcesarFoto(foto);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo abrir la cámara: {ex.Message}", "OK");
        }
    }

    private async void OnElegirGaleriaClicked(object? sender, EventArgs e)
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
                prefijoArchivo: "reporte",
                maxMb: 5
            );

            // Reemplazamos la foto original por la foto comprimida.
            // Así el resto del código puede seguir usando _fotoCapturada normalmente.
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
                $"No se pudo preparar la foto del reporte.\n\nDetalle:\n{ex.Message}",
                "OK"
            );
        }
    }

    private void ActualizarTextosSegunTipo()
    {
        if (_tipoReporteSeleccionado == 0)
        {
            LblAyudaTipoReporte.Text = "Mascota perdida";

            LblTituloDatosMascota.Text = "Datos de mi mascota";
            LblAyudaDatosMascota.Text = "Se completan desde la mascota seleccionada.";

            LblTituloFotoReporte.Text = "Foto";
            LblAyudaFotoReporte.Text =
                "Usaremos su foto principal o puedes agregar una.";
        }
        else if (_tipoReporteSeleccionado == 1)
        {
            LblAyudaTipoReporte.Text = "Mascota encontrada";

            LblTituloDatosMascota.Text = "Mascota encontrada";
            LblAyudaDatosMascota.Text = "Completa solo lo que puedas identificar.";

            LblTituloFotoReporte.Text = "Foto";
            LblAyudaFotoReporte.Text =
                "Agrega una imagen clara para reconocerla.";
        }
        else
        {
            LblAyudaTipoReporte.Text = string.Empty;

            LblTituloDatosMascota.Text = "Mascota";
            LblAyudaDatosMascota.Text = "Completa solo lo necesario.";

            LblTituloFotoReporte.Text = "Foto";
            LblAyudaFotoReporte.Text = "Agrega una imagen clara.";
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
        if (!BtnSexoSelector.IsEnabled)
            return;

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

    private async void OnSexoNoIdentificadoClicked(object sender, EventArgs e)
    {
        SeleccionarSexo("No identificado");
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
    }

    private async void OnEnviarClicked(object? sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnEnviar.IsEnabled = false;
        BtnEnviar.Text = "Publicando reporte...";

        try
        {
            if (_tipoReporteSeleccionado == -1)
            {
                await DisplayAlertAsync("Dato requerido", "Selecciona el tipo de reporte.", "OK");
                return;
            }

            short tipoReporteId = _tipoReporteSeleccionado == 0
                ? (short)1
                : (short)2;

            bool esMascotaPerdida = tipoReporteId == 1;
            bool esMascotaEncontrada = tipoReporteId == 2;

            if (esMascotaPerdida && _mascotaSeleccionada == null)
            {
                await DisplayAlertAsync(
                    "Mascota requerida",
                    "Para reportar una mascota perdida debes seleccionar una de tus mascotas registradas.",
                    "OK"
                );

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

            if (esMascotaEncontrada)
            {
                if (string.IsNullOrWhiteSpace(_especieSeleccionada))
                {
                    await DisplayAlertAsync(
                        "Dato requerido",
                        "Ingresa la especie de la mascota encontrada.",
                        "OK"
                    );

                    return;
                }

                if (_fotoCapturada == null)
                {
                    await DisplayAlertAsync(
                        "Foto requerida",
                        "Para reportar una mascota encontrada, agrega una foto.",
                        "OK"
                    );

                    return;
                }
            }

            if (!_latitudReporte.HasValue || !_longitudReporte.HasValue)
            {
                await DisplayAlertAsync(
                    "Ubicación requerida",
                    "Selecciona la ubicación del reporte usando tu GPS o tocando el mapa.",
                    "OK"
                );

                return;
            }

            string? sexoSeleccionado = _sexoSeleccionado;

            var resultado = await _apiService.CrearReporteAsync(
                mascotaId: esMascotaPerdida ? _mascotaSeleccionada?.Id : null,
                tipoReporteId: tipoReporteId,
                titulo: titulo,
                descripcion: descripcion,
                latitud: _latitudReporte.Value,
                longitud: _longitudReporte.Value,
                direccionReferencia: DireccionReferenciaEntry.Text,
                foto: _fotoCapturada,
                nombreMascotaReferencial: esMascotaEncontrada ? NombreMascotaEntry.Text : null,
                especieReferencial: esMascotaEncontrada ? _especieSeleccionada : null,
                razaReferencial: esMascotaEncontrada ? RazaEntry.Text : null,
                colorReferencial: esMascotaEncontrada ? ColorEntry.Text : null,
                sexoReferencial: esMascotaEncontrada ? sexoSeleccionado : null
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
            BtnEnviar.Text = "📌 Publicar reporte";
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
        return _tipoReporteSeleccionado != -1 ||
               _mascotaSeleccionada != null ||
               !string.IsNullOrWhiteSpace(TituloEntry.Text) ||
               !string.IsNullOrWhiteSpace(DescripcionEntry.Text) ||
               !string.IsNullOrWhiteSpace(DireccionReferenciaEntry.Text) ||
               !string.IsNullOrWhiteSpace(NombreMascotaEntry.Text) ||
               !string.IsNullOrWhiteSpace(_especieSeleccionada) ||
               !string.IsNullOrWhiteSpace(RazaEntry.Text) ||
               !string.IsNullOrWhiteSpace(ColorEntry.Text) ||
               !string.IsNullOrWhiteSpace(_sexoSeleccionado) ||
               _latitudReporte.HasValue ||
               _longitudReporte.HasValue ||
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
}