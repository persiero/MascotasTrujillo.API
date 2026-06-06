using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    // Inyectamos el servicio de la API
    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // Mostramos el círculo de carga (UX profesional)
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
            _apiService.SetToken(token); // Guardamos el token JWT de forma segura

            // CAMBIO CLAVE: Reemplazamos la ventana por AppShell para activar el TabBar inferior
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            // CORRECCIÓN: Usamos la advertencia técnica correspondiente
            await DisplayAlertAsync("Error de ingreso", "No se pudo conectar. Revisa tus credenciales o el estado de la API.", "OK");
        }
    }

    private async void OnOlvidastePasswordTapped(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Recuperación", "Próximamente: Pantalla de recuperación de contraseña.", "OK");
    }

    private async void OnRegistrarseTapped(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Registro", "Próximamente: Pantalla de creación de cuenta.", "OK");
    }
}