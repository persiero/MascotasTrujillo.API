using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class PerfilPage : ContentPage
{
    private readonly ApiService _apiService;

    public PerfilPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    // Acción para el botón de "Editar Datos"
    private async void OnEditarDatosTapped(object sender, TappedEventArgs e)
    {
        // Por ahora mostramos un mensaje. Más adelante aquí abriremos la pantalla del CRUD.
        await DisplayAlertAsync("Módulo en construcción", "Aquí abriremos el formulario para actualizar tus datos.", "OK");
    }

    // Acción para el botón de "Cerrar Sesión"
    private async void OnCerrarSesionTapped(object sender, TappedEventArgs e)
    {
        // 1. Preguntamos al usuario para evitar toques accidentales
        bool salir = await DisplayAlertAsync("Cerrar Sesión", "¿Estás seguro de que deseas salir de tu cuenta?", "Sí, Salir", "Cancelar");

        if (salir)
        {
            // 2. Eliminamos el token de la bóveda segura del celular
            SecureStorage.Default.Remove("auth_token");

            // 3. Limpiamos el token de la sesión activa en nuestro servicio
            _apiService.SetToken(string.Empty);

            // 4. Destruimos el menú (AppShell) y regresamos a la pantalla de Login usando la sintaxis moderna
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new LoginPage(_apiService);
            }
        }
    }
}