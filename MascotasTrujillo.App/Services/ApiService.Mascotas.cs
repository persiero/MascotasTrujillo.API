using System;
using System.Collections.Generic;
using System.Text;
using MascotasTrujillo.App.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
        public async Task<List<Models.Mascota>?> GetMisMascotasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("Mascotas/mis-mascotas");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Models.Mascota>>(json, _jsonOptions);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener mascotas: {ex.Message}");
                return null;
            }
        }


        public async Task<(bool Exito, string Mensaje)> RegistrarMascotaAsync(
    string nombre,
    string especie,
    string raza,
    string color,
    string sexo,
    string edadAproximada,
    string rasgos,
    string? enfermedades,
    string? discapacidades,
    string? tratamientos,
    string? necesidadesEspeciales,
    string? observacionesSalud,
    string dispositivoId,
    string rutaFotoLocal)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                AgregarCampoTexto(content, "Nombre", nombre);
                AgregarCampoTexto(content, "Especie", especie);
                AgregarCampoTexto(content, "Raza", raza);
                AgregarCampoTexto(content, "ColorPrincipal", color);
                AgregarCampoTexto(content, "Sexo", sexo);
                AgregarCampoTexto(content, "EdadAproximada", edadAproximada);
                AgregarCampoTexto(content, "RasgosParticulares", rasgos);

                AgregarCampoTexto(content, "Enfermedades", enfermedades);
                AgregarCampoTexto(content, "Discapacidades", discapacidades);
                AgregarCampoTexto(content, "Tratamientos", tratamientos);
                AgregarCampoTexto(content, "NecesidadesEspeciales", necesidadesEspeciales);
                AgregarCampoTexto(content, "ObservacionesSalud", observacionesSalud);

                AgregarCampoTexto(content, "DispositivoId", dispositivoId);

                await AgregarFotoAsync(content, rutaFotoLocal);

                var response = await _httpClient.PostAsync("Mascotas", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Mascota registrada exitosamente.");
                }

                string mensajeError = await LeerMensajeErrorApiAsync(
                    response,
                    "No se pudo registrar la mascota."
                );

                return (false, mensajeError);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexión con el servidor.\n\nDetalle técnico:\n{ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "El servidor tardó demasiado en responder. Verifica tu conexión a Internet e inténtalo nuevamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al preparar o enviar los datos de la mascota.\n\nDetalle técnico:\n{ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarMascotaAsync(
    long mascotaId,
    string nombre,
    string especie,
    string? raza,
    string? color,
    string? sexo,
    string? edadAproximada,
    string? rasgos,
    string? enfermedades,
    string? discapacidades,
    string? tratamientos,
    string? necesidadesEspeciales,
    string? observacionesSalud,
    string? dispositivoId,
    string? rutaFotoLocal)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                AgregarCampoTexto(content, "Nombre", nombre);
                AgregarCampoTexto(content, "Especie", especie);
                AgregarCampoTexto(content, "Raza", raza);
                AgregarCampoTexto(content, "ColorPrincipal", color);
                AgregarCampoTexto(content, "Sexo", sexo);
                AgregarCampoTexto(content, "EdadAproximada", edadAproximada);
                AgregarCampoTexto(content, "RasgosParticulares", rasgos);

                AgregarCampoTexto(content, "Enfermedades", enfermedades);
                AgregarCampoTexto(content, "Discapacidades", discapacidades);
                AgregarCampoTexto(content, "Tratamientos", tratamientos);
                AgregarCampoTexto(content, "NecesidadesEspeciales", necesidadesEspeciales);
                AgregarCampoTexto(content, "ObservacionesSalud", observacionesSalud);

                AgregarCampoTexto(content, "DispositivoId", dispositivoId);

                await AgregarFotoAsync(content, rutaFotoLocal);

                var response = await _httpClient.PutAsync($"Mascotas/{mascotaId}", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Mascota actualizada correctamente.");
                }

                string mensajeError = await LeerMensajeErrorApiAsync(
                    response,
                    "No se pudo actualizar la mascota."
                );

                return (false, mensajeError);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexión con el servidor.\n\nDetalle técnico:\n{ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "El servidor tardó demasiado en responder. Verifica tu conexión a Internet e inténtalo nuevamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al preparar o enviar los datos de la mascota.\n\nDetalle técnico:\n{ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> DesactivarMascotaAsync(long mascotaId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"Mascotas/{mascotaId}/desactivar", null);

                if (response.IsSuccessStatusCode)
                    return (true, "Mascota desactivada correctamente.");

                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ReactivarMascotaAsync(long mascotaId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"Mascotas/{mascotaId}/reactivar", null);

                if (response.IsSuccessStatusCode)
                    return (true, "Mascota reactivada correctamente.");

                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<List<UbicacionGpsHistorial>?> ObtenerHistorialGpsMascotaAsync(long mascotaId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Mascotas/{mascotaId}/historial-gps");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<UbicacionGpsHistorial>>(json, _jsonOptions);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener historial GPS: {ex.Message}");
                return null;
            }
        }

        private static void AgregarCampoTexto(MultipartFormDataContent content, string nombreCampo, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                content.Add(
                    new StringContent(valor.Trim(), Encoding.UTF8),
                    nombreCampo
                );
            }
        }

        private static string ObtenerContentTypeImagen(string rutaFotoLocal)
        {
            string extension = Path.GetExtension(rutaFotoLocal).ToLowerInvariant();

            return extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                ".heif" => "image/heif",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "image/jpeg"
            };
        }

        private static async Task AgregarFotoAsync(MultipartFormDataContent content, string? rutaFotoLocal)
        {
            if (string.IsNullOrWhiteSpace(rutaFotoLocal))
                return;

            if (!File.Exists(rutaFotoLocal))
                return;

            byte[] bytesFoto = await File.ReadAllBytesAsync(rutaFotoLocal);

            if (bytesFoto.Length == 0)
                return;

            const int maxMb = 5;
            int maxBytes = maxMb * 1024 * 1024;

            if (bytesFoto.Length > maxBytes)
            {
                double pesoMb = bytesFoto.Length / 1024.0 / 1024.0;

                throw new Exception(
                    $"La imagen pesa {pesoMb:0.00} MB y el máximo permitido es {maxMb} MB. " +
                    "Selecciona una imagen más ligera o toma una foto con menor resolución."
                );
            }

            var fileContent = new ByteArrayContent(bytesFoto);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(ObtenerContentTypeImagen(rutaFotoLocal));

            string nombreArchivo = Path.GetFileName(rutaFotoLocal);

            content.Add(fileContent, "Foto", nombreArchivo);
        }

        private async Task<string> LeerMensajeErrorApiAsync(HttpResponseMessage response, string mensajePorDefecto)
        {
            string contenido = await response.Content.ReadAsStringAsync();

            string codigoHttp = $"{(int)response.StatusCode} - {response.ReasonPhrase}";

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return $"{mensajePorDefecto}\n\nCódigo HTTP: {codigoHttp}";
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(contenido);
                JsonElement root = document.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString() ?? mensajePorDefecto;
                }

                if (root.TryGetProperty("mensaje", out JsonElement mensajeElement) &&
                    mensajeElement.ValueKind == JsonValueKind.String)
                {
                    return mensajeElement.GetString() ?? mensajePorDefecto;
                }

                if (root.TryGetProperty("Mensaje", out JsonElement mensajeMayusElement) &&
                    mensajeMayusElement.ValueKind == JsonValueKind.String)
                {
                    return mensajeMayusElement.GetString() ?? mensajePorDefecto;
                }

                if (root.TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString() ?? mensajePorDefecto;
                }

                if (root.TryGetProperty("title", out JsonElement titleElement) &&
                    titleElement.ValueKind == JsonValueKind.String)
                {
                    string titulo = titleElement.GetString() ?? mensajePorDefecto;

                    if (root.TryGetProperty("detail", out JsonElement detailElement) &&
                        detailElement.ValueKind == JsonValueKind.String)
                    {
                        return $"{titulo}\n\n{detailElement.GetString()}";
                    }

                    return $"{titulo}\n\nCódigo HTTP: {codigoHttp}";
                }

                if (root.TryGetProperty("errors", out JsonElement errorsElement) &&
                    errorsElement.ValueKind == JsonValueKind.Object)
                {
                    List<string> errores = new();

                    foreach (JsonProperty error in errorsElement.EnumerateObject())
                    {
                        if (error.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement detalle in error.Value.EnumerateArray())
                            {
                                string? texto = detalle.GetString();

                                if (!string.IsNullOrWhiteSpace(texto))
                                    errores.Add($"• {texto}");
                            }
                        }
                    }

                    if (errores.Count > 0)
                        return string.Join("\n", errores);
                }

                return $"{mensajePorDefecto}\n\nCódigo HTTP: {codigoHttp}\n\nRespuesta del servidor:\n{contenido}";
            }
            catch
            {
                return $"{mensajePorDefecto}\n\nCódigo HTTP: {codigoHttp}\n\nRespuesta del servidor:\n{contenido}";
            }
        }

    }
}
