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
    }
}
