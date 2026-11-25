using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MascotasTrujillo.API.DTOs;

namespace MascotasTrujillo.API.Services
{
    public class InteligenciaArtificialService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private const int MaxBytes = 5 * 1024 * 1024;

        public InteligenciaArtificialService(
            HttpClient httpClient,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // =====================================================
        // VALIDAR URL DE CLOUDFLARE R2
        // =====================================================

        private Uri ValidarUrlFoto(string fotoUrl)
        {
            string? dominioPublico =
                _configuration["CloudflareR2:PublicDomain"];

            if (!Uri.TryCreate(
                    dominioPublico,
                    UriKind.Absolute,
                    out var dominio))
            {
                throw new InvalidOperationException(
                    "El dominio de Cloudflare R2 no está configurado."
                );
            }

            if (!Uri.TryCreate(
                    fotoUrl,
                    UriKind.Absolute,
                    out var url))
            {
                throw new InvalidDataException(
                    "La URL de la fotografía original no es válida."
                );
            }

            if (url.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(
                    url.Host,
                    dominio.Host,
                    StringComparison.OrdinalIgnoreCase) ||
                url.Port != dominio.Port ||
                !string.IsNullOrEmpty(url.UserInfo))
            {
                throw new InvalidDataException(
                    "La fotografía no pertenece al almacenamiento autorizado."
                );
            }

            return url;
        }

        // =====================================================
        // DESCARGAR FOTO ORIGINAL
        // =====================================================

        private async Task<byte[]> DescargarFotoOriginalAsync(
            string fotoUrl,
            CancellationToken cancellationToken)
        {
            var url = ValidarUrlFoto(fotoUrl);

            var cliente = _httpClientFactory.CreateClient(
                "FotosMascotas"
            );

            using var response = await cliente.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    "No se pudo recuperar la fotografía original."
                );
            }

            if (response.Content.Headers.ContentLength > MaxBytes)
            {
                throw new InvalidDataException(
                    "La fotografía original supera los 5 MB."
                );
            }

            using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken);

            using var memoria = new MemoryStream();

            byte[] buffer = new byte[8192];
            int bytesLeidos;

            while ((bytesLeidos = await stream.ReadAsync(
                buffer.AsMemory(),
                cancellationToken)) > 0)
            {
                if (memoria.Length + bytesLeidos > MaxBytes)
                {
                    throw new InvalidDataException(
                        "La fotografía original supera los 5 MB."
                    );
                }

                await memoria.WriteAsync(
                    buffer.AsMemory(0, bytesLeidos),
                    cancellationToken
                );
            }

            if (memoria.Length == 0)
            {
                throw new InvalidDataException(
                    "La fotografía original está vacía."
                );
            }

            return memoria.ToArray();
        }

        // =====================================================
        // COMPARAR FOTOGRAFÍAS
        // =====================================================

        public async Task<ResultadoSimilitudDTO> CompararAsync(
            string fotoOriginalUrl,
            IFormFile fotoEncontrada,
            CancellationToken cancellationToken = default)
        {
            using var timeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

            timeout.CancelAfter(TimeSpan.FromSeconds(90));

            var token = timeout.Token;

            // 1. Descargar fotografía original desde R2
            byte[] bytesOriginal =
                await DescargarFotoOriginalAsync(
                    fotoOriginalUrl,
                    token
                );

            // 2. Leer fotografía encontrada
            using var memoriaEncontrada = new MemoryStream();

            await fotoEncontrada.CopyToAsync(
                memoriaEncontrada,
                token
            );

            byte[] bytesEncontrada =
                memoriaEncontrada.ToArray();

            // 3. Preparar petición multipart/form-data
            using var contenido = new MultipartFormDataContent();

            var fotoOriginalContent =
                new ByteArrayContent(bytesOriginal);

            fotoOriginalContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "application/octet-stream"
                );

            contenido.Add(
                fotoOriginalContent,
                "foto_original",
                "original"
            );

            var fotoEncontradaContent =
                new ByteArrayContent(bytesEncontrada);

            fotoEncontradaContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "application/octet-stream"
                );

            contenido.Add(
                fotoEncontradaContent,
                "foto_encontrada",
                "encontrada"
            );

            // 4. Enviar las fotografías a FastAPI
            using var respuesta = await _httpClient.PostAsync(
                "api/comparar",
                contenido,
                token
            );

            // 5. Validar respuesta
            if (!respuesta.IsSuccessStatusCode)
            {
                if (respuesta.StatusCode ==
                        HttpStatusCode.BadRequest ||
                    respuesta.StatusCode ==
                        HttpStatusCode.UnsupportedMediaType ||
                    respuesta.StatusCode ==
                        HttpStatusCode.RequestEntityTooLarge)
                {
                    throw new InvalidDataException(
                        "Una de las fotografías no tiene un formato, " +
                        "tamaño o resolución compatible."
                    );
                }

                throw new HttpRequestException(
                    "El servicio de inteligencia artificial " +
                    "no pudo completar el análisis.",
                    null,
                    respuesta.StatusCode
                );
            }

            // 6. Convertir JSON a DTO
            var resultado = await respuesta.Content
                .ReadFromJsonAsync<ResultadoSimilitudDTO>(
                    cancellationToken: token
                );

            if (resultado == null ||
                !resultado.Exito ||
                !double.IsFinite(resultado.PorcentajeSimilitud) ||
                resultado.PorcentajeSimilitud < 0 ||
                resultado.PorcentajeSimilitud > 100)
            {
                throw new InvalidOperationException(
                    "El servicio de IA devolvió un resultado inválido."
                );
            }

            return resultado;
        }
    }
}