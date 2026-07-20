using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MascotasTrujillo.API.Services;
using System.Security.Cryptography;

namespace MascotasTrujillo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AuthController(
            UserManager<Usuario> userManager,
            IConfiguration configuration,
            EmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistroDTO dto)
        {
            if (dto.Password != dto.ConfirmarPassword)
            {
                return BadRequest(new { Mensaje = "La contraseña y la confirmación no coinciden." });
            }

            var usuarioExistente = await _userManager.FindByEmailAsync(dto.Email);

            if (usuarioExistente != null)
            {
                return BadRequest(new { Mensaje = "Ya existe una cuenta registrada con este correo electrónico." });
            }

            var nuevoUsuario = new Usuario
            {
                UserName = dto.Email,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(dto.Telefono)
                    ? null
                    : dto.Telefono.Trim()
            };

            var resultado = await _userManager.CreateAsync(nuevoUsuario, dto.Password);

            if (resultado.Succeeded)
                return Ok(new { Mensaje = "Usuario creado exitosamente." });

            return BadRequest(resultado.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // 1. Buscamos si existe el correo
            var usuario = await _userManager.FindByEmailAsync(dto.Email);
            if (usuario == null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
                return Unauthorized(new { Mensaje = "Credenciales incorrectas" });

            if (!usuario.EstaActivo)
            {
                return Unauthorized(new { Mensaje = "La cuenta se encuentra desactivada." });
            }

            // 2. Si es correcto, fabricamos la "Pulsera VIP" (JWT Token)
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Email!),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), // Guardamos el ID real dentro del token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddDays(15), // La pulsera dura 15 días
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiracion = token.ValidTo,
                UsuarioId = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Telefono = usuario.PhoneNumber,
                FotoPerfilUrl = usuario.FotoPerfilUrl
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            var usuario = await _userManager.FindByEmailAsync(dto.Email);

            if (usuario == null || !usuario.EstaActivo)
            {
                return Ok(new
                {
                    Mensaje = "Si el correo existe, se enviará un código de recuperación."
                });
            }

            string codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            usuario.CodigoRecuperacionPassword = codigo;
            usuario.CodigoRecuperacionExpira = DateTime.UtcNow.AddMinutes(15);

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }

            string cuerpo = $@"
        <div style='font-family:Arial,sans-serif; color:#1F2340;'>
            <h2 style='color:#5B21E6;'>Pet Guardian 365</h2>
            <p>Hola {usuario.NombreCompleto},</p>
            <p>Recibimos una solicitud para recuperar tu contraseña.</p>
            <p>Tu código de recuperación es:</p>
            <div style='font-size:28px; font-weight:bold; color:#5B21E6; letter-spacing:4px;'>
                {codigo}
            </div>
            <p>Este código vencerá en 15 minutos.</p>
            <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
        </div>";

            try
            {
                await _emailService.EnviarCorreoAsync(
                    usuario.Email!,
                    "Código de recuperación - Pet Guardian 365",
                    cuerpo
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Mensaje = $"No se pudo enviar el correo de recuperación: {ex.Message}"
                });
            }

            return Ok(new
            {
                Mensaje = "Si el correo existe, se enviará un código de recuperación."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (dto.PasswordNuevo != dto.ConfirmarPasswordNuevo)
            {
                return BadRequest(new
                {
                    Mensaje = "La nueva contraseña y la confirmación no coinciden."
                });
            }

            var usuario = await _userManager.FindByEmailAsync(dto.Email);

            if (usuario == null || !usuario.EstaActivo)
            {
                return BadRequest(new
                {
                    Mensaje = "No se pudo restablecer la contraseña."
                });
            }

            if (string.IsNullOrWhiteSpace(usuario.CodigoRecuperacionPassword) ||
                usuario.CodigoRecuperacionPassword != dto.Codigo)
            {
                return BadRequest(new
                {
                    Mensaje = "El código de recuperación no es válido."
                });
            }

            if (!usuario.CodigoRecuperacionExpira.HasValue ||
                usuario.CodigoRecuperacionExpira.Value < DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    Mensaje = "El código de recuperación ha expirado."
                });
            }

            string token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

            var resultado = await _userManager.ResetPasswordAsync(
                usuario,
                token,
                dto.PasswordNuevo
            );

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }

            usuario.CodigoRecuperacionPassword = null;
            usuario.CodigoRecuperacionExpira = null;

            await _userManager.UpdateAsync(usuario);

            return Ok(new
            {
                Mensaje = "Contraseña restablecida correctamente."
            });
        }
    }
}
