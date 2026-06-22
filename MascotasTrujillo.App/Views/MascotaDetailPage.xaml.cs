using MascotasTrujillo.App.Models;

namespace MascotasTrujillo.App.Views;

public partial class MascotaDetailPage : ContentPage
{
    private readonly Reporte _reporteActual;

    public MascotaDetailPage(Reporte reporte)
    {
        InitializeComponent();
        _reporteActual = reporte;

        // Foto del reporte
        if (!string.IsNullOrWhiteSpace(reporte.FotoUrl))
        {
            FotoGrande.Source = reporte.FotoUrl;
        }

        // Descripción principal
        LblDescripcion.Text = reporte.Descripcion;

        // Distancia
        if (reporte.DistanciaMetros > 0)
        {
            LblDistancia.Text = $"Se encuentra a {reporte.DistanciaMetros:N0} metros de tu posición.";
        }
        else
        {
            LblDistancia.Text = "Ubicación registrada en el reporte.";
        }

        // Opcional: si tienes algún Label para título, puedes usarlo.
        // LblTitulo.Text = reporte.Titulo;
    }

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        try
        {
            // Número temporal para pruebas.
            // Más adelante debería venir desde la API como teléfono del creador del reporte.
            string numeroTelefono = "51915391298";

            string mensaje = $"¡Hola! Vi el reporte '{_reporteActual.Titulo}' en Mascotas Trujillo. ¿Me puedes brindar más información?";

            string mensajeCodificado = Uri.EscapeDataString(mensaje);

            string url = $"https://wa.me/{numeroTelefono}?text={mensajeCodificado}";

            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir WhatsApp. ¿Está instalado en este dispositivo?", "OK");
        }
    }
}