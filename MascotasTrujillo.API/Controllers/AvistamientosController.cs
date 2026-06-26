using MascotasTrujillo.API.Data;
using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Models;
using MascotasTrujillo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Security.Claims;

namespace MascotasTrujillo.API.Controllers
{
    [Authorize]
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

        // POST: api/avistamientos
        [HttpPost]
        public async Task<IActionResult> RegistrarAvistamiento([FromForm] AvistamientoCreateDTO dto)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var reporte = await _context.Reportes
                .FirstOrDefaultAsync(r => r.Id == dto.ReporteId && r.Visible);

            if (reporte == null)
            {
                return NotFound(new { Mensaje = "El reporte asociado no existe." });
            }

            // Solo reportes activos
            if (reporte.EstadoReporteId != 1)
            {
                return BadRequest(new { Mensaje = "Solo se pueden registrar avistamientos en reportes activos." });
            }

            // Solo reportes de mascota perdida
            if (reporte.TipoReporteId != 1)
            {
                return BadRequest(new { Mensaje = "Los avistamientos solo aplican a reportes de mascotas perdidas." });
            }

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var ubicacionPoint = geometryFactory.CreatePoint(new Coordinate(dto.Longitud, dto.Latitud));

            var nuevoAvistamiento = new Avistamiento
            {
                ReporteId = dto.ReporteId,
                UsuarioId = usuarioId,
                Descripcion = dto.Descripcion,
                Ubicacion = ubicacionPoint,
                DireccionReferencia = dto.DireccionReferencia,
                FechaAvistamiento = DateTime.UtcNow,
                Visible = true
            };

            if (dto.Foto != null)
            {
                var urlFotoReal = await _storageService.SubirFotoAsync(dto.Foto);

                nuevoAvistamiento.Fotos.Add(new FotoAvistamiento
                {
                    UrlFoto = urlFotoReal,
                    FechaRegistro = DateTime.UtcNow
                });
            }

            _context.Avistamientos.Add(nuevoAvistamiento);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "¡Avistamiento registrado con éxito!",
                Id = nuevoAvistamiento.Id
            });
        }

        // GET: api/avistamientos/reporte/5
        // Consulta la lista básica de avistamientos asociados a un reporte.
        [HttpGet("reporte/{reporteId:long}")]
        public async Task<IActionResult> ObtenerPorReporte(long reporteId)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var reporte = await _context.Reportes
                .FirstOrDefaultAsync(r => r.Id == reporteId && r.Visible);

            if (reporte == null)
            {
                return NotFound(new { Mensaje = "El reporte no existe." });
            }

            bool esDuenoReporte = reporte.UsuarioId == usuarioId;

            var avistamientosDb = await _context.Avistamientos
                .Where(a => a.ReporteId == reporteId && a.Visible)
                .OrderByDescending(a => a.FechaAvistamiento)
                .Select(a => new
                {
                    a.Id,
                    a.ReporteId,
                    a.UsuarioId,
                    a.Descripcion,
                    a.DireccionReferencia,
                    a.FechaAvistamiento,
                    Ubicacion = a.Ubicacion,
                    FotoUrl = a.Fotos
                        .OrderByDescending(f => f.FechaRegistro)
                        .Select(f => f.UrlFoto)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var avistamientos = avistamientosDb.Select(a =>
            {
                bool esAutorAvistamiento = a.UsuarioId == usuarioId;
                bool puedeVerDetalle = esDuenoReporte || esAutorAvistamiento;
                bool puedeContactar = esDuenoReporte && !esAutorAvistamiento;

                return new
                {
                    a.Id,
                    a.ReporteId,
                    a.UsuarioId,
                    a.Descripcion,
                    a.DireccionReferencia,
                    a.FechaAvistamiento,
                    Latitud = a.Ubicacion.Y,
                    Longitud = a.Ubicacion.X,
                    a.FotoUrl,

                    EsDuenoReporte = esDuenoReporte,
                    EsAutorAvistamiento = esAutorAvistamiento,
                    PuedeVerDetalle = puedeVerDetalle,
                    PuedeContactar = puedeContactar
                };
            });

            return Ok(avistamientos);
        }

        // GET: api/avistamientos/mis-avistamientos
        [HttpGet("mis-avistamientos")]
        public async Task<IActionResult> ObtenerMisAvistamientos()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var misAvistamientos = await _context.Avistamientos
                .Where(a => a.UsuarioId == usuarioId && a.Visible)
                .Select(a => new
                {
                    a.Id,
                    a.ReporteId,
                    ReporteTitulo = a.Reporte != null ? a.Reporte.Titulo : null,
                    a.Descripcion,
                    a.DireccionReferencia,
                    a.FechaAvistamiento,
                    Latitud = a.Ubicacion.Y,
                    Longitud = a.Ubicacion.X,
                    FotoUrl = a.Fotos
                        .OrderByDescending(f => f.FechaRegistro)
                        .Select(f => f.UrlFoto)
                        .FirstOrDefault()
                })
                .OrderByDescending(a => a.FechaAvistamiento)
                .ToListAsync();

            return Ok(misAvistamientos);
        }

        // GET: api/avistamientos/10
        [HttpGet("{id:long}")]
        public async Task<IActionResult> ObtenerDetalle(long id)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var avistamiento = await _context.Avistamientos
                .Include(a => a.Reporte)
                .Include(a => a.Usuario)
                .Include(a => a.Fotos)
                .FirstOrDefaultAsync(a => a.Id == id && a.Visible);

            if (avistamiento == null)
            {
                return NotFound(new { Mensaje = "El avistamiento no existe." });
            }

            bool esDuenoReporte = avistamiento.Reporte != null &&
                                   avistamiento.Reporte.UsuarioId == usuarioId;

            bool esAutorAvistamiento = avistamiento.UsuarioId == usuarioId;

            bool puedeVerDetalle = esDuenoReporte || esAutorAvistamiento;

            if (!puedeVerDetalle)
            {
                return Forbid();
            }

            bool puedeContactar = esDuenoReporte && !esAutorAvistamiento;

            string? fotoUrl = avistamiento.Fotos
                .OrderByDescending(f => f.FechaRegistro)
                .Select(f => f.UrlFoto)
                .FirstOrDefault();

            return Ok(new
            {
                avistamiento.Id,
                avistamiento.ReporteId,
                ReporteTitulo = avistamiento.Reporte?.Titulo,
                avistamiento.UsuarioId,

                NombreContacto = avistamiento.Usuario != null
                    ? avistamiento.Usuario.NombreCompleto
                    : null,

                TelefonoContacto = avistamiento.Usuario != null
                    ? avistamiento.Usuario.PhoneNumber
                    : null,

                avistamiento.Descripcion,
                avistamiento.DireccionReferencia,
                avistamiento.FechaAvistamiento,

                Latitud = avistamiento.Ubicacion.Y,
                Longitud = avistamiento.Ubicacion.X,

                FotoUrl = fotoUrl,

                EsDuenoReporte = esDuenoReporte,
                EsAutorAvistamiento = esAutorAvistamiento,
                PuedeVerDetalle = puedeVerDetalle,
                PuedeContactar = puedeContactar
            });
        }



    }
}
