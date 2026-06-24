using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MascotasTrujillo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;

        // Inyectamos el UserManager (que maneja las contraseñas encriptadas) y Configuration (para leer el appsettings)
        public AuthController(UserManager<Usuario> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistroDTO dto)
        {
            var nuevoUsuario = new Usuario
            {
                UserName = dto.Email,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                PhoneNumber = dto.Telefono
            };

            // UserManager se encarga de encriptar el password automáticamente (Hashing)
            var resultado = await _userManager.CreateAsync(nuevoUsuario, dto.Password);

            if (resultado.Succeeded)
                return Ok(new { Mensaje = "¡Usuario creado exitosamente!" });

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
                Telefono = usuario.PhoneNumber
            });
        }
    }
}
