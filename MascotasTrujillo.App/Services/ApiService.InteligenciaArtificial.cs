using MascotasTrujillo.App.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
        public async Task<(
            bool Exito,
            string Mensaje,
            ResultadoSimilitud? Resultado)>
            CompararMascotaAsync(
                long reporteId,
                FileResult fotoEncontrada,
                CancellationToken cancellationToken = default)
        {
            try
            {
                if (reporteId <= 0)
                {
                    return (
                        false,
                        "El identificador del reporte no es válido.",
                        null
                    );
                }

                if (fotoEncontrada == null)
                {
                    return (
                        false,
                        "Debes seleccionar una fotografía.",
                        null
                    );
                }

                using var contenido =
                    new MultipartFormDataContent();

                using var stream =
                    await fotoEncontrada.OpenReadAsync();

                var fotoContent = new StreamContent(stream);

                fotoContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        fotoEncontrada.ContentType ?? "image/jpeg"
                    );

                contenido.Add(
                    fotoContent,
                    "FotoEncontrada",
                    fotoEncontrada.FileName
                );

                using var respuesta =
                    await _httpClient.PostAsync(
                        $"InteligenciaArtificial/comparar-reporte/{reporteId}",
                        contenido,
                        cancellationToken
                    );

                string json = await respuesta.Content
                    .ReadAsStringAsync(cancellationToken);

                if (respuesta.IsSuccessStatusCode)
                {
                    var resultado =
                        JsonSerializer.Deserialize<ResultadoSimilitud>(
                            json,
                            _jsonOptions
                        );

                    if (resultado != null &&
                        resultado.Exito &&
                        double.IsFinite(resultado.PorcentajeSimilitud) &&
                        resultado.PorcentajeSimilitud >= 0 &&
                        resultado.PorcentajeSimilitud <= 100)
                    {
                        return (
                            true,
                            "Análisis realizado correctamente.",
                            resultado
                        );
                    }

                    return (
                        false,
                        "No se recibió un resultado válido.",
                        null
                    );
                }

                if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return (
                        false,
                        "Tu sesión no es válida. Inicia sesión nuevamente.",
                        null
                    );
                }

                string mensaje =
                    "No se pudo realizar el análisis.";

                try
                {
                    using var documento = JsonDocument.Parse(json);

                    foreach (var propiedad in documento.RootElement
                                 .EnumerateObject())
                    {
                        if (propiedad.Name.Equals(
                                "mensaje",
                                StringComparison.OrdinalIgnoreCase) &&
                            propiedad.Value.ValueKind ==
                                JsonValueKind.String)
                        {
                            mensaje = propiedad.Value.GetString()
                                      ?? mensaje;

                            break;
                        }
                    }
                }
                catch
                {
                    // Mantener el mensaje general.
                }

                return (false, mensaje, null);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return (
                    false,
                    "El análisis tardó demasiado. Inténtalo nuevamente.",
                    null
                );
            }
            catch (HttpRequestException)
            {
                return (
                    false,
                    "No se pudo conectar con el servidor. Verifica que las API estén ejecutándose.",
                    null
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                return (
                    false,
                    "Ocurrió un problema al analizar las fotografías.",
                    null
                );
            }
        }
    }
}