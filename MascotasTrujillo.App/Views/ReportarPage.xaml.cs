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

    private double? _latitudReporte;
    private double? _longitudReporte;

    private readonly double _latitudTrujillo = -8.1118;
    private readonly double _longitudTrujillo = -79.0287;

  
    public ReportarPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        BtnEnviar.IsEnabled = true;
        ActualizarTextosSegunTipo();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarMisMascotasAsync();
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

    private void OnTipoReporteChanged(object sender, EventArgs e)
    {
        bool esMascotaPerdida = TipoReportePicker.SelectedIndex == 0;
        bool esMascotaEncontrada = TipoReportePicker.SelectedIndex == 1;

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
                "Selecciona una mascota registrada para cargar sus datos automáticamente.";
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
        }
    }

    private void OnMascotaRegistradaSeleccionada(object sender, EventArgs e)
    {
        _mascotaSeleccionada = MisMascotasPicker.SelectedItem as Mascota;

        if (_mascotaSeleccionada == null)
            return;

        NombreMascotaEntry.Text = _mascotaSeleccionada.Nombre;
        EspecieEntry.Text = _mascotaSeleccionada.Especie;
        RazaEntry.Text = _mascotaSeleccionada.Raza;
        ColorEntry.Text = _mascotaSeleccionada.ColorPrincipal;

        if (!string.IsNullOrWhiteSpace(_mascotaSeleccionada.Sexo))
        {
            int index = SexoPicker.Items.IndexOf(_mascotaSeleccionada.Sexo);

            if (index >= 0)
                SexoPicker.SelectedIndex = index;
        }

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
    }

    private void HabilitarCamposMascota(bool habilitar)
    {
        NombreMascotaEntry.IsReadOnly = !habilitar;
        EspecieEntry.IsReadOnly = !habilitar;
        RazaEntry.IsReadOnly = !habilitar;
        ColorEntry.IsReadOnly = !habilitar;

        SexoPicker.IsEnabled = habilitar;

        var colorFondo = habilitar
            ? Color.FromArgb("#F1F5F9")
            : Color.FromArgb("#EEF2FF");

        NombreMascotaEntry.BackgroundColor = colorFondo;
        EspecieEntry.BackgroundColor = colorFondo;
        RazaEntry.BackgroundColor = colorFondo;
        ColorEntry.BackgroundColor = colorFondo;
    }

    private void LimpiarDatosMascota()
    {
        NombreMascotaEntry.Text = string.Empty;
        EspecieEntry.Text = string.Empty;
        RazaEntry.Text = string.Empty;
        ColorEntry.Text = string.Empty;
        SexoPicker.SelectedIndex = -1;
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

        _fotoCapturada = foto;

        var stream = await foto.OpenReadAsync();
        FotoPreview.Source = ImageSource.FromStream(() => stream);

        BotonesCaptura.IsVisible = false;
    }

    private void ActualizarTextosSegunTipo()
    {
        if (TipoReportePicker.SelectedIndex == 0)
        {
            LblAyudaTipoReporte.Text =
                "Selecciona una mascota registrada. Sus datos principales se cargarán automáticamente.";

            LblTituloDatosMascota.Text = "Datos de mi mascota";
            LblAyudaDatosMascota.Text =
                "Estos datos se cargan desde la mascota seleccionada y se usarán para publicar el reporte.";

            LblTituloFotoReporte.Text = "Fotografía del reporte";
            LblAyudaFotoReporte.Text =
                "Se usará la foto principal de tu mascota. Si no tiene foto registrada, puedes agregar una imagen.";
        }
        else if (TipoReportePicker.SelectedIndex == 1)
        {
            LblAyudaTipoReporte.Text =
                "Completa manualmente la información disponible de la mascota encontrada.";

            LblTituloDatosMascota.Text = "Datos referenciales de la mascota encontrada";
            LblAyudaDatosMascota.Text =
                "Ingresa los datos que hayas podido observar. No es necesario conocer toda la información.";

            LblTituloFotoReporte.Text = "Fotografía de la mascota encontrada";
            LblAyudaFotoReporte.Text =
                "Agrega una foto para que el dueño pueda reconocerla con mayor facilidad.";
        }
        else
        {
            LblAyudaTipoReporte.Text =
                "Selecciona si deseas reportar una mascota perdida o encontrada.";

            LblTituloDatosMascota.Text = "Datos de la mascota";
            LblAyudaDatosMascota.Text =
                "Completa estos datos si no estás asociando el reporte a una mascota registrada.";

            LblTituloFotoReporte.Text = "Fotografía del reporte";
            LblAyudaFotoReporte.Text =
                "Agrega una foto para que sea más fácil identificar a la mascota.";
        }
    }

    private async void OnEnviarClicked(object? sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnEnviar.IsEnabled = false;
        BtnEnviar.Text = "Publicando reporte...";

        try
        {
            if (TipoReportePicker.SelectedIndex == -1)
            {
                await DisplayAlertAsync("Dato requerido", "Selecciona el tipo de reporte.", "OK");
                return;
            }

            short tipoReporteId = TipoReportePicker.SelectedIndex == 0
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
                if (string.IsNullOrWhiteSpace(EspecieEntry.Text))
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

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
            );

            if (!_latitudReporte.HasValue || !_longitudReporte.HasValue)
            {
                await DisplayAlertAsync(
                    "Ubicación requerida",
                    "Selecciona la ubicación del reporte usando tu GPS o tocando el mapa.",
                    "OK"
                );

                return;
            }

            string? sexoSeleccionado = SexoPicker.SelectedIndex >= 0
                ? SexoPicker.Items[SexoPicker.SelectedIndex]
                : null;

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
                especieReferencial: esMascotaEncontrada ? EspecieEntry.Text : null,
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
}