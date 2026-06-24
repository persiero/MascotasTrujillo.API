using MascotasTrujillo.App.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
        private readonly HttpClient _httpClient;
        private string? _token = string.Empty;
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
                       

        public void ClearToken()
        {
            _token = string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
              

    }
}
