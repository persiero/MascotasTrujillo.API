using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Maui.Devices;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
        private readonly HttpClient _httpClient;
        private string? _token = string.Empty;
        private readonly JsonSerializerOptions _jsonOptions;

        // ============================================================
        // CAMBIA ESTO SEGÚN LO QUE QUIERAS PROBAR
        // ============================================================
        private static readonly bool UsarProduccionRailway = true;

        // API en Railway
        private const string ApiProduccion = "https://mascotastrujilloapi-production.up.railway.app/api/";

        // API local en Windows
        private const string ApiLocalWindows = "https://localhost:7013/api/";

        // API local desde emulador Android
        private const string ApiLocalAndroidEmulador = "https://10.0.2.2:7013/api/";

        // API local desde celular físico conectado a la misma red WiFi
        // Cambia la IP por la IP real de tu PC.
        private const string ApiLocalAndroidFisico = "http://192.168.1.50:5139/api/";

        public ApiService()
        {
            string baseUrl = ObtenerBaseUrl();

            var handler = new HttpClientHandler();

            // Solo ignoramos certificados cuando estamos en LOCAL con HTTPS.
            // En Railway NO se debe ignorar certificados.
            if (EsApiLocalHttps(baseUrl))
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    return true;
                };
            }

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static string ObtenerBaseUrl()
        {
            if (UsarProduccionRailway)
                return ApiProduccion;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Para emulador Android usa 10.0.2.2
                return ApiLocalAndroidEmulador;

                // Para celular físico, comenta la línea anterior y usa esta:
                // return ApiLocalAndroidFisico;
            }

            return ApiLocalWindows;
        }

        private static bool EsApiLocalHttps(string baseUrl)
        {
            return baseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) ||
                   baseUrl.StartsWith("https://10.0.2.2", StringComparison.OrdinalIgnoreCase);
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }

        public void ClearToken()
        {
            _token = string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
