using MascotasTrujillo.App.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace MascotasTrujillo.App.Views;

public partial class RadarPage : ContentPage
{
    private readonly ApiService _apiService;

    // Pedimos el ApiService en el constructor
    public RadarPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnActualizarClicked(object? sender, EventArgs e)
    {
        BtnActualizar.Text = "Buscando...";
        BtnActualizar.IsEnabled = false;

        // Simulamos que el usuario está en el centro de Trujillo
        double miLatitud = -8.1118;
        double miLongitud = -79.0287;

        // ¡Disparamos la petición a tu API!
        var mascotasCercanas = await _apiService.ObtenerCercanosAsync(miLatitud, miLongitud);

        // 1. Limpiamos pines anteriores
        MapaMascotas.Pins.Clear();

        // 2. Agregamos un Pin por cada mascota encontrada
        foreach (var m in mascotasCercanas)
        {
            var pin = new Pin
            {
                Label = m.Descripcion ?? "Mascota reportada",
                Address = $"A {m.DistanciaMetros:N0} metros",
                Type = PinType.Place,
                Location = new Location(m.Latitud, m.Longitud)
            };
            MapaMascotas.Pins.Add(pin);
        }

        // 3. Centramos el mapa en tu ubicación
        var region = MapSpan.FromCenterAndRadius(new Location(miLatitud, miLongitud), Distance.FromKilometers(1.5));
        MapaMascotas.MoveToRegion(region);

        MascotasList.ItemsSource = mascotasCercanas;

        BtnActualizar.Text = "Actualizar Radar";
        BtnActualizar.IsEnabled = true;
    }

    private async void OnIrAReportarClicked(object? sender, EventArgs e)
    {
        // Navegamos a la nueva pantalla
        await Navigation.PushAsync(new ReportarPage(_apiService));
    }

    private async void OnMascotaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        // Si el usuario seleccionó algo válido
        if (e.CurrentSelection.FirstOrDefault() is Models.Avistamiento mascotaSeleccionada)
        {
            // Quitamos el color de selección gris feo por defecto
            MascotasList.SelectedItem = null;

            // ¡Hacemos el viaje a la nueva pantalla llevándonos la mascota!
            await Navigation.PushAsync(new MascotaDetailPage(mascotaSeleccionada));
        }
    }
}