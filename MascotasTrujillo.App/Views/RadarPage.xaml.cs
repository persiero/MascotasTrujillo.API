using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace MascotasTrujillo.App.Views;

public partial class RadarPage : ContentPage
{
    private readonly ApiService _apiService;

    private List<Reporte> _reportesCargados = new();

    private double _miLatitud = -8.1118;
    private double _miLongitud = -79.0287;

    private string _tipoFiltro = "Todos";
    private double _radioMetros = 5000;

    private bool _cargandoRadar = false;
    private bool _radarInicializado = false;

    public RadarPage(ApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        LblResumenFiltro.Text = "0 reportes";
        LblResumenFiltroSuperior.Text = "5 km";

        ActualizarEstiloFiltros();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_radarInicializado)
            return;

        _radarInicializado = true;

        await CargarRadarAsync();
    }

    private async void OnActualizarClicked(object? sender, EventArgs e)
    {
        await CargarRadarAsync();
    }

    private async Task CargarRadarAsync()
    {
        if (_cargandoRadar)
            return;

        try
        {
            _cargandoRadar = true;

            BtnActualizar.Text = "Buscando...";
            BtnActualizar.IsEnabled = false;

            var ubicacionActual = await ObtenerUbicacionActualAsync();

            if (ubicacionActual == null)
            {
                return;
            }

            _miLatitud = ubicacionActual.Latitude;
            _miLongitud = ubicacionActual.Longitude;

            _reportesCargados = await _apiService.ObtenerReportesCercanosAsync(
                _miLatitud,
                _miLongitud,
                _radioMetros
            );

            AplicarFiltrosYActualizarVista(_miLatitud, _miLongitud, _radioMetros);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo actualizar el radar: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            _cargandoRadar = false;

            BtnActualizar.Text = "🔄 Actualizar";
            BtnActualizar.IsEnabled = true;
        }
    }

    private async Task<Location?> ObtenerUbicacionActualAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(
                    "Permiso requerido",
                    "Para usar el radar con tu ubicación real, debes permitir el acceso a la ubicación.",
                    "OK"
                );

                return null;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10)
                )
            );

            return location;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "GPS no disponible",
                $"No se pudo obtener tu ubicación actual: {ex.Message}",
                "OK"
            );

            return null;
        }
    }

    private void AplicarFiltrosYActualizarVista(
        double miLatitud,
        double miLongitud,
        double radioMetros)
    {
        var reportesFiltrados = FiltrarPorTipo(_reportesCargados);

        MapaMascotas.Pins.Clear();

        foreach (var r in reportesFiltrados)
        {
            var pin = new Pin
            {
                Label = r.Titulo,
                Address = $"{r.TipoReporte} - A {r.DistanciaMetros:N0} metros",
                Type = PinType.Place,
                Location = new Location(r.Latitud, r.Longitud)
            };

            MapaMascotas.Pins.Add(pin);
        }

        double radioKm = Math.Max(radioMetros / 1000.0, 1.5);

        var region = MapSpan.FromCenterAndRadius(
            new Location(miLatitud, miLongitud),
            Distance.FromKilometers(radioKm)
        );

        MapaMascotas.MoveToRegion(region);

        MascotasList.ItemsSource = reportesFiltrados;

        LblResumenFiltro.Text = reportesFiltrados.Count == 1
            ? "1 reporte"
            : $"{reportesFiltrados.Count} reportes activos";

        LblResumenFiltroSuperior.Text = $"{radioMetros / 1000:0} km";
    }

    private List<Reporte> FiltrarPorTipo(List<Reporte> reportes)
    {
        if (_tipoFiltro == "Perdidas")
        {
            return reportes
                .Where(r => EsReportePerdida(r.TipoReporte))
                .ToList();
        }

        if (_tipoFiltro == "Encontradas")
        {
            return reportes
                .Where(r => EsReporteEncontrada(r.TipoReporte))
                .ToList();
        }

        return reportes;
    }

    private bool EsReportePerdida(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;

        return valor.Equals("Perdida", StringComparison.OrdinalIgnoreCase) ||
               valor.Equals("Mascota perdida", StringComparison.OrdinalIgnoreCase) ||
               valor.Contains("perdid", StringComparison.OrdinalIgnoreCase);
    }

    private bool EsReporteEncontrada(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;

        return valor.Equals("Encontrada", StringComparison.OrdinalIgnoreCase) ||
               valor.Equals("Mascota encontrada", StringComparison.OrdinalIgnoreCase) ||
               valor.Contains("encontrad", StringComparison.OrdinalIgnoreCase);
    }

    private void ActualizarEstiloFiltros()
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

        AplicarEstado(BtnTipoTodos, _tipoFiltro == "Todos");
        AplicarEstado(BtnTipoPerdidas, _tipoFiltro == "Perdidas");
        AplicarEstado(BtnTipoEncontradas, _tipoFiltro == "Encontradas");

        AplicarEstado(BtnRadio1, _radioMetros == 1000);
        AplicarEstado(BtnRadio3, _radioMetros == 3000);
        AplicarEstado(BtnRadio5, _radioMetros == 5000);
        AplicarEstado(BtnRadio10, _radioMetros == 10000);

        LblResumenFiltroSuperior.Text = $"{_radioMetros / 1000:0} km";
    }

    private void AplicarFiltroTipoSinRecargar()
    {
        ActualizarEstiloFiltros();

        if (_reportesCargados.Count == 0)
        {
            LblResumenFiltro.Text = "0 reportes";
            return;
        }

        AplicarFiltrosYActualizarVista(_miLatitud, _miLongitud, _radioMetros);
    }

    private void OnTipoTodosClicked(object sender, EventArgs e)
    {
        _tipoFiltro = "Todos";
        AplicarFiltroTipoSinRecargar();
    }

    private void OnTipoPerdidasClicked(object sender, EventArgs e)
    {
        _tipoFiltro = "Perdidas";
        AplicarFiltroTipoSinRecargar();
    }

    private void OnTipoEncontradasClicked(object sender, EventArgs e)
    {
        _tipoFiltro = "Encontradas";
        AplicarFiltroTipoSinRecargar();
    }

    private async void OnRadio1Clicked(object sender, EventArgs e)
    {
        _radioMetros = 1000;
        ActualizarEstiloFiltros();
        await CargarRadarAsync();
    }

    private async void OnRadio3Clicked(object sender, EventArgs e)
    {
        _radioMetros = 3000;
        ActualizarEstiloFiltros();
        await CargarRadarAsync();
    }

    private async void OnRadio5Clicked(object sender, EventArgs e)
    {
        _radioMetros = 5000;
        ActualizarEstiloFiltros();
        await CargarRadarAsync();
    }

    private async void OnRadio10Clicked(object sender, EventArgs e)
    {
        _radioMetros = 10000;
        ActualizarEstiloFiltros();
        await CargarRadarAsync();
    }

    private async void OnIrAReportarClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ReportarPage(_apiService));
    }

    private async void OnMascotaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Reporte reporteSeleccionado)
        {
            MascotasList.SelectedItem = null;

            await Navigation.PushAsync(new DetalleReportePage(_apiService, reporteSeleccionado));
        }
    }
}