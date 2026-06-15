using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using System.Collections.ObjectModel;

namespace MascotasTrujillo.App.Views;

public partial class MisReportesPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<Avistamiento> _misPublicaciones;

    public MisReportesPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        _misPublicaciones = new ObservableCollection<Avistamiento>();
        MisReportesList.ItemsSource = _misPublicaciones;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarMisReportes();
    }

    private async Task CargarMisReportes()
    {
        try
        {
            var reportesServidor = await _apiService.GetMisReportesAsync();

            if (reportesServidor != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _misPublicaciones.Clear();
                    foreach (var reporte in reportesServidor)
                    {
                        _misPublicaciones.Add(reporte);
                    }
                    MisReportesList.ItemsSource = null;
                    MisReportesList.ItemsSource = _misPublicaciones;
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo conectar: {ex.Message}", "OK");
        }
    }

    private async void OnMascotaEncontradaClicked(object sender, EventArgs e)
    {
        var boton = (Button)sender;
        var mascota = (Avistamiento)boton.CommandParameter;

        if (mascota == null) return;

        bool respuesta = await DisplayAlertAsync("¡Qué alegría!", "¿Confirmas que encontraste a la mascota?", "Sí, Encontrada", "Cancelar");

        if (respuesta)
        {
            try
            {
                bool exito = await _apiService.MarcarComoResueltoAsync(mascota.Id);
                if (exito)
                {
                    await DisplayAlertAsync("Éxito", "El caso ha sido cerrado.", "OK");
                    await CargarMisReportes();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudo actualizar el estado: {ex.Message}", "OK");
            }
        }
    }
}