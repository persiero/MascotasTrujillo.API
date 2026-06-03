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

        // Agrega esto debajo de tu método LoginAsync
        public async Task<List<Models.Avistamiento>> ObtenerCercanosAsync(double latitud, double longitud, double radioMetros = 3000)
        {
            try
            {
                // Llamamos a tu endpoint de PostGIS
                var response = await _httpClient.GetAsync($"Avistamientos/cercanos?latitud={latitud}&longitud={longitud}&radioMetros={radioMetros}");

                if (response.IsSuccessStatusCode)
                {
                    // Convertimos el JSON en nuestra lista de C#
                    var lista = await response.Content.ReadFromJsonAsync<List<Models.Avistamiento>>();
                    return lista ?? new List<Models.Avistamiento>();
                }
                return new List<Models.Avistamiento>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en el radar: {ex.Message}");
                return new List<Models.Avistamiento>();
            }
        }

        // NUEVO MÉTODO: Enviar foto y datos a la API
        public async Task<bool> ReportarAvistamientoAsync(FileResult foto, string descripcion, double latitud, double longitud)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                // 1. Empaquetamos los textos
                content.Add(new StringContent(descripcion ?? ""), "Descripcion");
                content.Add(new StringContent(latitud.ToString()), "Latitud");
                content.Add(new StringContent(longitud.ToString()), "Longitud");

                // 2. Empaquetamos el archivo (La foto)
                var stream = await foto.OpenReadAsync();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(foto.ContentType);

                // OJO: "Foto" debe coincidir exactamente con el nombre de la propiedad en tu DTO de la API
                content.Add(fileContent, "Foto", foto.FileName);

                // 3. ¡Enviamos el paquete!
                var response = await _httpClient.PostAsync("Avistamientos", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al reportar: {ex.Message}");
                return false;
            }
        }

    }
}
