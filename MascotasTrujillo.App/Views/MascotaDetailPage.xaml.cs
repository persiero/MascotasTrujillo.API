using MascotasTrujillo.App.Models;

namespace MascotasTrujillo.App.Views;

public partial class MascotaDetailPage : ContentPage
{
    private readonly Avistamiento _mascotaActual;

    public MascotaDetailPage(Avistamiento mascota)
	{
		InitializeComponent();
        _mascotaActual = mascota;

        // Llenamos la pantalla con los datos recibidos
        FotoGrande.Source = mascota.FotoUrl;
        LblDescripcion.Text = mascota.Descripcion;
        LblDistancia.Text = $"Se encuentra a {mascota.DistanciaMetros:N0} metros de tu posición.";
    }

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        try
        {
            // OJO: Aquí deberías poner tu número real (con código de país ej: 51) para probarlo
            string numeroTelefono = "51915391298";

            // Armamos un mensaje predefinido súper amable
            string mensaje = $"¡Hola! Vi el reporte de: '{_mascotaActual.Descripcion}' en el radar de Mascotas Trujillo. ¿Me das más info?";

            // Convertimos el mensaje a formato URL (cambia los espacios por %20, etc.)
            string mensajeCodificado = Uri.EscapeDataString(mensaje);

            // Creamos el enlace oficial de la API de WhatsApp
            string url = $"https://wa.me/{numeroTelefono}?text={mensajeCodificado}";

            // ¡Launcher es una función nativa de MAUI que abre la app correspondiente en el celular!
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir WhatsApp. ¿Está instalado en este dispositivo?", "OK");
        }
    }
}