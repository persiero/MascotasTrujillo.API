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

    // NUEVO MÉTODO: Se ejecuta automáticamente cuando la pantalla se dibuja en el celular
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Verificamos si existe una sesión previa guardada encriptada
            string? tokenGuardado = await SecureStorage.Default.GetAsync("auth_token");

            if (!string.IsNullOrEmpty(tokenGuardado))
            {
                // Si existe, le colocamos la credencial al servicio web
                _apiService.SetToken(tokenGuardado);

                // Sintaxis moderna y segura para .NET 8/9 (Soporte Multi-ventana)
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
            }
        }
        catch (Exception ex) // <-- Le agregamos "ex" para capturar el error
        {
            // Registramos el error internamente para depuración.
            // La app simplemente se quedará en el Login de forma segura.
            Console.WriteLine($"Aviso - Fallo al leer SecureStorage: {ex.Message}");
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // Mostramos el círculo de carga (UX profesional)
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        // ¡Llamamos a nuestra API!
        var loginResponse = await _apiService.LoginAsync(email, password);

        // Ocultamos el círculo de carga
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (loginResponse != null && !string.IsNullOrWhiteSpace(loginResponse.Token))
        {
            _apiService.SetToken(loginResponse.Token);
            await SecureStorage.Default.SetAsync("auth_token", loginResponse.Token);

            if (!string.IsNullOrWhiteSpace(loginResponse.NombreCompleto))
                await SecureStorage.Default.SetAsync("usuario_nombre", loginResponse.NombreCompleto);

            if (!string.IsNullOrWhiteSpace(loginResponse.Email))
                await SecureStorage.Default.SetAsync("usuario_email", loginResponse.Email);

            if (!string.IsNullOrWhiteSpace(loginResponse.Telefono))
                await SecureStorage.Default.SetAsync("usuario_telefono", loginResponse.Telefono);

            // Sintaxis moderna y segura para .NET 8/9 (Soporte Multi-ventana)
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            await DisplayAlertAsync("Error de ingreso", "No se pudo conectar. Revisa tus credenciales o el estado de la API.", "OK");
        }
    }

    private async void OnOlvidastePasswordTapped(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Recuperación", "Próximamente: Pantalla de recuperación de contraseña.", "OK");
    }

    private async void OnRegistrarseTapped(object? sender, EventArgs e)
    {
        // Abrimos la pantalla de registro de forma modal (animación de abajo hacia arriba)
        await Navigation.PushModalAsync(new RegistroPage(_apiService));
    }
}