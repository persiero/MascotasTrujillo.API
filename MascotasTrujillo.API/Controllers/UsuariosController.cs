using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
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

        public UsuariosController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
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