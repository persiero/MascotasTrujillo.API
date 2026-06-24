using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class PerfilPage : ContentPage
{
    private readonly ApiService _apiService;
    private PerfilUsuario? _perfilActual;

    public PerfilPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPerfilAsync();
    }

    private async Task CargarPerfilAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnGuardarPerfil.IsEnabled = false;

        try
        {
            var perfil = await _apiService.ObtenerPerfilAsync();

            if (perfil == null)
            {
                await DisplayAlertAsync(
                    "Sesión",
                    "No se pudo cargar tu perfil. Vuelve a iniciar sesión.",
                    "OK"
                );

                return;
            }

            _perfilActual = perfil;

            LblNombrePerfil.Text = perfil.NombreCompleto;
            LblEmailPerfil.Text = perfil.Email;

            NombreEntry.Text = perfil.NombreCompleto;
            EmailEntry.Text = perfil.Email;
            TelefonoEntry.Text = perfil.Telefono;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo cargar el perfil: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            BtnGuardarPerfil.IsEnabled = true;
        }
    }

    private async void OnGuardarPerfilClicked(object sender, EventArgs e)
    {
        string nombre = NombreEntry.Text?.Trim() ?? string.Empty;
        string telefono = TelefonoEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlertAsync(
                "Dato requerido",
                "El nombre completo es obligatorio.",
                "OK"
            );

            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnGuardarPerfil.IsEnabled = false;
        BtnGuardarPerfil.Text = "Guardando...";

        try
        {
            var resultado = await _apiService.ActualizarPerfilAsync(
                nombreCompleto: nombre,
                telefono: telefono
            );

            if (resultado.Exito)
            {
                await SecureStorage.Default.SetAsync("usuario_nombre", nombre);

                if (!string.IsNullOrWhiteSpace(telefono))
                    await SecureStorage.Default.SetAsync("usuario_telefono", telefono);
                else
                    SecureStorage.Default.Remove("usuario_telefono");

                await DisplayAlertAsync(
                    "Perfil actualizado",
                    "Tus datos fueron actualizados correctamente.",
                    "OK"
                );

                await CargarPerfilAsync();
            }
            else
            {
                await DisplayAlertAsync(
                    "No se pudo actualizar",
                    resultado.Mensaje,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo actualizar el perfil: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            BtnGuardarPerfil.IsEnabled = true;
            BtnGuardarPerfil.Text = "Guardar cambios";
        }
    }

    private async void OnCerrarSesionTapped(object sender, TappedEventArgs e)
    {
        bool salir = await DisplayAlertAsync(
            "Cerrar sesión",
            "¿Estás seguro de que deseas salir de tu cuenta?",
            "Sí, salir",
            "Cancelar"
        );

        if (!salir)
            return;

        SecureStorage.Default.Remove("auth_token");
        SecureStorage.Default.Remove("usuario_nombre");
        SecureStorage.Default.Remove("usuario_email");
        SecureStorage.Default.Remove("usuario_telefono");

        _apiService.ClearToken();

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new LoginPage(_apiService);
        }
    }
}