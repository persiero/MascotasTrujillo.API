using MascotasTrujillo.App.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
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


    }
}
