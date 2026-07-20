using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;

namespace MascotasTrujillo.App.Views;

public partial class PerfilPage : ContentPage
{
    private readonly ApiService _apiService;
    private PerfilUsuario? _perfilActual;
    private string _rutaFotoPerfilLocal = string.Empty;

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
            FotoPerfilImage.Source = perfil.FotoMostrar;

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
        string telefono = LimpiarTelefono(TelefonoEntry.Text?.Trim() ?? string.Empty);

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlertAsync(
                "Dato requerido",
                "El nombre completo es obligatorio.",
                "OK"
            );

            return;
        }

        if (!string.IsNullOrWhiteSpace(telefono) && telefono.Length < 9)
        {
            await DisplayAlertAsync(
                "Teléfono inválido",
                "Ingresa un número de teléfono válido para WhatsApp.",
                "OK"
            );

            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        BtnGuardarPerfil.IsEnabled = false;
        BtnGuardarPerfil.Text = "Guardando cambios...";

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
            BtnGuardarPerfil.Text = "💾 Guardar cambios";
        }
    }

    private async void OnTomarFotoPerfilClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync(
                    "No disponible",
                    "La cámara no está soportada en este dispositivo.",
                    "OK"
                );

                return;
            }

            FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo == null)
                return;

            await ProcesarFotoPerfilAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo tomar la foto: {ex.Message}",
                "OK"
            );
        }
    }

    private async void OnSeleccionarFotoPerfilClicked(object sender, EventArgs e)
    {
        try
        {
            IEnumerable<FileResult> photos = await MediaPicker.Default.PickPhotosAsync();

            FileResult? photo = photos?.FirstOrDefault();

            if (photo == null)
                return;

            await ProcesarFotoPerfilAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo seleccionar la foto: {ex.Message}",
                "OK"
            );
        }
    }

    private async Task ProcesarFotoPerfilAsync(FileResult photo)
    {
        string extension = Path.GetExtension(photo.FileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        string nombreArchivo = $"perfil_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        string localFilePath = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);

        byte[] imageBytes;

        using (Stream sourceStream = await photo.OpenReadAsync())
        using (MemoryStream memoryStream = new MemoryStream())
        {
            await sourceStream.CopyToAsync(memoryStream);
            imageBytes = memoryStream.ToArray();
        }

        await File.WriteAllBytesAsync(localFilePath, imageBytes);

        _rutaFotoPerfilLocal = localFilePath;

        // Mostramos la imagen desde memoria, no desde el archivo físico.
        // Así evitamos que Android bloquee el archivo que luego vamos a subir.
        FotoPerfilImage.Source = ImageSource.FromStream(
            () => new MemoryStream(imageBytes)
        );

        await SubirFotoPerfilAsync();
    }

    private async Task SubirFotoPerfilAsync()
    {
        if (string.IsNullOrWhiteSpace(_rutaFotoPerfilLocal))
            return;

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var resultado = await _apiService.ActualizarFotoPerfilAsync(_rutaFotoPerfilLocal);

            if (resultado.Exito)
            {
                if (!string.IsNullOrWhiteSpace(resultado.FotoPerfilUrl))
                {
                    await SecureStorage.Default.SetAsync("usuario_foto", resultado.FotoPerfilUrl);
                    FotoPerfilImage.Source = resultado.FotoPerfilUrl;
                }

                await DisplayAlertAsync(
                    "Foto actualizada",
                    "Tu foto de perfil fue actualizada correctamente.",
                    "OK"
                );
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
                $"No se pudo actualizar la foto: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
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
        SecureStorage.Default.Remove("usuario_id");
        SecureStorage.Default.Remove("usuario_nombre");
        SecureStorage.Default.Remove("usuario_email");
        SecureStorage.Default.Remove("usuario_telefono");
        SecureStorage.Default.Remove("usuario_foto");

        _apiService.ClearToken();

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new LoginPage(_apiService);
        }
    }

    private async void OnCambiarPasswordTapped(object sender, TappedEventArgs e)
    {
        PasswordActualEntry.Text = string.Empty;
        PasswordNuevoEntry.Text = string.Empty;
        ConfirmarPasswordNuevoEntry.Text = string.Empty;

        PasswordOverlay.IsVisible = true;
        PasswordOverlay.Opacity = 0;

        await PasswordOverlay.FadeToAsync(1, 150);
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

    private async void OnCerrarPasswordClicked(object sender, EventArgs e)
    {
        await CerrarPasswordOverlayAsync();
    }

    private async Task CerrarPasswordOverlayAsync()
    {
        await PasswordOverlay.FadeToAsync(0, 120);
        PasswordOverlay.IsVisible = false;
        PasswordOverlay.Opacity = 0;
    }

    private async void OnGuardarPasswordClicked(object sender, EventArgs e)
    {
        string passwordActual = PasswordActualEntry.Text ?? string.Empty;
        string passwordNuevo = PasswordNuevoEntry.Text ?? string.Empty;
        string confirmarPassword = ConfirmarPasswordNuevoEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(passwordActual))
        {
            await DisplayAlertAsync("Dato requerido", "Ingresa tu contraseña actual.", "OK");
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
            BtnGuardarPassword.IsEnabled = false;
            BtnGuardarPassword.Text = "Actualizando...";

            var resultado = await _apiService.CambiarPasswordAsync(
                passwordActual,
                passwordNuevo,
                confirmarPassword
            );

            if (resultado.Exito)
            {
                await CerrarPasswordOverlayAsync();

                await DisplayAlertAsync(
                    "Contraseña actualizada",
                    "Tu contraseña fue actualizada correctamente.",
                    "OK"
                );
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
                $"No se pudo actualizar la contraseña: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            BtnGuardarPassword.IsEnabled = true;
            BtnGuardarPassword.Text = "Actualizar contraseña";
        }
    }

}