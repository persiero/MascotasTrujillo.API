using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace MascotasTrujillo.App.Views;

public partial class RadarPage : ContentPage
{
    private readonly ApiService _apiService;

    public RadarPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnActualizarClicked(object? sender, EventArgs e)
    {
        BtnActualizar.Text = "Buscando...";
        BtnActualizar.IsEnabled = false;

        double miLatitud = -8.1118;
        double miLongitud = -79.0287;

        var reportesCercanos = await _apiService.ObtenerReportesCercanosAsync(miLatitud, miLongitud,100000);

        MapaMascotas.Pins.Clear();

        foreach (var r in reportesCercanos)
        {
            var pin = new Pin
            {
                Label = r.Titulo,
                Address = $"A {r.DistanciaMetros:N0} metros",
                Type = PinType.Place,
                Location = new Location(r.Latitud, r.Longitud)
            };

            MapaMascotas.Pins.Add(pin);
        }

        var region = MapSpan.FromCenterAndRadius(
            new Location(miLatitud, miLongitud),
            Distance.FromKilometers(1.5)
        );

        MapaMascotas.MoveToRegion(region);

        MascotasList.ItemsSource = reportesCercanos;

        BtnActualizar.Text = "Actualizar Radar";
        BtnActualizar.IsEnabled = true;
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