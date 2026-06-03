using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    // Inyectamos nuestro servicio mágico
    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // Mostramos el círculo de carga
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        // ¡Llamamos a nuestra API!
        string? token = await _apiService.LoginAsync(email, password);

        // Ocultamos el círculo de carga
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (!string.IsNullOrEmpty(token))
        {
            _apiService.SetToken(token); // Guardamos la pulsera en el servicio

            // Le decimos a la aplicación que cambie la pantalla principal por el Radar
            Application.Current?.Windows[0].Page = new NavigationPage(new RadarPage(_apiService));
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo conectar. Revisa la consola o credenciales.", "OK");
        }
    }

    private async void OnOlvidastePasswordTapped(object? sender, EventArgs e)
    {
        // Por ahora mostraremos una alerta, luego crearemos la pantalla
        await DisplayAlertAsync("Recuperación", "Próximamente: Pantalla de recuperación de contraseña.", "OK");
    }

    private async void OnRegistrarseTapped(object? sender, EventArgs e)
    {
        // Por ahora mostraremos una alerta, luego crearemos la pantalla
        await DisplayAlertAsync("Registro", "Próximamente: Pantalla de creación de cuenta.", "OK");
    }
}