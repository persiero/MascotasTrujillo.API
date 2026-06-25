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

    public RadarPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        FiltroTipoPicker.SelectedIndex = 0;
        RadioPicker.SelectedIndex = 2;
        LblResumenFiltro.Text = "0 reportes activos";
    }

    private async void OnActualizarClicked(object? sender, EventArgs e)
    {
        await CargarRadarAsync();
    }

    private async Task CargarRadarAsync()
    {
        BtnActualizar.Text = "Buscando...";
        BtnActualizar.IsEnabled = false;

        try
        {
            var ubicacionActual = await ObtenerUbicacionActualAsync();

            if (ubicacionActual == null)
            {
                return;
            }

            _miLatitud = ubicacionActual.Latitude;
            _miLongitud = ubicacionActual.Longitude;

            double radioMetros = ObtenerRadioSeleccionado();

            _reportesCargados = await _apiService.ObtenerReportesCercanosAsync(
                _miLatitud,
                _miLongitud,
                radioMetros
            );

            AplicarFiltrosYActualizarVista(_miLatitud, _miLongitud, radioMetros);
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
    }

    private List<Reporte> FiltrarPorTipo(List<Reporte> reportes)
    {
        int filtro = FiltroTipoPicker.SelectedIndex;

        if (filtro == 1)
        {
            return reportes
                .Where(r => EsReportePerdida(r.TipoReporte))
                .ToList();
        }

        if (filtro == 2)
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

    private double ObtenerRadioSeleccionado()
    {
        return RadioPicker.SelectedIndex switch
        {
            0 => 1000,
            1 => 3000,
            2 => 5000,
            3 => 10000,
            _ => 5000
        };
    }

    private void OnFiltroTipoChanged(object sender, EventArgs e)
    {
        if (_reportesCargados.Count == 0)
            return;

        double radioMetros = ObtenerRadioSeleccionado();

        AplicarFiltrosYActualizarVista(_miLatitud, _miLongitud, radioMetros);
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