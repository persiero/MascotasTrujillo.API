using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class RecuperarPasswordPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _procesando = false;
    private string _emailRecuperacion = string.Empty;

    public RecuperarPasswordPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnEnviarCodigoClicked(object sender, EventArgs e)
    {
        if (_procesando)
            return;

        string email = EmailRecuperacionEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlertAsync(
                "Dato requerido",
                "Ingresa tu correo electrónico.",
                "OK"
            );

            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            await DisplayAlertAsync(
                "Correo inválido",
                "Ingresa un correo electrónico válido.",
                "OK"
            );

            return;
        }

        try
        {
            _procesando = true;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            BtnEnviarCodigo.IsEnabled = false;
            BtnEnviarCodigo.Text = "Enviando código...";

            var resultado = await _apiService.ForgotPasswordAsync(email);

            if (resultado.Exito)
            {
                _emailRecuperacion = email;

                LblCorreoDestino.Text =
                    $"Enviamos un código de recuperación a: {email}";

                PasoCorreoContainer.IsVisible = false;
                PasoCodigoContainer.IsVisible = true;

                CodigoEntry.Text = string.Empty;
                PasswordNuevoEntry.Text = string.Empty;
                ConfirmarPasswordEntry.Text = string.Empty;

                await DisplayAlertAsync(
                    "Código enviado",
                    "Revisa tu correo e ingresa el código recibido.",
                    "OK"
                );

                CodigoEntry.Focus();
            }
            else
            {
                await DisplayAlertAsync(
                    "No se pudo enviar",
                    resultado.Mensaje,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo enviar el código: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            _procesando = false;

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            BtnEnviarCodigo.IsEnabled = true;
            BtnEnviarCodigo.Text = "Enviar código";
        }
    }

    private async void OnRestablecerClicked(object sender, EventArgs e)
    {
        if (_procesando)
            return;

        string codigo = CodigoEntry.Text?.Trim() ?? string.Empty;
        string passwordNuevo = PasswordNuevoEntry.Text ?? string.Empty;
        string confirmarPassword = ConfirmarPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_emailRecuperacion))
        {
            await DisplayAlertAsync(
                "Correo requerido",
                "Primero debes solicitar un código de recuperación.",
                "OK"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
        {
            await DisplayAlertAsync(
                "Código inválido",
                "Ingresa el código de 6 dígitos que llegó a tu correo.",
                "OK"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(passwordNuevo) || passwordNuevo.Length < 6)
        {
            await DisplayAlertAsync(
                "Contraseña inválida",
                "La nueva contraseña debe tener al menos 6 caracteres.",
                "OK"
            );

            return;
        }

        if (passwordNuevo != confirmarPassword)
        {
            await DisplayAlertAsync(
                "No coincide",
                "La nueva contraseña y la confirmación no coinciden.",
                "OK"
            );

            return;
        }

        try
        {
            _procesando = true;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            BtnRestablecer.IsEnabled = false;
            BtnRestablecer.Text = "Restableciendo...";

            var resultado = await _apiService.ResetPasswordAsync(
                _emailRecuperacion,
                codigo,
                passwordNuevo,
                confirmarPassword
            );

            if (resultado.Exito)
            {
                await DisplayAlertAsync(
                    "Contraseña restablecida",
                    "Tu contraseña fue actualizada correctamente. Ahora puedes iniciar sesión.",
                    "OK"
                );

                await Navigation.PopModalAsync();
            }
            else
            {
                await DisplayAlertAsync(
                    "No se pudo restablecer",
                    resultado.Mensaje,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo restablecer la contraseña: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            _procesando = false;

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            BtnRestablecer.IsEnabled = true;
            BtnRestablecer.Text = "Restablecer contraseña";
        }
    }

    private void OnCambiarCorreoClicked(object sender, EventArgs e)
    {
        PasoCodigoContainer.IsVisible = false;
        PasoCorreoContainer.IsVisible = true;

        CodigoEntry.Text = string.Empty;
        PasswordNuevoEntry.Text = string.Empty;
        ConfirmarPasswordEntry.Text = string.Empty;

        _emailRecuperacion = string.Empty;

        EmailRecuperacionEntry.Focus();
    }
}