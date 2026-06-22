using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace MascotasTrujillo.App.Views;

public partial class MisMascotasPage : ContentPage
{
    private readonly ApiService _apiService;
    private Mascota? _mascotaSeleccionada;

    private bool _rastreoActivo = false;
    private bool _timerIniciado = false;

    public MisMascotasPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _rastreoActivo = true;

        await CargarMascotas();

        if (!_timerIniciado)
        {
            IniciarSeguimientoGpsPrivado();
            _timerIniciado = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _rastreoActivo = false;
    }

    private async Task CargarMascotas()
    {
        try
        {
            var lista = await _apiService.GetMisMascotasAsync();

            if (lista != null && lista.Count > 0)
            {
                MascotasCarousel.ItemsSource = lista;

                if (_mascotaSeleccionada == null)
                {
                    MascotasCarousel.SelectedItem = lista.First();
                }
            }
            else
            {
                MascotasCarousel.ItemsSource = null;
                MascotaMap.Pins.Clear();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudieron cargar tus mascotas: {ex.Message}", "OK");
        }
    }

    private async void OnMascotaSelected(object sender, SelectionChangedEventArgs e)
    {
        _mascotaSeleccionada = e.CurrentSelection.FirstOrDefault() as Mascota;

        if (_mascotaSeleccionada == null)
            return;

        if (_mascotaSeleccionada.Latitud.HasValue && _mascotaSeleccionada.Longitud.HasValue)
        {
            ActualizarMapa();
        }
        else
        {
            MascotaMap.Pins.Clear();

            await DisplayAlertAsync(
                "Sin ubicación GPS",
                "Esta mascota todavía no tiene una ubicación GPS registrada.",
                "OK"
            );
        }
    }

    private void IniciarSeguimientoGpsPrivado()
    {
        Dispatcher.StartTimer(TimeSpan.FromSeconds(10), () =>
        {
            if (!_rastreoActivo)
                return true;

            Task.Run(async () =>
            {
                try
                {
                    var listaActualizada = await _apiService.GetMisMascotasAsync();

                    if (listaActualizada == null)
                        return;

                    var mascotaActual = _mascotaSeleccionada;

                    if (mascotaActual == null)
                        return;

                    var mascotaActualizada = listaActualizada.FirstOrDefault(x => x.Id == mascotaActual.Id);

                    if (mascotaActualizada == null)
                        return;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        mascotaActual.Latitud = mascotaActualizada.Latitud;
                        mascotaActual.Longitud = mascotaActualizada.Longitud;
                        mascotaActual.UltimaActualizacion = mascotaActualizada.UltimaActualizacion;
                        mascotaActual.DispositivoId = mascotaActualizada.DispositivoId;

                        if (mascotaActual.Latitud.HasValue && mascotaActual.Longitud.HasValue)
                        {
                            ActualizarMapa();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error actualizando ubicación GPS: {ex.Message}");
                }
            });

            return true;
        });
    }

    private void ActualizarMapa()
    {
        var mascotaActual = _mascotaSeleccionada;

        if (mascotaActual?.Latitud == null || mascotaActual.Longitud == null)
            return;

        var ubicacion = new Location(
            mascotaActual.Latitud.Value,
            mascotaActual.Longitud.Value
        );

        MascotaMap.Pins.Clear();

        MascotaMap.Pins.Add(new Pin
        {
            Label = mascotaActual.Nombre,
            Address = mascotaActual.UltimaActualizacion.HasValue
                ? $"Última actualización: {mascotaActual.UltimaActualizacion.Value:dd/MM/yyyy HH:mm}"
                : "Última ubicación registrada",
            Location = ubicacion,
            Type = PinType.Place
        });

        MascotaMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                ubicacion,
                Distance.FromKilometers(0.5)
            )
        );
    }

    private async void OnAbrirRegistroMascotaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrarMascotaPage(_apiService));
    }
}