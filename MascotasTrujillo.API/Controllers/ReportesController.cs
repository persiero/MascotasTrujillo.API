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
    public class ReportesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly R2StorageService _storageService;

        public ReportesController(ApplicationDbContext context, R2StorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // POST: api/reportes
        [HttpPost]
        public async Task<IActionResult> CrearReporte([FromForm] ReporteCreateDTO dto)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            if (dto.TipoReporteId != 1 && dto.TipoReporteId != 2)
            {
                return BadRequest(new { Mensaje = "El tipo de reporte no es válido. Use 1 para Perdida o 2 para Encontrada." });
            }

            if (dto.MascotaId.HasValue)
            {
                var mascotaExiste = await _context.Mascotas
                    .AnyAsync(m => m.Id == dto.MascotaId.Value &&
                                   m.UsuarioId == usuarioId &&
                                   m.EstaActiva);

                if (!mascotaExiste)
                {
                    return BadRequest(new { Mensaje = "La mascota indicada no existe o no pertenece al usuario autenticado." });
                }
            }

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var ubicacionPoint = geometryFactory.CreatePoint(new Coordinate(dto.Longitud, dto.Latitud));

            var nuevoReporte = new Reporte
            {
                UsuarioId = usuarioId,
                MascotaId = dto.MascotaId,
                TipoReporteId = dto.TipoReporteId,
                EstadoReporteId = 1, // Activo
                Titulo = dto.Titulo,
                Descripcion = dto.Descripcion,
                NombreMascotaReferencial = dto.NombreMascotaReferencial,
                EspecieReferencial = dto.EspecieReferencial,
                RazaReferencial = dto.RazaReferencial,
                ColorReferencial = dto.ColorReferencial,
                SexoReferencial = dto.SexoReferencial,
                Ubicacion = ubicacionPoint,
                DireccionReferencia = dto.DireccionReferencia,
                FechaReporte = DateTime.UtcNow,
                Visible = true
            };

            if (dto.Foto != null)
            {
                var urlFotoReal = await _storageService.SubirFotoAsync(dto.Foto);

                nuevoReporte.Fotos.Add(new FotoReporte
                {
                    UrlFoto = urlFotoReal,
                    FechaRegistro = DateTime.UtcNow
                });
            }

            _context.Reportes.Add(nuevoReporte);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "¡Reporte registrado con éxito!",
                Id = nuevoReporte.Id
            });
        }

        // GET: api/reportes
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var reportesDb = await _context.Reportes
                .Where(r => r.EstadoReporteId == 1 && r.Visible)
                .Select(r => new
                {
                    r.Id,
                    r.UsuarioId,
                    r.MascotaId,
                    TipoReporte = r.TipoReporte != null ? r.TipoReporte.Nombre : null,
                    EstadoReporte = r.EstadoReporte != null ? r.EstadoReporte.Nombre : null,
                    r.Titulo,
                    r.Descripcion,
                    r.NombreMascotaReferencial,
                    r.EspecieReferencial,
                    r.RazaReferencial,
                    r.ColorReferencial,
                    r.SexoReferencial,
                    FotoUrl = r.Fotos
                        .OrderByDescending(f => f.FechaRegistro)
                        .Select(f => f.UrlFoto)
                        .FirstOrDefault(),
                    r.FechaReporte,
                    Ubicacion = r.Ubicacion
                })
                .ToListAsync();

            var reportes = reportesDb.Select(r => new
            {
                r.Id,
                r.UsuarioId,
                r.MascotaId,
                r.TipoReporte,
                r.EstadoReporte,
                r.Titulo,
                r.Descripcion,
                r.NombreMascotaReferencial,
                r.EspecieReferencial,
                r.RazaReferencial,
                r.ColorReferencial,
                r.SexoReferencial,
                r.FotoUrl,
                r.FechaReporte,
                Latitud = r.Ubicacion.Y,
                Longitud = r.Ubicacion.X
            });

            return Ok(reportes);
        }

        // GET: api/reportes/mis-reportes
        [HttpGet("mis-reportes")]
        public async Task<IActionResult> ObtenerMisReportes()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var misReportesDb = await _context.Reportes
                .Where(r => r.UsuarioId == usuarioId && r.Visible)
                .Select(r => new
                {
                    r.Id,
                    r.MascotaId,
                    TipoReporte = r.TipoReporte != null ? r.TipoReporte.Nombre : null,
                    EstadoReporte = r.EstadoReporte != null ? r.EstadoReporte.Nombre : null,
                    r.Titulo,
                    r.Descripcion,
                    r.NombreMascotaReferencial,
                    r.EspecieReferencial,
                    r.RazaReferencial,
                    r.ColorReferencial,
                    r.SexoReferencial,
                    FotoUrl = r.Fotos
                        .OrderByDescending(f => f.FechaRegistro)
                        .Select(f => f.UrlFoto)
                        .FirstOrDefault(),
                    r.FechaReporte,
                    r.FechaResolucion,
                    Ubicacion = r.Ubicacion
                })
                .ToListAsync();

            var misReportes = misReportesDb.Select(r => new
            {
                r.Id,
                r.MascotaId,
                r.TipoReporte,
                r.EstadoReporte,
                r.Titulo,
                r.Descripcion,
                r.NombreMascotaReferencial,
                r.EspecieReferencial,
                r.RazaReferencial,
                r.ColorReferencial,
                r.SexoReferencial,
                r.FotoUrl,
                r.FechaReporte,
                r.FechaResolucion,
                Latitud = r.Ubicacion.Y,
                Longitud = r.Ubicacion.X
            });

            return Ok(misReportes);
        }

        // GET: api/reportes/cercanos?latitud=-8.11&longitud=-79.03&radioMetros=3000
        [HttpGet("cercanos")]
        public async Task<IActionResult> ObtenerCercanos(double latitud, double longitud, double radioMetros = 3000)
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var miUbicacion = geometryFactory.CreatePoint(new Coordinate(longitud, latitud));

            var cercanosDb = await _context.Reportes
                .Where(r => r.EstadoReporteId == 1 &&
                            r.Visible &&
                            r.Ubicacion.IsWithinDistance(miUbicacion, radioMetros))
                .OrderBy(r => r.Ubicacion.Distance(miUbicacion))
                .Select(r => new
                {
                    r.Id,
                    r.UsuarioId,
                    r.MascotaId,
                    TipoReporte = r.TipoReporte != null ? r.TipoReporte.Nombre : null,
                    EstadoReporte = r.EstadoReporte != null ? r.EstadoReporte.Nombre : null,
                    r.Titulo,
                    r.Descripcion,
                    r.NombreMascotaReferencial,
                    r.EspecieReferencial,
                    r.RazaReferencial,
                    r.ColorReferencial,
                    FotoUrl = r.Fotos
                        .OrderByDescending(f => f.FechaRegistro)
                        .Select(f => f.UrlFoto)
                        .FirstOrDefault(),
                    r.FechaReporte,
                    Ubicacion = r.Ubicacion,
                    DistanciaMetros = r.Ubicacion.Distance(miUbicacion)
                })
                .ToListAsync();

            var resultado = cercanosDb.Select(r => new
            {
                r.Id,
                r.UsuarioId,
                r.MascotaId,
                r.TipoReporte,
                r.EstadoReporte,
                r.Titulo,
                r.Descripcion,
                r.NombreMascotaReferencial,
                r.EspecieReferencial,
                r.RazaReferencial,
                r.ColorReferencial,
                r.FotoUrl,
                r.FechaReporte,
                Latitud = r.Ubicacion.Y,
                Longitud = r.Ubicacion.X,
                DistanciaMetros = Math.Round(r.DistanciaMetros, 2)
            });

            return Ok(resultado);
        }

        // PUT: api/reportes/5/resolver
        [HttpPut("{id:long}/resolver")]
        public async Task<IActionResult> MarcarComoResuelto(long id)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var reporte = await _context.Reportes.FindAsync(id);

            if (reporte == null)
            {
                return NotFound(new { Mensaje = "El reporte no existe." });
            }

            if (reporte.UsuarioId != usuarioId)
            {
                return Forbid();
            }

            reporte.EstadoReporteId = 2; // Resuelto
            reporte.FechaResolucion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Reporte marcado como resuelto correctamente." });
        }
    }
}