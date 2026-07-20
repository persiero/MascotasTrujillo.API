using MascotasTrujillo.App.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace MascotasTrujillo.App.Services
{
    public partial class ApiService
    {
        public async Task<PerfilUsuario?> ObtenerPerfilAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("Usuarios/perfil");

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<PerfilUsuario>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener perfil: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarPerfilAsync(
            string nombreCompleto,
            string? telefono)
        {
            try
            {
                var data = new
                {
                    NombreCompleto = nombreCompleto,
                    Telefono = telefono
                };

                var response = await _httpClient.PutAsJsonAsync("Usuarios/perfil", data);

                if (response.IsSuccessStatusCode)
                    return (true, "Perfil actualizado correctamente.");

                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje, string? FotoPerfilUrl)> ActualizarFotoPerfilAsync(string rutaFotoLocal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rutaFotoLocal) || !File.Exists(rutaFotoLocal))
                {
                    return (false, "No se encontró la imagen seleccionada.", null);
                }

                using var content = new MultipartFormDataContent();

                byte[] fileBytes = await File.ReadAllBytesAsync(rutaFotoLocal);

                var fileContent = new ByteArrayContent(fileBytes);

                string extension = Path.GetExtension(rutaFotoLocal).ToLower();

                string contentType = extension switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    _ => "image/jpeg"
                };

                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

                content.Add(fileContent, "Foto", Path.GetFileName(rutaFotoLocal));

                var response = await _httpClient.PutAsync("Usuarios/perfil/foto", content);

                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(json);

                    string? fotoUrl = null;

                    if (document.RootElement.TryGetProperty("fotoPerfilUrl", out var fotoElement))
                    {
                        fotoUrl = fotoElement.GetString();
                    }
                    else if (document.RootElement.TryGetProperty("FotoPerfilUrl", out var fotoElementMayus))
                    {
                        fotoUrl = fotoElementMayus.GetString();
                    }

                    return (true, "Foto de perfil actualizada correctamente.", fotoUrl);
                }

                return (false, json, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}", null);
            }
        }

        public async Task<(bool Exito, string Mensaje)> CambiarPasswordAsync(
            string passwordActual,
            string passwordNuevo,
            string confirmarPasswordNuevo)
        {
            try
            {
                var data = new
                {
                    PasswordActual = passwordActual,
                    PasswordNuevo = passwordNuevo,
                    ConfirmarPasswordNuevo = confirmarPasswordNuevo
                };

                var response = await _httpClient.PutAsJsonAsync(
                    "Usuarios/cambiar-password",
                    data
                );

                if (response.IsSuccessStatusCode)
                    return (true, "Contraseña actualizada correctamente.");

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
