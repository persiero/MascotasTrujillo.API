using MascotasTrujillo.App.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
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
    }
}
