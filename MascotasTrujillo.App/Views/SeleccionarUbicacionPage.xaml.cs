using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace MascotasTrujillo.App.Views;

public partial class SeleccionarUbicacionPage : ContentPage
{
    private readonly double _latitudInicial;
    private readonly double _longitudInicial;
    private readonly Action<double, double> _onUbicacionConfirmada;

    private double? _latitudSeleccionada;
    private double? _longitudSeleccionada;

    private bool _mapaInicializado = false;

    public SeleccionarUbicacionPage(
        double latitudInicial,
        double longitudInicial,
        Action<double, double> onUbicacionConfirmada,
        string titulo = "Seleccionar ubicación")
    {
        InitializeComponent();

        _latitudInicial = latitudInicial;
        _longitudInicial = longitudInicial;
        _onUbicacionConfirmada = onUbicacionConfirmada;

        Title = titulo;
        LblTitulo.Text = titulo;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_mapaInicializado)
        {
            CentrarMapa(_latitudInicial, _longitudInicial, 3);
            _mapaInicializado = true;
        }
    }

    private void OnMapaClicked(object sender, MapClickedEventArgs e)
    {
        SeleccionarUbicacion(
            e.Location.Latitude,
            e.Location.Longitude,
            "Ubicación seleccionada manualmente."
        );
    }

    private async void OnUsarGpsClicked(object sender, EventArgs e)
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

            SeleccionarUbicacion(
                location.Latitude,
                location.Longitude,
                "Ubicación obtenida desde tu GPS actual."
            );
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("GPS", $"No se pudo obtener la ubicación: {ex.Message}", "OK");
        }
    }

    private async void OnConfirmarClicked(object sender, EventArgs e)
    {
        if (!_latitudSeleccionada.HasValue || !_longitudSeleccionada.HasValue)
        {
            await DisplayAlertAsync(
                "Ubicación requerida",
                "Toca el mapa o usa tu GPS para seleccionar una ubicación.",
                "OK"
            );

            return;
        }

        _onUbicacionConfirmada.Invoke(
            _latitudSeleccionada.Value,
            _longitudSeleccionada.Value
        );

        await Navigation.PopAsync();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void SeleccionarUbicacion(double latitud, double longitud, string mensaje)
    {
        _latitudSeleccionada = latitud;
        _longitudSeleccionada = longitud;

        MapaUbicacion.Pins.Clear();

        var pin = new Pin
        {
            Label = "Ubicación seleccionada",
            Address = mensaje,
            Type = PinType.Place,
            Location = new Location(latitud, longitud)
        };

        MapaUbicacion.Pins.Add(pin);

        CentrarMapa(latitud, longitud, 1);

        LblUbicacion.Text =
            $"Ubicación seleccionada: Lat. {latitud:F6}, Long. {longitud:F6}";
    }

    private void CentrarMapa(double latitud, double longitud, double radioKm)
    {
        var region = MapSpan.FromCenterAndRadius(
            new Location(latitud, longitud),
            Distance.FromKilometers(radioKm)
        );

        MapaUbicacion.MoveToRegion(region);
    }
}