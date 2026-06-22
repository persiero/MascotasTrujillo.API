using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using System.Collections.ObjectModel;

namespace MascotasTrujillo.App.Views;

public partial class MisReportesPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<Reporte> _misPublicaciones;

    public MisReportesPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        _misPublicaciones = new ObservableCollection<Reporte>();
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

    private async void OnMarcarResueltoClicked(object sender, EventArgs e)
    {
        var boton = (Button)sender;
        var reporte = boton.CommandParameter as Reporte;

        if (reporte == null) return;

        bool respuesta = await DisplayAlertAsync(
            "Confirmar",
            "¿Confirmas que este reporte ya fue resuelto?",
            "Sí, resolver",
            "Cancelar"
        );

        if (respuesta)
        {
            try
            {
                bool exito = await _apiService.MarcarReporteComoResueltoAsync(reporte.Id);

                if (exito)
                {
                    await DisplayAlertAsync("Éxito", "El reporte ha sido cerrado.", "OK");
                    await CargarMisReportes();
                }
                else
                {
                    await DisplayAlertAsync("Error", "No se pudo cerrar el reporte.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudo actualizar el estado: {ex.Message}", "OK");
            }
        }
    }
}