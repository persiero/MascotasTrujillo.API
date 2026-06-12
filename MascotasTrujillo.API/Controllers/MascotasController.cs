using MascotasTrujillo.API.Data;
using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using MascotasTrujillo.API.Services; // IMPORTANTE: Para usar tu R2StorageService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries; // IMPORTANTE: Para la lógica de Point
using System.Security.Claims;

namespace MascotasTrujillo.API.Controllers
{
    [Authorize] // Asegura que los endpoints de usuario requieran token VIP
    [Route("api/[controller]")]
    [ApiController]
    public class MascotasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly R2StorageService _storageService; // Inyectamos tu servicio de Cloudflare R2

        // Constructor actualizado con doble inyección
        public MascotasController(ApplicationDbContext context, R2StorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // 1. ENDPOINT: OBTENER MIS MASCOTAS (GET)
        // MODIFICACIÓN: Cambiado para que solo devuelva los animales del dueño autenticado
        [HttpGet("mis-mascotas")]
        public async Task<IActionResult> GetMisMascotas()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

            // Buscamos solo las mascotas que le pertenecen a este usuario en PostgreSQL
            var mascotas = await _context.Mascotas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => new
                {
                    m.Id,
                    m.Nombre,
                    m.Especie,
                    m.Raza,
                    m.ColorPrincipal,
                    m.RasgosParticulares,
                    m.FotoPerfilUrl,
                    m.DispositivoId,
                    m.UltimaActualizacion,
                    // Convertimos el punto geográfico a coordenadas sueltas legibles para MAUI
                    Latitud = m.UltimaUbicacion != null ? (double?)m.UltimaUbicacion.Y : null,
                    Longitud = m.UltimaUbicacion != null ? (double?)m.UltimaUbicacion.X : null
                })
                .ToListAsync();

            return Ok(mascotas);
        }

        // 2. ENDPOINT: REGISTRAR UNA NUEVA MASCOTA (POST)
        // MODIFICACIÓN: Usamos [FromForm] para recibir la foto de la cámara/galería
        [HttpPost]
        public async Task<IActionResult> CrearMascota([FromForm] MascotaCreateDTO mascotaDto)
        {
            // ¡LA MAGIA AQUÍ! Extraemos el ID del dueño directamente desde su Token VIP
            var usuarioIdDelToken = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(usuarioIdDelToken))
            {
                return Unauthorized("No se pudo identificar al usuario desde el token.");
            }

            // Subimos la foto a R2 si es que el usuario seleccionó una imagen desde la app
            string? urlFotoReal = null;
            if (mascotaDto.Foto != null)
            {
                urlFotoReal = await _storageService.SubirFotoAsync(mascotaDto.Foto);
            }

            var nuevaMascota = new Mascota
            {
                Nombre = mascotaDto.Nombre,
                Especie = mascotaDto.Especie,
                Raza = mascotaDto.Raza,
                ColorPrincipal = mascotaDto.ColorPrincipal,
                RasgosParticulares = mascotaDto.RasgosParticulares,
                DispositivoId = mascotaDto.DispositivoId, // Vinculamos el hardware
                FotoPerfilUrl = urlFotoReal, // Guardamos el enlace de Cloudflare
                UsuarioId = usuarioIdDelToken // Asignación automática y segura
            };

            _context.Mascotas.Add(nuevaMascota);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "¡Mascota registrada con éxito!", Id = nuevaMascota.Id });
        }

        // 3. ENDPOINT IoT: ACTUALIZAR UBICACIÓN DESDE EL COLLAR GPS (POST)
        // CRÍTICO: Usamos [AllowAnonymous] para que el chip de celular del collar pueda
        // conectarse de forma pública sin requerir un token JWT.
        [AllowAnonymous]
        [HttpPost("actualizar-ubicacion-iot")]
        public async Task<IActionResult> ActualizarUbicacionIoT([FromBody] MascotaUbicacionIoTRequest request)
        {
            // Buscamos a qué mascota le pertenece este collar por su ID único
            var mascota = await _context.Mascotas
                .FirstOrDefaultAsync(m => m.DispositivoId == request.DispositivoId);

            if (mascota == null)
            {
                return NotFound(new { Mensaje = "El identificador del dispositivo no está registrado." });
            }

            // LA MAGIA ESPACIAL: Reconstruimos el Punto geográfico (SRID 4326 = GPS Estándar)
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            mascota.UltimaUbicacion = geometryFactory.CreatePoint(new Coordinate(request.Longitud, request.Latitud));
            mascota.UltimaActualizacion = DateTime.UtcNow;

            _context.Entry(mascota).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Respuesta limpia y sin peso para no consumir el plan de datos del chip IoT
            return Ok();
        }
    }
}