using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace MascotasTrujillo.App.Services
{
    internal class ApiService
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

            // Nota: Al probar localmente con HTTPS en Android, a veces el emulador 
            // rechaza el certificado de desarrollo. Si nos da error, lo ajustaremos.

            _httpClient = new HttpClient
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

        // --- Aquí iremos agregando nuestras llamadas a la API ---
        // public async Task<bool> LoginAsync(string email, string password) { ... }
        // public async Task<List<Avistamiento>> ObtenerCercanosAsync(double lat, double lon) { ... }
    }
}
