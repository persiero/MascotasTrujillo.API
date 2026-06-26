using System;
using System.Collections.Generic;
using System.Text;
using MascotasTrujillo.App.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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
                    content.Add(new StringContent(dispositivoId?.Trim() ?? string.Empty), "DispositivoId");

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

    }
}
