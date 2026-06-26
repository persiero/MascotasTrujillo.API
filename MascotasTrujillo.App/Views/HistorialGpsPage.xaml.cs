using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using System.Collections.ObjectModel;

namespace MascotasTrujillo.App.Views;

public partial class HistorialGpsPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Mascota _mascota;

    private readonly ObservableCollection<UbicacionGpsHistorial> _historial = new();

    public HistorialGpsPage(ApiService apiService, Mascota mascota)
    {
        InitializeComponent();

        _apiService = apiService;
        _mascota = mascota;

        HistorialList.ItemsSource = _historial;

        LblTitulo.Text = $"Historial GPS de {_mascota.Nombre}";
        LblSubtitulo.Text = string.IsNullOrWhiteSpace(_mascota.DispositivoId)
            ? "Esta mascota no tiene un collar GPS asociado."
            : $"Últimas ubicaciones registradas por el collar {_mascota.DispositivoId}.";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarHistorialAsync();
    }

    private async Task CargarHistorialAsync()
    {
        try
        {
            var lista = await _apiService.ObtenerHistorialGpsMascotaAsync(_mascota.Id);

            _historial.Clear();

            if (lista != null && lista.Count > 0)
            {
                foreach (var item in lista)
                {
                    _historial.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo cargar el historial GPS: {ex.Message}",
                "OK"
            );
        }
    }

    private async void OnVerMapaClicked(object sender, EventArgs e)
    {
        if (sender is not Button boton)
            return;

        if (boton.CommandParameter is not UbicacionGpsHistorial ubicacion)
            return;

        try
        {
            string url = $"https://www.google.com/maps/search/?api=1&query={ubicacion.Latitud},{ubicacion.Longitud}";
            await Launcher.Default.OpenAsync(url);
        }
        catch
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudo abrir la ubicación en el mapa.",
                "OK"
            );
        }
    }
}