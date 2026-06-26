using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using System.Collections.ObjectModel;


namespace MascotasTrujillo.App.Views;

public partial class DetalleReportePage : ContentPage
{
    private readonly Reporte _reporteActual;
    private readonly ApiService _apiService;
    private readonly ObservableCollection<Avistamiento> _avistamientos = new();

    private bool _esReportePropio = false;

    public DetalleReportePage(ApiService apiService, Reporte reporte)
    {
        InitializeComponent();

        _apiService = apiService;
        _reporteActual = reporte;
        BindingContext = reporte;

        AvistamientosList.ItemsSource = _avistamientos;

        CargarDatos();
    }

    private bool PuedeRegistrarAvistamiento()
    {
        string tipo = _reporteActual.TipoReporte?.Trim() ?? string.Empty;
        string estado = _reporteActual.EstadoReporte?.Trim() ?? string.Empty;

        bool esReportePerdida =
            tipo.Equals("Perdida", StringComparison.OrdinalIgnoreCase) ||
            tipo.Equals("Mascota perdida", StringComparison.OrdinalIgnoreCase) ||
            tipo.Contains("perdid", StringComparison.OrdinalIgnoreCase);

        bool estaActivo =
            estado.Equals("Activo", StringComparison.OrdinalIgnoreCase);

        return esReportePerdida && estaActivo;
    }

    private void CargarDatos()
    {
        if (string.IsNullOrWhiteSpace(_reporteActual.FotoUrl))
        {
            FotoReporteImage.Source = "https://cdn-icons-png.flaticon.com/512/616/616408.png";
        }

        if (_reporteActual.DistanciaMetros > 0)
        {
            LblDistancia.Text = $"A {_reporteActual.DistanciaMetros:N0} metros de tu ubicación.";
        }
        else
        {
            LblDistancia.Text = "Ubicación registrada en el reporte.";
        }

        LblNombreMascota.Text = TextoONoRegistrado(_reporteActual.NombreMascotaReferencial);
        LblEspecie.Text = TextoONoRegistrado(_reporteActual.EspecieReferencial);
        LblRaza.Text = TextoONoRegistrado(_reporteActual.RazaReferencial);
        LblColor.Text = TextoONoRegistrado(_reporteActual.ColorReferencial);
        LblSexo.Text = TextoONoRegistrado(_reporteActual.SexoReferencial);

        SeccionSalud.IsVisible = EsReportePerdida() && _reporteActual.TieneInformacionSalud;

        LblEnfermedades.Text = TextoONoRegistrado(_reporteActual.Enfermedades);
        LblDiscapacidades.Text = TextoONoRegistrado(_reporteActual.Discapacidades);
        LblTratamientos.Text = TextoONoRegistrado(_reporteActual.Tratamientos);
        LblNecesidadesEspeciales.Text = TextoONoRegistrado(_reporteActual.NecesidadesEspeciales);
        LblObservacionesSalud.Text = TextoONoRegistrado(_reporteActual.ObservacionesSalud);

        ConfigurarAccionesSegunUsuario();

        SeccionAvistamientos.IsVisible = EsReportePerdida();

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _esReportePropio = await EsReportePropioAsync();

        ConfigurarAccionesSegunUsuario();

        await CargarAvistamientosAsync();
    }

    private bool EsReportePerdida()
    {
        string tipo = _reporteActual.TipoReporte?.Trim() ?? string.Empty;

        return tipo.Equals("Perdida", StringComparison.OrdinalIgnoreCase) ||
               tipo.Equals("Mascota perdida", StringComparison.OrdinalIgnoreCase) ||
               tipo.Contains("perdid", StringComparison.OrdinalIgnoreCase);
    }

    private string TextoONoRegistrado(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor;
    }

    private async void OnVerUbicacionClicked(object sender, EventArgs e)
    {
        try
        {
            string url = $"https://www.google.com/maps/search/?api=1&query={_reporteActual.Latitud},{_reporteActual.Longitud}";
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la ubicación en el mapa.", "OK");
        }
    }

    private async void OnRegistrarAvistamientoClicked(object sender, EventArgs e)
    {
        if (await EsReportePropioAsync())
        {
            await DisplayAlertAsync(
                "Acción no disponible",
                "No puedes registrar avistamientos sobre un reporte creado por ti.",
                "OK"
            );

            return;
        }

        if (!PuedeRegistrarAvistamiento())
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes registrar avistamientos en reportes activos de mascotas perdidas.",
                "OK"
            );

            return;
        }

        await Navigation.PushAsync(new RegistrarAvistamientoPage(_apiService, _reporteActual));
    }

    private async Task CargarAvistamientosAsync()
    {
        if (!EsReportePerdida())
        {
            SeccionAvistamientos.IsVisible = false;
            return;
        }

        SeccionAvistamientos.IsVisible = true;

        var lista = await _apiService.ObtenerAvistamientosPorReporteAsync(_reporteActual.Id);

        _avistamientos.Clear();

        if (lista != null && lista.Count > 0)
        {
            foreach (var avistamiento in lista)
            {
                _avistamientos.Add(avistamiento);
            }

            LblAvistamientosResumen.Text = lista.Count == 1
                ? "1 avistamiento registrado para este reporte."
                : $"{lista.Count} avistamientos registrados para este reporte.";
        }
        else
        {
            LblAvistamientosResumen.Text = "Aún no hay avistamientos registrados para este reporte.";
        }
    }

    private async void OnActualizarAvistamientosClicked(object sender, EventArgs e)
    {
        await CargarAvistamientosAsync();
    }

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        if (await EsReportePropioAsync())
        {
            await DisplayAlertAsync(
                "Acción no disponible",
                "No puedes contactarte por WhatsApp sobre un reporte creado por ti.",
                "OK"
            );

            return;
        }

        try
        {
            string numeroTelefono = LimpiarNumeroWhatsapp(_reporteActual.TelefonoContacto);

            if (string.IsNullOrWhiteSpace(numeroTelefono))
            {
                await DisplayAlertAsync(
                    "Teléfono no disponible",
                    "El usuario que creó este reporte no tiene un número de WhatsApp registrado.",
                    "OK"
                );

                return;
            }

            string nombreContacto = string.IsNullOrWhiteSpace(_reporteActual.NombreContacto)
                ? "el usuario"
                : _reporteActual.NombreContacto;

            string mensaje =
                $"¡Hola {nombreContacto}! Vi tu reporte \"{_reporteActual.Titulo}\" en Mascotas Trujillo. ¿Me puedes brindar más información?";

            string mensajeCodificado = Uri.EscapeDataString(mensaje);
            string url = $"https://wa.me/{numeroTelefono}?text={mensajeCodificado}";

            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudo abrir WhatsApp. Verifica si está instalado en este dispositivo.",
                "OK"
            );
        }
    }

    private string LimpiarNumeroWhatsapp(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return string.Empty;

        string numeroLimpio = new string(
            telefono.Where(char.IsDigit).ToArray()
        );

        if (string.IsNullOrWhiteSpace(numeroLimpio))
            return string.Empty;

        // Si el usuario guardó solo 9 dígitos peruanos, agregamos código de país 51.
        if (numeroLimpio.Length == 9)
            numeroLimpio = "51" + numeroLimpio;

        return numeroLimpio;
    }

    private async Task<long?> ObtenerUsuarioIdActualAsync()
    {
        try
        {
            string? usuarioIdTexto = await SecureStorage.Default.GetAsync("usuario_id");

            if (long.TryParse(usuarioIdTexto, out long usuarioId))
                return usuarioId;

            var perfil = await _apiService.ObtenerPerfilAsync();

            if (perfil != null)
            {
                await SecureStorage.Default.SetAsync("usuario_id", perfil.Id.ToString());
                return perfil.Id;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> EsReportePropioAsync()
    {
        long? usuarioIdActual = await ObtenerUsuarioIdActualAsync();

        if (!usuarioIdActual.HasValue)
            return false;

        return _reporteActual.UsuarioId == usuarioIdActual.Value;
    }

    private void ConfigurarAccionesSegunUsuario()
    {
        bool puedeRegistrarAvistamientoBase = PuedeRegistrarAvistamiento();
        bool puedeRegistrarAvistamientoFinal = puedeRegistrarAvistamientoBase && !_esReportePropio;

        BtnRegistrarAvistamiento.IsVisible = puedeRegistrarAvistamientoFinal;
        BtnWhatsApp.IsVisible = !_esReportePropio;
        LblReportePropio.IsVisible = _esReportePropio;

        if (_esReportePropio)
        {
            LblAvistamientoNoDisponible.IsVisible = true;
            LblAvistamientoNoDisponible.Text =
                "Este reporte fue creado por ti. No puedes registrar avistamientos sobre tu propio reporte.";
        }
        else
        {
            LblAvistamientoNoDisponible.IsVisible = !puedeRegistrarAvistamientoBase;
            LblAvistamientoNoDisponible.Text =
                "Los avistamientos solo pueden registrarse en reportes activos de mascotas perdidas.";
        }
    }

}