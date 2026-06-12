using MascotasTrujillo.API.Data;
using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using MascotasTrujillo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Security.Claims;

namespace MascotasTrujillo.API.Controllers
{
    [Authorize] // Solo usuarios autenticados pueden reportar avistamientos
    [Route("api/[controller]")]
    [ApiController]
    public class AvistamientosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly R2StorageService _storageService;

        public AvistamientosController(ApplicationDbContext context, R2StorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        [HttpPost]
        public async Task<IActionResult> ReportarAvistamiento([FromForm] AvistamientoCreateDTO dto)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

            string urlFotoReal = await _storageService.SubirFotoAsync(dto.Foto);

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var ubicacionPoint = geometryFactory.CreatePoint(new Coordinate(dto.Longitud, dto.Latitud));

            var nuevoAvistamiento = new Avistamiento
            {
                UsuarioId = usuarioId,
                FotoUrl = urlFotoReal,
                Descripcion = dto.Descripcion,
                Ubicacion = ubicacionPoint,
                FechaHora = DateTime.UtcNow,
                IsResolved = false // Explícito por seguridad
            };

            _context.Avistamientos.Add(nuevoAvistamiento);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "¡Avistamiento reportado con éxito!", Id = nuevoAvistamiento.Id });
        }

        // Endpoint extra para ver todos los avistamientos activos
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var avistamientos = await _context.Avistamientos
                .Where(a => !a.IsResolved) // MODIFICACIÓN: Solo traer los NO resueltos
                .Select(a => new
                {
                    a.Id,
                    a.UsuarioId,
                    a.FotoUrl,
                    a.Descripcion,
                    a.FechaHora,
                    Latitud = a.Ubicacion.Y,
                    Longitud = a.Ubicacion.X
                })
                .ToListAsync();

            return Ok(avistamientos);
        }

        // NUEVO ENDPOINT: Obtiene solo los avistamientos del usuario autenticado
        [HttpGet("mis-reportes")]
        public async Task<IActionResult> ObtenerMisReportes()
        {
            // 1. Extraemos el ID del usuario directamente desde su Token JWT (Su firma digital)
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

            // 2. Filtramos en PostgreSQL: que pertenezcan a este usuario Y que no estén resueltos
            var misAvistamientos = await _context.Avistamientos
                .Where(a => a.UsuarioId == usuarioId && !a.IsResolved)
                .Select(a => new
                {
                    a.Id,
                    a.UsuarioId,
                    a.FotoUrl,
                    a.Descripcion,
                    a.FechaHora,
                    Latitud = a.Ubicacion.Y,
                    Longitud = a.Ubicacion.X
                })
                .ToListAsync();

            return Ok(misAvistamientos);
        }

        [HttpGet("cercanos")]
        public async Task<IActionResult> ObtenerCercanos(double latitud, double longitud, double radioMetros = 3000)
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var miUbicacion = geometryFactory.CreatePoint(new Coordinate(longitud, latitud));

            var cercanos = await _context.Avistamientos
                .Where(a => !a.IsResolved && a.Ubicacion.IsWithinDistance(miUbicacion, radioMetros)) // MODIFICACIÓN: Incluye !a.IsResolved
                .OrderBy(a => a.Ubicacion.Distance(miUbicacion))
                .Select(a => new
                {
                    a.Id,
                    a.FotoUrl,
                    a.Descripcion,
                    a.FechaHora,
                    Latitud = a.Ubicacion.Y,
                    Longitud = a.Ubicacion.X,
                    DistanciaMetros = Math.Round(a.Ubicacion.Distance(miUbicacion), 2)
                })
                .ToListAsync();

            return Ok(cercanos);
        }

        // NUEVO ENDPOINT: Cambia el estado del reporte a resuelto
        [HttpPut("{id}/resolver")]
        public async Task<IActionResult> MarcarComoResuelto(int id)
        {
            // 1. Buscamos el reporte en PostgreSQL
            var avistamiento = await _context.Avistamientos.FindAsync(id);

            if (avistamiento == null)
            {
                return NotFound(new { Mensaje = "El reporte de avistamiento no existe." });
            }

            // Opcional: Validar que el usuario que lo resuelve sea el mismo dueño del reporte
            var usuarioActualId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (avistamiento.UsuarioId != usuarioActualId)
            {
                return Forbid(); // No puede resolver un reporte ajeno
            }

            // 2. Modificamos el estado
            avistamiento.IsResolved = true;

            // 3. Guardamos los cambios en la BD
            _context.Entry(avistamiento).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Mascota marcada como encontrada con éxito." });
        }
    }
}