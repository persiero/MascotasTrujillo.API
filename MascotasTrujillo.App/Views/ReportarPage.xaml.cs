using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class ReportarPage : ContentPage
{
    private readonly ApiService _apiService;

    private FileResult? _fotoCapturada;
    private List<Mascota> _misMascotas = new();
    private Mascota? _mascotaSeleccionada;

    public ReportarPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        BtnEnviar.IsEnabled = true;
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

    private void OnTipoReporteChanged(object sender, EventArgs e)
    {
        bool esMascotaPerdida = TipoReportePicker.SelectedIndex == 0;
        bool esMascotaEncontrada = TipoReportePicker.SelectedIndex == 1;

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

        LblResumenMascotaSeleccionada.Text =
            $"Mascota seleccionada: {_mascotaSeleccionada.Nombre}. Sus datos y foto se usarán en el reporte.";
    }

    private void HabilitarCamposMascota(bool habilitar)
    {
        NombreMascotaEntry.IsEnabled = habilitar;
        EspecieEntry.IsEnabled = habilitar;
        RazaEntry.IsEnabled = habilitar;
        ColorEntry.IsEnabled = habilitar;
        SexoPicker.IsEnabled = habilitar;
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

            if (location == null)
            {
                await DisplayAlertAsync("Error GPS", "No se pudo obtener la ubicación actual.", "OK");
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
                latitud: location.Latitude,
                longitud: location.Longitude,
                direccionReferencia: DireccionReferenciaEntry.Text,
                foto: esMascotaEncontrada ? _fotoCapturada : null,
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
            BtnEnviar.Text = "📍 Enviar reporte con GPS";
        }
    }
}