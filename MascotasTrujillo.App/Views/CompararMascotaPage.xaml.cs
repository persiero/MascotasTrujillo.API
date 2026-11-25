using MascotasTrujillo.App.Models;
using MascotasTrujillo.App.Services;
using Microsoft.Maui.Graphics;

namespace MascotasTrujillo.App.Views;

public partial class CompararMascotaPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Reporte _reporte;

    private FileResult? _fotoEncontrada;
    private string? _rutaFotoTemporal;

    private bool _procesando = false;

    private CancellationTokenSource? _analisisCts;

    private readonly IndicadorSimilitudDrawable _indicador;

    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public CompararMascotaPage(
        ApiService apiService,
        Reporte reporte)
    {
        InitializeComponent();

        _apiService = apiService;
        _reporte = reporte;

        _indicador = new IndicadorSimilitudDrawable();

        GraficoSimilitud.Drawable = _indicador;

        CargarDatosReporte();
    }

    // =====================================================
    // CARGAR INFORMACION DEL REPORTE
    // =====================================================

    private void CargarDatosReporte()
    {
        LblTituloReporte.Text = _reporte.Titulo;

        LblNombreMascota.Text =
            string.IsNullOrWhiteSpace(
                _reporte.NombreMascotaReferencial)
            ? "Mascota reportada"
            : _reporte.NombreMascotaReferencial;

        if (!string.IsNullOrWhiteSpace(_reporte.FotoUrl))
        {
            FotoOriginalImage.Source = _reporte.FotoUrl;
        }

        ActualizarEstadoBotones();
    }

    // =====================================================
    // TOMAR FOTO DESDE CAMARA
    // =====================================================

    private async void OnTomarFotoClicked(
        object sender,
        EventArgs e)
    {
        if (_procesando)
            return;

        CambiarEstadoProcesando(
            true,
            "Preparando fotografía..."
        );

        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync(
                    "Cámara no disponible",
                    "Este dispositivo no permite capturar fotografías.",
                    "OK"
                );

                return;
            }

            var foto = await MediaPicker.Default
                .CapturePhotoAsync();

            await ProcesarFotoAsync(foto);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo abrir la cámara: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            CambiarEstadoProcesando(false);
        }
    }

    // =====================================================
    // SELECCIONAR FOTO DESDE GALERIA
    // =====================================================

    private async void OnElegirGaleriaClicked(
        object sender,
        EventArgs e)
    {
        if (_procesando)
            return;

        CambiarEstadoProcesando(
            true,
            "Preparando fotografía..."
        );

        try
        {
            var fotos = await MediaPicker.Default
                .PickPhotosAsync();

            var foto = fotos?.FirstOrDefault();

            await ProcesarFotoAsync(foto);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo acceder a la galería: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            CambiarEstadoProcesando(false);
        }
    }

    // =====================================================
    // COMPRIMIR Y PREPARAR FOTOGRAFIA
    // =====================================================

    private async Task ProcesarFotoAsync(FileResult? foto)
    {
        if (foto == null)
            return;

        var resultado = await Task.Run(() =>
            ImageCompressionService.ComprimirFileResultAsync(
                foto,
                prefijoArchivo: "comparacion_ia",
                maxMb: 5,
                tipoRecorte: TipoRecorteImagen.SinRecorte
            )
        );

        EliminarFotoTemporal();

        _rutaFotoTemporal = resultado.RutaLocal;

        _fotoEncontrada = new FileResult(
            resultado.RutaLocal,
            "image/jpeg"
        );

        FotoEncontradaImage.Source =
            ImageSource.FromStream(
                () => new MemoryStream(resultado.Bytes)
            );

        FotoEncontradaImage.IsVisible = true;
        PlaceholderFoto.IsVisible = false;

        LblEstadoFoto.Text = "Imagen preparada";

        LblPesoFoto.Text =
            $"{resultado.PesoFinalMb:0.00} MB";

        PanelResultado.IsVisible = false;
    }

    // =====================================================
    // EJECUTAR ANALISIS DE INTELIGENCIA ARTIFICIAL
    // =====================================================

    private async void OnAnalizarClicked(
        object sender,
        EventArgs e)
    {
        if (_procesando)
            return;

        if (_fotoEncontrada == null)
        {
            await DisplayAlertAsync(
                "Fotografía requerida",
                "Selecciona una fotografía para realizar el análisis.",
                "OK"
            );

            return;
        }

        PanelResultado.IsVisible = false;

        CambiarEstadoProcesando(
            true,
            "Analizando las características visuales..."
        );

        BtnAnalizar.Text = "Analizando fotografías...";

        using var cts = new CancellationTokenSource();

        _analisisCts = cts;

        ActualizarEstadoBotones();

        try
        {
            var respuesta =
                await _apiService.CompararMascotaAsync(
                    _reporte.Id,
                    _fotoEncontrada,
                    cts.Token
                );

            if (cts.IsCancellationRequested)
                return;

            if (respuesta.Exito &&
                respuesta.Resultado != null)
            {
                MostrarResultado(respuesta.Resultado);
            }
            else
            {
                await DisplayAlertAsync(
                    "Análisis no disponible",
                    respuesta.Mensaje,
                    "OK"
                );
            }
        }
        catch (OperationCanceledException)
            when (cts.IsCancellationRequested)
        {
            // El usuario canceló el análisis al salir.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Error",
                "No se pudo completar el análisis.",
                "OK"
            );
        }
        finally
        {
            _analisisCts = null;

            BtnAnalizar.Text = "🔍 Analizar similitud";

            CambiarEstadoProcesando(false);
        }
    }

    // =====================================================
    // MOSTRAR RESULTADO
    // =====================================================

    private void MostrarResultado(ResultadoSimilitud resultado)
    {
        double porcentaje = Math.Clamp(
            resultado.PorcentajeSimilitud,
            0,
            100
        );

        Color colorPrincipal;
        Color colorFondo;

        string nivel = resultado.NivelSimilitud.Trim();

        if (nivel.Equals(
                "Alta",
                StringComparison.OrdinalIgnoreCase))
        {
            colorPrincipal = Color.FromArgb("#0F766E");
            colorFondo = Color.FromArgb("#DCFCE7");
        }
        else if (nivel.Equals(
                     "Media",
                     StringComparison.OrdinalIgnoreCase))
        {
            colorPrincipal = Color.FromArgb("#B45309");
            colorFondo = Color.FromArgb("#FEF3C7");
        }
        else if (nivel.Equals(
                     "Baja",
                     StringComparison.OrdinalIgnoreCase))
        {
            colorPrincipal = Color.FromArgb("#B91C1C");
            colorFondo = Color.FromArgb("#FEE2E2");
        }
        else
        {
            colorPrincipal = Color.FromArgb("#5B21E6");
            colorFondo = Color.FromArgb("#EEE7FF");
        }

        LblNivelSimilitud.Text = resultado.Titulo;
        LblNivelSimilitud.TextColor = colorPrincipal;

        BadgeResultado.BackgroundColor = colorFondo;

        LblDescripcionResultado.Text =
            resultado.Descripcion;

        LblModelo.Text = resultado.Modelo;
        LblMetodo.Text = resultado.Metodo;

        LblAdvertencia.Text =
            "El resultado representa una similitud visual " +
            "estimada. No confirma ni descarta la identidad " +
            "de la mascota. Los umbrales son provisionales.";

        // Actualizar la fotografia original utilizada por la API.
        if (!string.IsNullOrWhiteSpace(
                resultado.FotoOriginalUrl))
        {
            FotoOriginalImage.Source =
                resultado.FotoOriginalUrl;
        }

        // Actualizar indicador circular.
        _indicador.Porcentaje = porcentaje;
        _indicador.ColorPrincipal = colorPrincipal;
        _indicador.ColorFondo = colorFondo;

        PanelResultado.IsVisible = true;

        GraficoSimilitud.Invalidate();
    }

    // =====================================================
    // NUEVA COMPARACION
    // =====================================================

    private void OnNuevaComparacionClicked(
        object sender,
        EventArgs e)
    {
        if (_procesando)
            return;

        _fotoEncontrada = null;

        FotoEncontradaImage.Source = null;
        FotoEncontradaImage.IsVisible = false;

        EliminarFotoTemporal();

        PlaceholderFoto.IsVisible = true;

        LblEstadoFoto.Text = "Sin fotografía";
        LblPesoFoto.Text = "Cámara o galería";

        PanelResultado.IsVisible = false;

        _indicador.Porcentaje = 0;

        GraficoSimilitud.Invalidate();

        ActualizarEstadoBotones();
    }

    // =====================================================
    // ESTADO DE PROCESAMIENTO
    // =====================================================

    private void CambiarEstadoProcesando(
        bool procesando,
        string mensaje = "")
    {
        _procesando = procesando;

        PanelProcesando.IsVisible = procesando;

        LoadingIndicator.IsRunning = procesando;

        if (!string.IsNullOrWhiteSpace(mensaje))
        {
            LblProcesando.Text = mensaje;
        }

        ActualizarEstadoBotones();
    }

    private void ActualizarEstadoBotones()
    {
        BtnCamara.IsEnabled = !_procesando;
        BtnGaleria.IsEnabled = !_procesando;

        BtnNuevaComparacion.IsEnabled = !_procesando;

        BtnAnalizar.IsEnabled =
            !_procesando &&
            _fotoEncontrada != null &&
            !string.IsNullOrWhiteSpace(_reporte.FotoUrl);

        BtnAnalizar.Opacity =
            BtnAnalizar.IsEnabled ? 1 : 0.50;

        // Durante la selección de imágenes se evita salir.
        // Durante el análisis sí se permite cancelarlo.
        BtnVolver.IsEnabled =
            !_procesando || _analisisCts != null;
    }

    // =====================================================
    // ELIMINAR ARCHIVO TEMPORAL
    // =====================================================

    private void EliminarFotoTemporal()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_rutaFotoTemporal) &&
                File.Exists(_rutaFotoTemporal))
            {
                File.Delete(_rutaFotoTemporal);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        _rutaFotoTemporal = null;
    }

    // =====================================================
    // VOLVER A LA PANTALLA ANTERIOR
    // =====================================================

    private bool _saliendo = false;

    private async void OnVolverClicked(
        object sender,
        EventArgs e)
    {
        await VolverAsync();
    }

    private async Task VolverAsync()
    {
        if (_saliendo)
            return;

        if (_procesando && _analisisCts == null)
            return;

        _saliendo = true;

        try
        {
            if (_analisisCts != null)
            {
                bool salir = await DisplayAlertAsync(
                    "Cancelar análisis",
                    "Hay un análisis en proceso. ¿Deseas salir?",
                    "Sí, salir",
                    "Continuar"
                );

                if (!salir)
                    return;

                _analisisCts?.Cancel();
            }

            await Navigation.PopAsync();
        }
        finally
        {
            _saliendo = false;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await VolverAsync();
        });

        return true;
    }

    protected override void OnNavigatedFrom(
        NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        _analisisCts?.Cancel();

        EliminarFotoTemporal();
    }
}


// =========================================================
// INDICADOR CIRCULAR DE SIMILITUD
// =========================================================

public class IndicadorSimilitudDrawable : IDrawable
{
    public double Porcentaje { get; set; }

    public Color ColorPrincipal { get; set; } =
        Color.FromArgb("#5B21E6");

    public Color ColorFondo { get; set; } =
        Color.FromArgb("#EEE7FF");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float centroX = dirtyRect.Center.X;
        float centroY = dirtyRect.Center.Y;

        float radio =
            Math.Min(
                dirtyRect.Width,
                dirtyRect.Height
            ) / 2 - 13;

        float diametro = radio * 2;

        float x = centroX - radio;
        float y = centroY - radio;

        float grosor = 12;

        // Círculo de fondo.

        canvas.StrokeSize = grosor;
        canvas.StrokeColor = ColorFondo;

        canvas.DrawCircle(
            centroX,
            centroY,
            radio
        );

        // Arco correspondiente al porcentaje.

        double valor = Math.Clamp(
            Porcentaje,
            0,
            100
        );

        canvas.StrokeColor = ColorPrincipal;
        canvas.StrokeLineCap = LineCap.Round;

        if (valor >= 100)
        {
            canvas.DrawCircle(
                centroX,
                centroY,
                radio
            );
        }
        else if (valor > 0)
        {
            float anguloFinal =
                90f - (float)(valor * 3.6);

            canvas.DrawArc(
                x,
                y,
                diametro,
                diametro,
                90,
                anguloFinal,
                true,
                false
            );
        }

        // Porcentaje central.

        canvas.FontColor = ColorPrincipal;

        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;

        canvas.FontSize = 31;

        canvas.DrawString(
            $"{valor:0.00}%",
            x,
            centroY - 27,
            diametro,
            40,
            HorizontalAlignment.Center,
            VerticalAlignment.Center
        );

        // Texto inferior.

        canvas.FontColor = Color.FromArgb("#64748B");

        canvas.Font =
            Microsoft.Maui.Graphics.Font.Default;

        canvas.FontSize = 12;

        canvas.DrawString(
            "Similitud visual",
            x,
            centroY + 14,
            diametro,
            24,
            HorizontalAlignment.Center,
            VerticalAlignment.Center
        );
    }
}