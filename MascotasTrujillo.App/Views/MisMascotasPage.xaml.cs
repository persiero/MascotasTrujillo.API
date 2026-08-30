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
    private bool _cargandoMascotas = false;

    private bool _mapaCentradoInicialmente = false;
    private long? _ultimaMascotaCentradaId = null;

    public MisMascotasPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _rastreoActivo = true;

        await CargarMascotas(
            mostrarErrores: true,
            centrarMapa: true,
            limpiarSiVacio: true
        );

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

        BtnAccionesMascota.IsEnabled = hayMascotaSeleccionada;
        BtnAccionesMascota.Opacity = hayMascotaSeleccionada ? 1 : 0.45;
    }

    private async Task CargarMascotas(
    bool mostrarErrores = true,
    bool centrarMapa = false,
    bool limpiarSiVacio = true)
    {
        if (_cargandoMascotas)
            return;

        _cargandoMascotas = true;

        try
        {
            long? mascotaSeleccionadaId = _mascotaSeleccionada?.Id;

            var lista = await _apiService.GetMisMascotasAsync();

            if (lista == null)
            {
                if (mostrarErrores)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudieron cargar tus mascotas.",
                        "OK"
                    );
                }

                return;
            }

            if (lista.Count > 0)
            {
                LblConteoMascotas.Text = lista.Count == 1
                    ? "1 mascota"
                    : $"{lista.Count} mascotas";

                MascotasCarousel.ItemsSource = lista;

                Mascota? mascotaParaSeleccionar = null;

                if (mascotaSeleccionadaId.HasValue)
                {
                    mascotaParaSeleccionar = lista.FirstOrDefault(
                        x => x.Id == mascotaSeleccionadaId.Value
                    );
                }

                mascotaParaSeleccionar ??= lista.First();

                bool cambioMascota =
                    _mascotaSeleccionada == null ||
                    _mascotaSeleccionada.Id != mascotaParaSeleccionar.Id;

                _mascotaSeleccionada = mascotaParaSeleccionar;
                MascotasCarousel.SelectedItem = mascotaParaSeleccionar;

                ActualizarEstadoBotonesMascota();

                if (_mascotaSeleccionada.Latitud.HasValue &&
                    _mascotaSeleccionada.Longitud.HasValue)
                {
                    ActualizarMapa(centrarMapa: centrarMapa || cambioMascota);
                }
                else
                {
                    MostrarMascotaSinUbicacion(centrarMapa: centrarMapa || cambioMascota);
                }

                return;
            }

            if (!limpiarSiVacio)
                return;

            MascotasCarousel.ItemsSource = null;
            MascotaMap.Pins.Clear();

            LblConteoMascotas.Text = "0 mascotas";
            LblTituloMapa.Text = "Sin mascota seleccionada";
            LblNotaSeguimiento.Text = "Registra una mascota para consultar su seguimiento GPS privado.";

            _mascotaSeleccionada = null;
            _mapaCentradoInicialmente = false;
            _ultimaMascotaCentradaId = null;

            ActualizarEstadoBotonesMascota();
        }
        catch (Exception ex)
        {
            if (mostrarErrores)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron cargar tus mascotas: {ex.Message}",
                    "OK"
                );
            }
            else
            {
                Console.WriteLine($"Error actualizando mascotas/GPS: {ex.Message}");
            }
        }
        finally
        {
            _cargandoMascotas = false;
        }
    }

    private async void OnMascotaSelected(object sender, SelectionChangedEventArgs e)
    {
        var mascotaSeleccionada = e.CurrentSelection.FirstOrDefault() as Mascota;

        if (mascotaSeleccionada == null)
        {
            _mascotaSeleccionada = null;
            ActualizarEstadoBotonesMascota();
            MostrarMascotaSinUbicacion(centrarMapa: false);
            return;
        }

        bool cambioMascota =
            _mascotaSeleccionada == null ||
            _mascotaSeleccionada.Id != mascotaSeleccionada.Id;

        _mascotaSeleccionada = mascotaSeleccionada;

        ActualizarEstadoBotonesMascota();

        if (_mascotaSeleccionada.Latitud.HasValue &&
            _mascotaSeleccionada.Longitud.HasValue)
        {
            ActualizarMapa(centrarMapa: cambioMascota);
        }
        else
        {
            MostrarMascotaSinUbicacion(centrarMapa: cambioMascota);
        }
    }

    private void OnMascotaCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Mascota mascota)
            return;

        _mascotaSeleccionada = mascota;
        MascotasCarousel.SelectedItem = mascota;

        ActualizarEstadoBotonesMascota();

        if (_mascotaSeleccionada.Latitud.HasValue &&
            _mascotaSeleccionada.Longitud.HasValue)
        {
            ActualizarMapa(centrarMapa: true);
        }
        else
        {
            MostrarMascotaSinUbicacion(centrarMapa: true);
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

                await CargarMascotas(
                    mostrarErrores: true,
                    centrarMapa: true,
                    limpiarSiVacio: true
                );
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

            _ = RefrescarGpsPrivadoAsync();

            return true;
        });
    }

    private async Task RefrescarGpsPrivadoAsync()
    {
        if (!_rastreoActivo)
            return;

        try
        {
            await CargarMascotas(
                mostrarErrores: false,
                centrarMapa: false,
                limpiarSiVacio: false
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en refresco GPS privado: {ex.Message}");
        }
    }

    private void ActualizarMapa(bool centrarMapa = false)
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

        LblTituloMapa.Text = $"Rastreo GPS de {mascotaActual.Nombre}";
        LblNotaSeguimiento.Text = mascotaActual.UltimaUbicacionTexto;

        bool cambioMascota = _ultimaMascotaCentradaId != mascotaActual.Id;

        if (centrarMapa || !_mapaCentradoInicialmente || cambioMascota)
        {
            MascotaMap.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    ubicacion,
                    Distance.FromMeters(350)
                )
            );

            _mapaCentradoInicialmente = true;
            _ultimaMascotaCentradaId = mascotaActual.Id;
        }
    }

    private void MostrarMascotaSinUbicacion(bool centrarMapa = false)
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

        if (!centrarMapa && _mapaCentradoInicialmente)
            return;

        var ubicacionReferencia = new Location(-8.1118, -79.0287);

        MascotaMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                ubicacionReferencia,
                Distance.FromKilometers(3)
            )
        );

        _mapaCentradoInicialmente = true;
        _ultimaMascotaCentradaId = _mascotaSeleccionada.Id;
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

    private async void OnAccionesMascotaClicked(object sender, EventArgs e)
    {
        if (_mascotaSeleccionada == null)
        {
            await DisplayAlertAsync("Aviso", "Selecciona una mascota primero.", "OK");
            return;
        }

        LblAccionesMascotaTitulo.Text = _mascotaSeleccionada.Nombre;

        bool tieneGps = !string.IsNullOrWhiteSpace(_mascotaSeleccionada.DispositivoId);
        BtnAccionHistorialGps.IsVisible = tieneGps;

        AccionesMascotaOverlay.IsVisible = true;
        AccionesMascotaOverlay.Opacity = 0;

        await AccionesMascotaOverlay.FadeToAsync(1, 150);
    }

    private async void OnCerrarAccionesMascotaClicked(object sender, EventArgs e)
    {
        await CerrarAccionesMascotaOverlayAsync();
    }

    private async Task CerrarAccionesMascotaOverlayAsync()
    {
        await AccionesMascotaOverlay.FadeToAsync(0, 120);
        AccionesMascotaOverlay.IsVisible = false;
        AccionesMascotaOverlay.Opacity = 0;
    }

    private async void OnAccionEditarMascotaClicked(object sender, EventArgs e)
    {
        await CerrarAccionesMascotaOverlayAsync();
        OnEditarMascotaClicked(sender, e);
    }

    private async void OnAccionHistorialGpsClicked(object sender, EventArgs e)
    {
        await CerrarAccionesMascotaOverlayAsync();
        OnHistorialGpsClicked(sender, e);
    }

    private async void OnAccionDesactivarMascotaClicked(object sender, EventArgs e)
    {
        await CerrarAccionesMascotaOverlayAsync();
        OnDesactivarMascotaClicked(sender, e);
    }

}