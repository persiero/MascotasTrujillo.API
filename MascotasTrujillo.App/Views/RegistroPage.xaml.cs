using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RegistroPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _procesandoRegistro = false;

    public RegistroPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        if (_procesandoRegistro)
            return;

        string nombre = NombreEntry.Text?.Trim() ?? string.Empty;
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string telefono = LimpiarTelefono(TelefonoEntry.Text?.Trim() ?? string.Empty);
        string password = PasswordEntry.Text ?? string.Empty;
        string confirmarPassword = ConfirmarPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlertAsync("Dato requerido", "Ingresa tu nombre completo.", "OK");
            return;
        }

        if (nombre.Length < 5)
        {
            await DisplayAlertAsync("Nombre inválido", "Ingresa tu nombre y apellido.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlertAsync("Dato requerido", "Ingresa tu correo electrónico.", "OK");
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            await DisplayAlertAsync("Correo inválido", "Ingresa un correo electrónico válido.", "OK");
            return;
        }

        if (!string.IsNullOrWhiteSpace(telefono) && telefono.Length < 9)
        {
            await DisplayAlertAsync(
                "Teléfono inválido",
                "Ingresa un número válido. Para Perú puedes escribir 9 dígitos o 51 + número.",
                "OK"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Dato requerido", "Ingresa una contraseña.", "OK");
            return;
        }

        if (password.Length < 6)
        {
            await DisplayAlertAsync(
                "Contraseña inválida",
                "La contraseña debe tener al menos 6 caracteres.",
                "OK"
            );

            return;
        }

        if (password != confirmarPassword)
        {
            await DisplayAlertAsync(
                "No coincide",
                "La contraseña y la confirmación no coinciden.",
                "OK"
            );

            return;
        }

        if (!AceptaTerminosCheckBox.IsChecked)
        {
            await DisplayAlertAsync(
                "Confirmación requerida",
                "Debes aceptar el uso de tus datos de contacto para funciones de rescate.",
                "OK"
            );

            return;
        }

        try
        {
            _procesandoRegistro = true;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            BtnRegistrar.IsEnabled = false;
            BtnRegistrar.Text = "Creando cuenta...";

            var resultado = await _apiService.RegistrarAsync(
                nombreCompleto: nombre,
                email: email,
                password: password,
                confirmarPassword: confirmarPassword,
                telefono: telefono
            );

            if (resultado.Exito)
            {
                await DisplayAlertAsync(
                    "Cuenta creada",
                    "Tu cuenta fue creada correctamente. Ahora puedes iniciar sesión.",
                    "OK"
                );

                await Navigation.PopModalAsync();
            }
            else
            {
                await DisplayAlertAsync(
                    "No se pudo registrar",
                    resultado.Mensaje,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo crear la cuenta: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            _procesandoRegistro = false;

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            BtnRegistrar.IsEnabled = true;
            BtnRegistrar.Text = "Crear cuenta";
        }
    }

    private async void OnVolverAlLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        BtnTogglePassword.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
    }

    private void OnToggleConfirmarPasswordClicked(object sender, EventArgs e)
    {
        ConfirmarPasswordEntry.IsPassword = !ConfirmarPasswordEntry.IsPassword;
        BtnToggleConfirmarPassword.Text = ConfirmarPasswordEntry.IsPassword ? "👁" : "🙈";
    }

    private string LimpiarTelefono(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return string.Empty;

        string numeroLimpio = new string(
            telefono.Where(char.IsDigit).ToArray()
        );

        if (numeroLimpio.Length == 9)
            numeroLimpio = "51" + numeroLimpio;

        return numeroLimpio;
    }
}