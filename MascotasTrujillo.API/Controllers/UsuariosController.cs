using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using MascotasTrujillo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace MascotasTrujillo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly R2StorageService _storageService;

        public UsuariosController(
            UserManager<Usuario> userManager,
            R2StorageService storageService)
        {
            _userManager = userManager;
            _storageService = storageService;
        }

        [HttpGet("perfil")]
        public async Task<IActionResult> ObtenerPerfil()
        {
            var usuario = await ObtenerUsuarioActualAsync();

            if (usuario == null)
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario autenticado." });
            }

            if (!usuario.EstaActivo)
            {
                return Unauthorized(new { Mensaje = "La cuenta se encuentra desactivada." });
            }

            return Ok(new PerfilUsuarioDTO
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email ?? string.Empty,
                Telefono = usuario.PhoneNumber,
                FotoPerfilUrl = usuario.FotoPerfilUrl,
                FechaRegistro = usuario.FechaRegistro
            });
        }

        [HttpPut("perfil")]
        public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilDTO dto)
        {
            var usuario = await ObtenerUsuarioActualAsync();

            if (usuario == null)
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario autenticado." });
            }

            if (!usuario.EstaActivo)
            {
                return Unauthorized(new { Mensaje = "La cuenta se encuentra desactivada." });
            }

            usuario.NombreCompleto = dto.NombreCompleto.Trim();
            usuario.PhoneNumber = string.IsNullOrWhiteSpace(dto.Telefono)
                ? null
                : dto.Telefono.Trim();

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }

            return Ok(new
            {
                Mensaje = "Perfil actualizado correctamente.",
                Usuario = new PerfilUsuarioDTO
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email ?? string.Empty,
                    Telefono = usuario.PhoneNumber,
                    FotoPerfilUrl = usuario.FotoPerfilUrl,
                    FechaRegistro = usuario.FechaRegistro
                }
            });
        }

        [HttpPut("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDTO dto)
        {
            var usuario = await ObtenerUsuarioActualAsync();

            if (usuario == null)
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario autenticado." });
            }

            if (!usuario.EstaActivo)
            {
                return Unauthorized(new { Mensaje = "La cuenta se encuentra desactivada." });
            }

            if (dto.PasswordNuevo != dto.ConfirmarPasswordNuevo)
            {
                return BadRequest(new { Mensaje = "La nueva contraseña y la confirmación no coinciden." });
            }

            var resultado = await _userManager.ChangePasswordAsync(
                usuario,
                dto.PasswordActual,
                dto.PasswordNuevo
            );

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }

            return Ok(new { Mensaje = "Contraseña actualizada correctamente." });
        }

        [HttpPut("perfil/foto")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ActualizarFotoPerfil([FromForm] ActualizarFotoPerfilDTO dto)
        {
            var usuario = await ObtenerUsuarioActualAsync();

            if (usuario == null)
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario autenticado." });
            }

            if (!usuario.EstaActivo)
            {
                return Unauthorized(new { Mensaje = "La cuenta se encuentra desactivada." });
            }

            if (dto.Foto == null || dto.Foto.Length == 0)
            {
                return BadRequest(new { Mensaje = "Debes enviar una imagen para actualizar la foto de perfil." });
            }

            try
            {
                string urlFoto = await _storageService.SubirFotoAsync(
                    dto.Foto,
                    "usuarios/perfiles"
                );

                usuario.FotoPerfilUrl = urlFoto;

                var resultado = await _userManager.UpdateAsync(usuario);

                if (!resultado.Succeeded)
                {
                    return BadRequest(resultado.Errors);
                }

                return Ok(new
                {
                    Mensaje = "Foto de perfil actualizada correctamente.",
                    FotoPerfilUrl = usuario.FotoPerfilUrl,
                    Usuario = new PerfilUsuarioDTO
                    {
                        Id = usuario.Id,
                        NombreCompleto = usuario.NombreCompleto,
                        Email = usuario.Email ?? string.Empty,
                        Telefono = usuario.PhoneNumber,
                        FotoPerfilUrl = usuario.FotoPerfilUrl,
                        FechaRegistro = usuario.FechaRegistro
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Mensaje = $"No se pudo subir la foto de perfil: {ex.Message}"
                });
            }
        }

        [HttpDelete("perfil")]
        public async Task<IActionResult> DesactivarCuenta()
        {
            var usuario = await ObtenerUsuarioActualAsync();

            if (usuario == null)
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario autenticado." });
            }

            if (!usuario.EstaActivo)
            {
                return BadRequest(new { Mensaje = "La cuenta ya se encuentra desactivada." });
            }

            usuario.EstaActivo = false;

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }

            return Ok(new { Mensaje = "Cuenta desactivada correctamente." });
        }

        private async Task<Usuario?> ObtenerUsuarioActualAsync()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return null;
            }

            return await _userManager.FindByIdAsync(usuarioId.ToString());
        }
    }
}