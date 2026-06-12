using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RegistroPage : ContentPage
{
    private readonly ApiService _apiService;

    public RegistroPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        // 1. Validaciones básicas locales
        if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            // CORREGIDO: DisplayAlertAsync
            await DisplayAlertAsync("Atención", "El nombre, correo y contraseña son obligatorios.", "OK");
            return;
        }

        if (PasswordEntry.Text.Length < 6)
        {
            // CORREGIDO: DisplayAlertAsync
            await DisplayAlertAsync("Atención", "La contraseña debe tener al menos 6 caracteres.", "OK");
            return;
        }

        // 2. Mostramos carga
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        // 3. Llamamos a la API (Ahora devuelve dos datos)
        var resultado = await _apiService.RegistrarAsync(
            NombreEntry.Text.Trim(),
            EmailEntry.Text.Trim(),
            PasswordEntry.Text,
            TelefonoEntry.Text?.Trim()
        );

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        // 4. Resultado detallado
        if (resultado.Exito)
        {
            await DisplayAlertAsync("¡Bienvenido!", "Tu cuenta ha sido creada exitosamente. Ahora puedes iniciar sesión.", "OK");
            await Navigation.PopModalAsync();
        }
        else
        {
            // AHORA SÍ: Mostramos exactamente por qué la API nos rechazó
            await DisplayAlertAsync("Error del Servidor", $"Detalle: {resultado.Mensaje}", "OK");
        }
    }

    private async void OnVolverAlLoginTapped(object sender, EventArgs e)
    {
        // Cierra esta pantalla y vuelve a la anterior (el Login)
        await Navigation.PopModalAsync();
    }
}