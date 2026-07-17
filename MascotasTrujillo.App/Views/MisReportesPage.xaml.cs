using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using System.Collections.ObjectModel;

namespace MascotasTrujillo.App.Views;

public partial class MisReportesPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<Reporte> _misPublicaciones;
    private bool _cargandoReportes = false;

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

    private async void OnActualizarClicked(object sender, EventArgs e)
    {
        await CargarMisReportes();
    }

    private async Task CargarMisReportes()
    {
        if (_cargandoReportes)
            return;

        try
        {
            _cargandoReportes = true;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var reportesServidor = await _apiService.GetMisReportesAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _misPublicaciones.Clear();

                if (reportesServidor != null)
                {
                    foreach (var reporte in reportesServidor)
                    {
                        _misPublicaciones.Add(reporte);
                    }
                }

                LblConteoReportes.Text = _misPublicaciones.Count == 1
                    ? "1 reporte"
                    : $"{_misPublicaciones.Count} reportes";
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo conectar: {ex.Message}", "OK");
        }
        finally
        {
            _cargandoReportes = false;

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }


    private async void OnVerDetalleClicked(object sender, EventArgs e)
    {
        var boton = (Button)sender;
        var reporte = boton.CommandParameter as Reporte;

        if (reporte == null)
            return;

        await Navigation.PushAsync(new DetalleReportePage(_apiService, reporte));
    }

    private async void OnMarcarResueltoClicked(object sender, EventArgs e)
    {
        var boton = (Button)sender;
        var reporte = boton.CommandParameter as Reporte;

        if (reporte == null)
            return;

        if (!string.Equals(reporte.EstadoReporte, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes marcar como resuelto un reporte activo.",
                "OK"
            );

            return;
        }

        bool respuesta = await DisplayAlertAsync(
            "Confirmar",
            "¿Confirmas que este reporte ya fue resuelto?",
            "Sí, resolver",
            "Cancelar"
        );

        if (!respuesta)
            return;

        try
        {
            bool exito = await _apiService.MarcarReporteComoResueltoAsync(reporte.Id);

            if (exito)
            {
                await DisplayAlertAsync("Éxito", "El reporte ha sido marcado como resuelto.", "OK");
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

    private async void OnSuspenderReporteClicked(object sender, EventArgs e)
    {
        var boton = (Button)sender;
        var reporte = boton.CommandParameter as Reporte;

        if (reporte == null)
            return;

        if (!string.Equals(reporte.EstadoReporte, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes suspender reportes activos.",
                "OK"
            );

            return;
        }

        bool respuesta = await DisplayAlertAsync(
            "Confirmar suspensión",
            "¿Deseas suspender este reporte? Ya no aparecerá en el radar comunitario.",
            "Sí, suspender",
            "Cancelar"
        );

        if (!respuesta)
            return;

        try
        {
            bool exito = await _apiService.SuspenderReporteAsync(reporte.Id);

            if (exito)
            {
                await DisplayAlertAsync("Éxito", "El reporte fue suspendido correctamente.", "OK");
                await CargarMisReportes();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo suspender el reporte.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo suspender el reporte: {ex.Message}", "OK");
        }
    }

    private async void OnReactivarReporteClicked(object sender, EventArgs e)
    {
        var boton = (Button)sender;
        var reporte = boton.CommandParameter as Reporte;

        if (reporte == null)
            return;

        if (!string.Equals(reporte.EstadoReporte, "Suspendido", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes reactivar reportes suspendidos.",
                "OK"
            );

            return;
        }

        bool respuesta = await DisplayAlertAsync(
            "Confirmar reactivación",
            "¿Deseas reactivar este reporte? Volverá a aparecer en el radar comunitario.",
            "Sí, reactivar",
            "Cancelar"
        );

        if (!respuesta)
            return;

        try
        {
            bool exito = await _apiService.ReactivarReporteAsync(reporte.Id);

            if (exito)
            {
                await DisplayAlertAsync("Éxito", "El reporte fue reactivado correctamente.", "OK");
                await CargarMisReportes();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo reactivar el reporte.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo reactivar el reporte: {ex.Message}", "OK");
        }
    }

    private async void OnAccionesReporteClicked(object sender, EventArgs e)
    {
        if (sender is not Button boton)
            return;

        if (boton.CommandParameter is not Reporte reporte)
            return;

        var opciones = new List<string>
    {
        "Ver detalle"
    };

        if (reporte.PuedeResolver)
            opciones.Add("Marcar como resuelto");

        if (reporte.PuedeSuspender)
            opciones.Add("Pausar reporte");

        if (reporte.PuedeReactivar)
            opciones.Add("Reactivar reporte");

        string accion = await DisplayActionSheetAsync(
            "Acciones del reporte",
            "Cancelar",
            null,
            opciones.ToArray()
        );

        if (string.IsNullOrWhiteSpace(accion) || accion == "Cancelar")
            return;

        switch (accion)
        {
            case "Ver detalle":
                await Navigation.PushAsync(new DetalleReportePage(_apiService, reporte));
                break;

            case "Marcar como resuelto":
                await ResolverReporteAsync(reporte);
                break;

            case "Pausar reporte":
                await SuspenderReporteAsync(reporte);
                break;

            case "Reactivar reporte":
                await ReactivarReporteAsync(reporte);
                break;
        }
    }

    private async Task ResolverReporteAsync(Reporte reporte)
    {
        if (!string.Equals(reporte.EstadoReporte, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes marcar como resuelto un reporte activo.",
                "OK"
            );

            return;
        }

        bool respuesta = await DisplayAlertAsync(
            "Confirmar",
            "¿Confirmas que este reporte ya fue resuelto?",
            "Sí, resolver",
            "Cancelar"
        );

        if (!respuesta)
            return;

        try
        {
            bool exito = await _apiService.MarcarReporteComoResueltoAsync(reporte.Id);

            if (exito)
            {
                await DisplayAlertAsync("Éxito", "El reporte ha sido marcado como resuelto.", "OK");
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

    private async Task SuspenderReporteAsync(Reporte reporte)
    {
        if (!string.Equals(reporte.EstadoReporte, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes suspender reportes activos.",
                "OK"
            );

            return;
        }

        bool respuesta = await DisplayAlertAsync(
            "Confirmar suspensión",
            "¿Deseas suspender este reporte? Ya no aparecerá en el radar comunitario.",
            "Sí, suspender",
            "Cancelar"
        );

        if (!respuesta)
            return;

        try
        {
            bool exito = await _apiService.SuspenderReporteAsync(reporte.Id);

            if (exito)
            {
                await DisplayAlertAsync("Éxito", "El reporte fue suspendido correctamente.", "OK");
                await CargarMisReportes();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo suspender el reporte.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo suspender el reporte: {ex.Message}", "OK");
        }
    }

    private async Task ReactivarReporteAsync(Reporte reporte)
    {
        if (!string.Equals(reporte.EstadoReporte, "Suspendido", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "No disponible",
                "Solo puedes reactivar reportes suspendidos.",
                "OK"
            );

            return;
        }

        bool respuesta = await DisplayAlertAsync(
            "Confirmar reactivación",
            "¿Deseas reactivar este reporte? Volverá a aparecer en el radar comunitario.",
            "Sí, reactivar",
            "Cancelar"
        );

        if (!respuesta)
            return;

        try
        {
            bool exito = await _apiService.ReactivarReporteAsync(reporte.Id);

            if (exito)
            {
                await DisplayAlertAsync("Éxito", "El reporte fue reactivado correctamente.", "OK");
                await CargarMisReportes();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo reactivar el reporte.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo reactivar el reporte: {ex.Message}", "OK");
        }
    }

}