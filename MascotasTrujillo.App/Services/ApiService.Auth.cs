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


        public async Task<(bool Exito, string Mensaje)> RegistrarAsync(
            string nombreCompleto,
            string email,
            string password,
            string confirmarPassword,
            string? telefono)
        {
            try
            {
                var registroData = new
                {
                    NombreCompleto = nombreCompleto,
                    Email = email,
                    Password = password,
                    ConfirmarPassword = confirmarPassword,
                    Telefono = telefono
                };

                var response = await _httpClient.PostAsJsonAsync("Auth/registrar", registroData);

                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cuenta creada exitosamente.");
                }

                string mensaje = ExtraerMensajeApi(json, json);

                return (false, mensaje);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ForgotPasswordAsync(string email)
        {
            try
            {
                var data = new
                {
                    Email = email
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "Auth/forgot-password",
                    data
                );

                var json = await response.Content.ReadAsStringAsync();
                string mensaje = ExtraerMensajeApi(json, "Solicitud procesada.");

                if (response.IsSuccessStatusCode)
                    return (true, mensaje);

                return (false, mensaje);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ResetPasswordAsync(
            string email,
            string codigo,
            string passwordNuevo,
            string confirmarPasswordNuevo)
        {
            try
            {
                var data = new
                {
                    Email = email,
                    Codigo = codigo,
                    PasswordNuevo = passwordNuevo,
                    ConfirmarPasswordNuevo = confirmarPasswordNuevo
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "Auth/reset-password",
                    data
                );

                var json = await response.Content.ReadAsStringAsync();
                string mensaje = ExtraerMensajeApi(json, "Solicitud procesada.");

                if (response.IsSuccessStatusCode)
                    return (true, mensaje);

                return (false, mensaje);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        private static string ExtraerMensajeApi(string json, string mensajeDefecto)
        {
            if (string.IsNullOrWhiteSpace(json))
                return mensajeDefecto;

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("mensaje", out var mensaje))
                        return mensaje.GetString() ?? mensajeDefecto;

                    if (root.TryGetProperty("Mensaje", out var mensajeMayus))
                        return mensajeMayus.GetString() ?? mensajeDefecto;
                }

                return json;
            }
            catch
            {
                return json;
            }
        }
    }
}
