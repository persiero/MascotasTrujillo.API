using MascotasTrujillo.API.Data;
using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MascotasTrujillo.API.Controllers
{
    [Authorize] // Esto asegura que solo usuarios autenticados puedan acceder a estos endpoints
    [Route("api/[controller]")]
    [ApiController]
    public class MascotasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // El Constructor: Aquí inyectamos nuestra base de datos
        public MascotasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. ENDPOINT: OBTENER TODAS LAS MASCOTAS (GET)
        [HttpGet]
        public async Task<IActionResult> GetMascotas()
        {
            // Buscamos todas las mascotas en la base de datos
            var mascotas = await _context.Mascotas.ToListAsync();
            return Ok(mascotas); // Devuelve un código 200 con la lista en formato JSON
        }

        // 2. ENDPOINT: REGISTRAR UNA NUEVA MASCOTA (POST)
        [HttpPost]
        public async Task<IActionResult> CrearMascota([FromBody] MascotaCreateDTO mascotaDto)
        {
            // ¡LA MAGIA AQUÍ! Extraemos el ID del dueño directamente desde su Token VIP
            var usuarioIdDelToken = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(usuarioIdDelToken))
            {
                return Unauthorized("No se pudo identificar al usuario desde el token.");
            }

            var nuevaMascota = new Mascota
            {
                Nombre = mascotaDto.Nombre,
                Especie = mascotaDto.Especie,
                Raza = mascotaDto.Raza,
                ColorPrincipal = mascotaDto.ColorPrincipal,
                RasgosParticulares = mascotaDto.RasgosParticulares,
                UsuarioId = usuarioIdDelToken // Se lo asignamos automáticamente y de forma segura
            };

            _context.Mascotas.Add(nuevaMascota);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMascotas), new { id = nuevaMascota.Id }, nuevaMascota);
        }
    }
}
