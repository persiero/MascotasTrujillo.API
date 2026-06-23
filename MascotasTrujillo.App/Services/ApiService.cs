using MascotasTrujillo.App.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MascotasTrujillo.App.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string? _token;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService()
        {
            // EL TRUCO MULTIPLATAFORMA:
            // Si corremos en Android, apuntamos a la IP del puente del emulador.
            // Si corremos en Windows, usamos el localhost normal.
            // OJO: Asegúrate de que el puerto "7013" coincida con el HTTPS de tu API.
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7013/api/"
                : "https://localhost:7013/api/";

            // NUEVO: Le decimos a Android que confíe en nuestro certificado local (HTTPS)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Solo permitimos esto porque estamos desarrollando en nuestra propia PC
                return true;
            };

            // Le pasamos el handler a nuestro cliente
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };

            // AGREGAR ESTA CONFIGURACIÓN:
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Método para guardar el token después de hacer Login
        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        // --- NUEVO MÉTODO DE LOGIN ---
        public async Task<string?> LoginAsync(string email, string password)
        {
            try
            {
                // 1. Armamos el paquete de datos tal como lo pide tu DTO en la API
                var loginData = new { Email = email, Password = password };

                // 2. Tocamos la puerta del endpoint Auth/login
                var response = await _httpClient.PostAsJsonAsync("Auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    // 3. Si entramos, leemos la respuesta
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // Extraemos el Token (Limpiamos las comillas en caso sea un string puro)
                    using var document = JsonDocument.Parse(jsonResponse);
                    if (document.RootElement.TryGetProperty("token", out var tokenElement))
                    {
                        return tokenElement.GetString();
                    }

                    return jsonResponse.Trim('"'); // Por si tu API devuelve solo el texto
                }

                return null; // Credenciales incorrectas
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error de conexión: {ex.Message}");
                return null;
            }
        }

        // --- NUEVO MÉTODO DE REGISTRO MEJORADO ---
        public async Task<(bool Exito, string Mensaje)> RegistrarAsync(string nombreCompleto, string email, string password, string? telefono)
        {
            try
            {
                var registroData = new
                {
                    NombreCompleto = nombreCompleto,
                    Email = email,
                    Password = password,
                    Telefono = telefono
                };

                var response = await _httpClient.PostAsJsonAsync("Auth/registrar", registroData);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cuenta creada exitosamente");
                }
                else
                {
                    // ¡Atrapamos el mensaje real del backend!
                    string errorInfo = await response.Content.ReadAsStringAsync();
                    return (false, errorInfo);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> CrearReporteAsync(
            long? mascotaId,
            short tipoReporteId,
            string titulo,
            string descripcion,
            double latitud,
            double longitud,
            string? direccionReferencia,
            FileResult? foto = null,
            string? nombreMascotaReferencial = null,
            string? especieReferencial = null,
            string? razaReferencial = null,
            string? colorReferencial = null,
            string? sexoReferencial = null)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                if (mascotaId.HasValue)
                    content.Add(new StringContent(mascotaId.Value.ToString()), "MascotaId");

                content.Add(new StringContent(tipoReporteId.ToString()), "TipoReporteId");
                content.Add(new StringContent(titulo), "Titulo");
                content.Add(new StringContent(descripcion), "Descripcion");
                content.Add(new StringContent(latitud.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitud");
                content.Add(new StringContent(longitud.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitud");

                if (!string.IsNullOrWhiteSpace(direccionReferencia))
                    content.Add(new StringContent(direccionReferencia), "DireccionReferencia");

                if (!string.IsNullOrWhiteSpace(nombreMascotaReferencial))
                    content.Add(new StringContent(nombreMascotaReferencial), "NombreMascotaReferencial");

                if (!string.IsNullOrWhiteSpace(especieReferencial))
                    content.Add(new StringContent(especieReferencial), "EspecieReferencial");

                if (!string.IsNullOrWhiteSpace(razaReferencial))
                    content.Add(new StringContent(razaReferencial), "RazaReferencial");

                if (!string.IsNullOrWhiteSpace(colorReferencial))
                    content.Add(new StringContent(colorReferencial), "ColorReferencial");

                if (!string.IsNullOrWhiteSpace(sexoReferencial))
                    content.Add(new StringContent(sexoReferencial), "SexoReferencial");

                if (foto != null)
                {
                    var stream = await foto.OpenReadAsync();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(foto.ContentType ?? "image/jpeg");
                    content.Add(fileContent, "Foto", foto.FileName);
                }

                var response = await _httpClient.PostAsync("Reportes", content);

                if (response.IsSuccessStatusCode)
                    return (true, "Reporte registrado exitosamente.");

                var errorInfo = await response.Content.ReadAsStringAsync();
                return (false, $"Error del servidor: {errorInfo}");
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        // Agrega esto debajo de tu método LoginAsync
        public async Task<List<Reporte>> ObtenerReportesCercanosAsync(double latitud, double longitud, double radioMetros = 3000)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"Reportes/cercanos?latitud={latitud}&longitud={longitud}&radioMetros={radioMetros}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var lista = await response.Content.ReadFromJsonAsync<List<Reporte>>(_jsonOptions);
                    return lista ?? new List<Reporte>();
                }

                return new List<Reporte>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en el radar: {ex.Message}");
                return new List<Reporte>();
            }
        }

        // NUEVO MÉTODO: Enviar foto y datos a la API
        public async Task<(bool Exito, string Mensaje)> RegistrarAvistamientoAsync(
            long reporteId,
            string? descripcion,
            double latitud,
            double longitud,
            string? direccionReferencia,
            FileResult? foto = null)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(reporteId.ToString()), "ReporteId");

                if (!string.IsNullOrWhiteSpace(descripcion))
                    content.Add(new StringContent(descripcion), "Descripcion");

                content.Add(new StringContent(latitud.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitud");
                content.Add(new StringContent(longitud.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitud");

                if (!string.IsNullOrWhiteSpace(direccionReferencia))
                    content.Add(new StringContent(direccionReferencia), "DireccionReferencia");

                if (foto != null)
                {
                    var stream = await foto.OpenReadAsync();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(foto.ContentType ?? "image/jpeg");
                    content.Add(fileContent, "Foto", foto.FileName);
                }

                var response = await _httpClient.PostAsync("Avistamientos", content);

                if (response.IsSuccessStatusCode)
                    return (true, "Avistamiento registrado exitosamente.");

                var errorInfo = await response.Content.ReadAsStringAsync();
                return (false, $"Error del servidor: {errorInfo}");
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<List<Avistamiento>?> ObtenerAvistamientosPorReporteAsync(long reporteId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Avistamientos/reporte/{reporteId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Avistamiento>>(json, _jsonOptions);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener avistamientos: {ex.Message}");
                return null;
            }
        }

        // Añade estos métodos dentro de tu clase ApiService

        public async Task<List<Reporte>?> GetMisReportesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("Reportes/mis-reportes");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Reporte>>(json, _jsonOptions);
                }

                string errorInfo = await response.Content.ReadAsStringAsync();
                throw new Exception($"El servidor rechazó la petición. Código: {response.StatusCode}. Detalle: {errorInfo}");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> MarcarReporteComoResueltoAsync(long reporteId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"Reportes/{reporteId}/resolver", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // =======================================================
        // NUEVO: Obtener la lista de mascotas del usuario actual
        // =======================================================
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

        // =======================================================
        // NUEVO: Registrar una mascota con foto (Multipart/Form-Data)
        // =======================================================
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

                content.Add(new StringContent(nombre), "Nombre");
                content.Add(new StringContent(especie), "Especie");

                if (!string.IsNullOrWhiteSpace(raza))
                    content.Add(new StringContent(raza), "Raza");

                if (!string.IsNullOrWhiteSpace(color))
                    content.Add(new StringContent(color), "ColorPrincipal");

                if (!string.IsNullOrWhiteSpace(sexo))
                    content.Add(new StringContent(sexo), "Sexo");

                if (!string.IsNullOrWhiteSpace(edadAproximada))
                    content.Add(new StringContent(edadAproximada), "EdadAproximada");

                if (!string.IsNullOrWhiteSpace(rasgos))
                    content.Add(new StringContent(rasgos), "RasgosParticulares");

                if (!string.IsNullOrWhiteSpace(enfermedades))
                    content.Add(new StringContent(enfermedades), "Enfermedades");

                if (!string.IsNullOrWhiteSpace(discapacidades))
                    content.Add(new StringContent(discapacidades), "Discapacidades");

                if (!string.IsNullOrWhiteSpace(tratamientos))
                    content.Add(new StringContent(tratamientos), "Tratamientos");

                if (!string.IsNullOrWhiteSpace(necesidadesEspeciales))
                    content.Add(new StringContent(necesidadesEspeciales), "NecesidadesEspeciales");

                if (!string.IsNullOrWhiteSpace(observacionesSalud))
                    content.Add(new StringContent(observacionesSalud), "ObservacionesSalud");

                if (!string.IsNullOrWhiteSpace(dispositivoId))
                    content.Add(new StringContent(dispositivoId), "DispositivoId");

                if (!string.IsNullOrWhiteSpace(rutaFotoLocal) && File.Exists(rutaFotoLocal))
                {
                    var fileStream = File.OpenRead(rutaFotoLocal);
                    var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                    content.Add(streamContent, "Foto", Path.GetFileName(rutaFotoLocal));
                }

                var response = await _httpClient.PostAsync("Mascotas", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Mascota registrada exitosamente.");
                }

                string errorInfo = await response.Content.ReadAsStringAsync();
                return (false, $"Error del servidor: {errorInfo}");
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<bool> SuspenderReporteAsync(long reporteId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"Reportes/{reporteId}/suspender", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReactivarReporteAsync(long reporteId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"Reportes/{reporteId}/reactivar", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
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

                content.Add(new StringContent(nombre), "Nombre");
                content.Add(new StringContent(especie), "Especie");

                if (!string.IsNullOrWhiteSpace(raza))
                    content.Add(new StringContent(raza), "Raza");

                if (!string.IsNullOrWhiteSpace(color))
                    content.Add(new StringContent(color), "ColorPrincipal");

                if (!string.IsNullOrWhiteSpace(sexo))
                    content.Add(new StringContent(sexo), "Sexo");

                if (!string.IsNullOrWhiteSpace(edadAproximada))
                    content.Add(new StringContent(edadAproximada), "EdadAproximada");

                if (!string.IsNullOrWhiteSpace(rasgos))
                    content.Add(new StringContent(rasgos), "RasgosParticulares");

                if (!string.IsNullOrWhiteSpace(enfermedades))
                    content.Add(new StringContent(enfermedades), "Enfermedades");

                if (!string.IsNullOrWhiteSpace(discapacidades))
                    content.Add(new StringContent(discapacidades), "Discapacidades");

                if (!string.IsNullOrWhiteSpace(tratamientos))
                    content.Add(new StringContent(tratamientos), "Tratamientos");

                if (!string.IsNullOrWhiteSpace(necesidadesEspeciales))
                    content.Add(new StringContent(necesidadesEspeciales), "NecesidadesEspeciales");

                if (!string.IsNullOrWhiteSpace(observacionesSalud))
                    content.Add(new StringContent(observacionesSalud), "ObservacionesSalud");

                if (!string.IsNullOrWhiteSpace(dispositivoId))
                    content.Add(new StringContent(dispositivoId), "DispositivoId");

                if (!string.IsNullOrWhiteSpace(rutaFotoLocal) && File.Exists(rutaFotoLocal))
                {
                    var fileStream = File.OpenRead(rutaFotoLocal);
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                    content.Add(fileContent, "Foto", Path.GetFileName(rutaFotoLocal));
                }

                var response = await _httpClient.PutAsync($"Mascotas/{mascotaId}", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Mascota actualizada correctamente.");
                }

                var errorInfo = await response.Content.ReadAsStringAsync();
                return (false, errorInfo);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
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

    }
}
