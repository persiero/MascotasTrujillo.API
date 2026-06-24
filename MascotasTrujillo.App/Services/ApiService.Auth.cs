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
        
        public async Task<LoginResponse?> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new
                {
                    Email = email,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync("Auth/login", loginData);

                if (!response.IsSuccessStatusCode)
                    return null;

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(
                    jsonResponse,
                    _jsonOptions
                );

                return loginResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error de conexión: {ex.Message}");
                return null;
            }
        }

        
        public async Task<(bool Exito, string Mensaje)> RegistrarAsync(string nombreCompleto, string email, string password, string? telefono)
        {
            try
            {
                var registroData = new
                {
                    NombreCompleto = nombreCompleto,
                    Email = email,
                    Password = password,
                    Telefono = telefono
                };

                var response = await _httpClient.PostAsJsonAsync("Auth/registrar", registroData);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cuenta creada exitosamente");
                }
                else
                {
                    // ¡Atrapamos el mensaje real del backend!
                    string errorInfo = await response.Content.ReadAsStringAsync();
                    return (false, errorInfo);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }
    }
}
