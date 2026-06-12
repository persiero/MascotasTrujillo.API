using MascotasTrujillo.App.Services;
using MascotasTrujillo.App.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;

namespace MascotasTrujillo.App.Views;

public partial class MisMascotasPage : ContentPage
{
    private readonly ApiService _apiService;
    private Mascota? _mascotaSeleccionada;
    private bool _isTimerRunning = true;

    public MisMascotasPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarMascotas();
        IniciarMotorDeRastreo();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isTimerRunning = false;
    }

    private async Task CargarMascotas()
    {
        var lista = await _apiService.GetMisMascotasAsync();
        if (lista != null && lista.Count > 0)
        {
            MascotasCarousel.ItemsSource = lista;
            MascotasCarousel.SelectedItem = lista.First();
        }
    }

    private void OnMascotaSelected(object sender, SelectionChangedEventArgs e)
    {
        _mascotaSeleccionada = e.CurrentSelection.FirstOrDefault() as Mascota;
        if (_mascotaSeleccionada != null && _mascotaSeleccionada.Latitud.HasValue)
        {
            ActualizarMapa();
        }
    }

    private void IniciarMotorDeRastreo()
    {
        _isTimerRunning = true;
        Dispatcher.StartTimer(TimeSpan.FromSeconds(10), () =>
        {
            Task.Run(async () =>
            {
                var listaActualizada = await _apiService.GetMisMascotasAsync();

                if (listaActualizada != null)
                {
                    // SOLUCIÓN DE NULOS: Copia local segura para evitar cambios asíncronos externos
                    var mascotaActual = _mascotaSeleccionada;

                    if (mascotaActual != null)
                    {
                        var pet = listaActualizada.FirstOrDefault(x => x.Id == mascotaActual.Id);
                        if (pet != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                mascotaActual.Latitud = pet.Latitud;
                                mascotaActual.Longitud = pet.Longitud;
                                ActualizarMapa();
                            });
                        }
                    }
                }
            });
            return _isTimerRunning;
        });
    }

    private void ActualizarMapa()
    {
        // SOLUCIÓN DE NULOS: Evaluamos la copia local de la mascota de forma estricta
        var mascotaActual = _mascotaSeleccionada;
        if (mascotaActual?.Latitud == null || mascotaActual.Longitud == null) return;

        var loc = new Location(mascotaActual.Latitud.Value, mascotaActual.Longitud.Value);

        MascotaMap.Pins.Clear();
        MascotaMap.Pins.Add(new Pin
        {
            Label = mascotaActual.Nombre,
            Location = loc,
            Type = PinType.Place
        });

        MascotaMap.MoveToRegion(MapSpan.FromCenterAndRadius(loc, Distance.FromKilometers(0.5)));
    }
}