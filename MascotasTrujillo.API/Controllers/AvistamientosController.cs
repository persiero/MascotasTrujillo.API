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
            // 1. Identificamos al héroe que reporta (desde su Token VIP)
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

            // 1. SUBIMOS LA FOTO A CLOUDFLARE R2 Y OBTENEMOS LA URL
            string urlFotoReal = await _storageService.SubirFotoAsync(dto.Foto);

            // 2. LA MAGIA ESPACIAL: Creamos el convertidor de coordenadas
            // El número 4326 es el código mundial para el sistema GPS estándar (WGS 84)
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            // OJO: En geografía, primero va la Longitud (X) y luego la Latitud (Y)
            var ubicacionPoint = geometryFactory.CreatePoint(new Coordinate(dto.Longitud, dto.Latitud));

            // 3. Ensamblamos el modelo para la base de datos
            var nuevoAvistamiento = new Avistamiento
            {
                UsuarioId = usuarioId,
                FotoUrl = urlFotoReal,
                Descripcion = dto.Descripcion,
                Ubicacion = ubicacionPoint, // ¡Asignamos el Punto exacto en el mapa!
                FechaHora = DateTime.UtcNow
            };

            _context.Avistamientos.Add(nuevoAvistamiento);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "¡Avistamiento reportado con éxito!", Id = nuevoAvistamiento.Id });
        }

        // Endpoint extra para ver todos los avistamientos reportados
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var avistamientos = await _context.Avistamientos
                .Select(a => new
                {
                    a.Id,
                    a.UsuarioId,
                    a.FotoUrl,
                    a.Descripcion,
                    a.FechaHora,
                    // Devolvemos la latitud y longitud sueltas para que la app móvil no se confunda
                    Latitud = a.Ubicacion.Y,
                    Longitud = a.Ubicacion.X
                })
                .ToListAsync();

            return Ok(avistamientos);
        }

        [HttpGet("cercanos")]
        public async Task<IActionResult> ObtenerCercanos(double latitud, double longitud, double radioMetros = 3000)
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var miUbicacion = geometryFactory.CreatePoint(new Coordinate(longitud, latitud));

            // Usamos la función IsWithinDistance de NetTopologySuite
            // que Entity Framework traduce automáticamente a ST_DWithin de PostGIS
            var cercanos = await _context.Avistamientos
                .Where(a => a.Ubicacion.IsWithinDistance(miUbicacion, radioMetros))
                .OrderBy(a => a.Ubicacion.Distance(miUbicacion)) // Los más cercanos primero
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


    }
}
