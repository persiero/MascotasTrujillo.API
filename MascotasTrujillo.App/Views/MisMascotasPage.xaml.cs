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

    private void ActualizarEstadoBotonesMascota()
    {
        bool hayMascotaSeleccionada = _mascotaSeleccionada != null;
        bool tieneGps = hayMascotaSeleccionada &&
                        !string.IsNullOrWhiteSpace(_mascotaSeleccionada?.DispositivoId);

        BtnEditarMascota.IsEnabled = hayMascotaSeleccionada;
        BtnDesactivarMascota.IsEnabled = hayMascotaSeleccionada;
        BtnHistorialGps.IsEnabled = tieneGps;

        BtnEditarMascota.Opacity = hayMascotaSeleccionada ? 1 : 0.45;
        BtnDesactivarMascota.Opacity = hayMascotaSeleccionada ? 1 : 0.45;
        BtnHistorialGps.Opacity = tieneGps ? 1 : 0.45;
    }

    private async Task CargarMascotas()
    {
        try
        {
            var lista = await _apiService.GetMisMascotasAsync();

            if (lista != null && lista.Count > 0)
            {
                LblConteoMascotas.Text = lista.Count == 1
                    ? "1 mascota"
                    : $"{lista.Count} mascotas";

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

                LblConteoMascotas.Text = "0 mascotas";
                LblTituloMapa.Text = "Sin mascota seleccionada";
                LblNotaSeguimiento.Text = "Registra una mascota para consultar su seguimiento GPS privado.";

                _mascotaSeleccionada = null;
                ActualizarEstadoBotonesMascota();
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

        ActualizarEstadoBotonesMascota();

        if (_mascotaSeleccionada == null)
            return;

        if (_mascotaSeleccionada.Latitud.HasValue && _mascotaSeleccionada.Longitud.HasValue)
        {
            ActualizarMapa();
        }
        else
        {
            MostrarMascotaSinUbicacion();
        }
    }

    private async void OnDesactivarMascotaClicked(object sender, EventArgs e)
    {
        if (_mascotaSeleccionada == null)
        {
            await DisplayAlertAsync("Aviso", "Selecciona una mascota primero.", "OK");
            return;
        }

        bool confirmar = await DisplayAlertAsync(
            "Desactivar mascota",
            $"¿Deseas desactivar a {_mascotaSeleccionada.Nombre}? Ya no aparecerá en tu lista de mascotas.",
            "Sí, desactivar",
            "Cancelar"
        );

        if (!confirmar)
            return;

        try
        {
            var resultado = await _apiService.DesactivarMascotaAsync(_mascotaSeleccionada.Id);

            if (resultado.Exito)
            {
                await DisplayAlertAsync("Éxito", "La mascota fue desactivada correctamente.", "OK");

                _mascotaSeleccionada = null;
                MascotaMap.Pins.Clear();
                ActualizarEstadoBotonesMascota();

                await CargarMascotas();
            }
            else
            {
                await DisplayAlertAsync(
                    "No se pudo desactivar",
                    resultado.Mensaje,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo desactivar la mascota: {ex.Message}", "OK");
        }
    }

    private async void OnEditarMascotaClicked(object sender, EventArgs e)
    {
        if (_mascotaSeleccionada == null)
        {
            await DisplayAlertAsync("Aviso", "Selecciona una mascota primero.", "OK");
            return;
        }

        await Navigation.PushAsync(new RegistrarMascotaPage(_apiService, _mascotaSeleccionada));
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
                        mascotaActual.EstadoConexionGps = mascotaActualizada.EstadoConexionGps;
                        mascotaActual.BateriaGps = mascotaActualizada.BateriaGps;

                        ActualizarEstadoBotonesMascota();

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
            Address = mascotaActual.UltimaUbicacionTexto,
            Location = ubicacion,
            Type = PinType.Place
        });

        LblTituloMapa.Text = $"Última ubicación de {mascotaActual.Nombre}";

        LblNotaSeguimiento.Text = mascotaActual.UltimaUbicacionTexto;

        MascotaMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                ubicacion,
                Distance.FromKilometers(0.5)
            )
        );
    }

    private void MostrarMascotaSinUbicacion()
    {
        MascotaMap.Pins.Clear();

        if (_mascotaSeleccionada == null)
        {
            LblTituloMapa.Text = "Sin mascota seleccionada";
            LblNotaSeguimiento.Text = "Selecciona una mascota para consultar su última ubicación GPS.";
            return;
        }

        LblTituloMapa.Text = "Sin ubicación GPS";

        LblNotaSeguimiento.Text = _mascotaSeleccionada.UltimaUbicacionTexto;

        var ubicacionReferencia = new Location(-8.1118, -79.0287);

        MascotaMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                ubicacionReferencia,
                Distance.FromKilometers(3)
            )
        );
    }

    private async void OnAbrirRegistroMascotaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrarMascotaPage(_apiService));
    }

    private async void OnHistorialGpsClicked(object sender, EventArgs e)
    {
        if (_mascotaSeleccionada == null)
        {
            await DisplayAlertAsync(
                "Aviso",
                "Selecciona una mascota primero.",
                "OK"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(_mascotaSeleccionada.DispositivoId))
        {
            await DisplayAlertAsync(
                "Sin GPS",
                "Esta mascota no tiene un collar GPS asociado.",
                "OK"
            );

            return;
        }

        await Navigation.PushAsync(
            new HistorialGpsPage(_apiService, _mascotaSeleccionada)
        );
    }

}