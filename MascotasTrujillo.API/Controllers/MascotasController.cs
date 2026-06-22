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
    public class MascotasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly R2StorageService _storageService;

        public MascotasController(ApplicationDbContext context, R2StorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // GET: api/mascotas/mis-mascotas
        [HttpGet("mis-mascotas")]
        public async Task<IActionResult> GetMisMascotas()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var mascotas = await _context.Mascotas
                .Where(m => m.UsuarioId == usuarioId && m.EstaActiva)
                .Include(m => m.Fotos)
                .Include(m => m.DispositivosGps)
                    .ThenInclude(d => d.Ubicaciones)
                .ToListAsync();

            var resultado = mascotas.Select(m =>
            {
                var fotoPrincipal = m.Fotos
                    .OrderByDescending(f => f.EsPrincipal)
                    .ThenByDescending(f => f.FechaRegistro)
                    .FirstOrDefault();

                var dispositivoActivo = m.DispositivosGps
                    .Where(d => d.Activo)
                    .OrderByDescending(d => d.FechaAsociacion)
                    .FirstOrDefault();

                var ultimaUbicacion = dispositivoActivo?.Ubicaciones
                    .OrderByDescending(u => u.FechaRegistro)
                    .FirstOrDefault();

                return new
                {
                    m.Id,
                    m.Nombre,
                    m.Especie,
                    m.Raza,
                    m.ColorPrincipal,
                    m.Sexo,
                    m.EdadAproximada,
                    m.RasgosParticulares,
                    FotoPerfilUrl = fotoPrincipal?.UrlFoto,
                    DispositivoId = dispositivoActivo?.CodigoDispositivo,
                    UltimaActualizacion = ultimaUbicacion?.FechaRegistro,
                    Latitud = ultimaUbicacion?.Ubicacion != null ? (double?)ultimaUbicacion.Ubicacion.Y : null,
                    Longitud = ultimaUbicacion?.Ubicacion != null ? (double?)ultimaUbicacion.Ubicacion.X : null
                };
            });

            return Ok(resultado);
        }

        // POST: api/mascotas
        [HttpPost]
        public async Task<IActionResult> CrearMascota([FromForm] MascotaCreateDTO mascotaDto)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

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
                Sexo = mascotaDto.Sexo,
                EdadAproximada = mascotaDto.EdadAproximada,
                RasgosParticulares = mascotaDto.RasgosParticulares,
                UsuarioId = usuarioId,
                EstaActiva = true,
                FechaRegistro = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(urlFotoReal))
            {
                nuevaMascota.Fotos.Add(new FotoMascota
                {
                    UrlFoto = urlFotoReal,
                    EsPrincipal = true,
                    FechaRegistro = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrWhiteSpace(mascotaDto.DispositivoId))
            {
                nuevaMascota.DispositivosGps.Add(new DispositivoGps
                {
                    CodigoDispositivo = mascotaDto.DispositivoId,
                    NombreDispositivo = "Collar GPS",
                    EstadoConexion = "Desconectado",
                    Activo = true,
                    FechaAsociacion = DateTime.UtcNow
                });
            }

            _context.Mascotas.Add(nuevaMascota);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "¡Mascota registrada con éxito!",
                Id = nuevaMascota.Id
            });
        }

        // POST: api/mascotas/actualizar-ubicacion-iot
        [AllowAnonymous]
        [HttpPost("actualizar-ubicacion-iot")]
        public async Task<IActionResult> ActualizarUbicacionIoT([FromBody] MascotaUbicacionIoTRequest request)
        {
            var dispositivo = await _context.DispositivosGps
                .FirstOrDefaultAsync(d => d.CodigoDispositivo == request.DispositivoId && d.Activo);

            if (dispositivo == null)
            {
                return NotFound(new { Mensaje = "El identificador del dispositivo no está registrado." });
            }

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            var nuevaUbicacion = new UbicacionGps
            {
                DispositivoGpsId = dispositivo.Id,
                Ubicacion = geometryFactory.CreatePoint(new Coordinate(request.Longitud, request.Latitud)),
                Bateria = request.Bateria,
                FechaRegistro = DateTime.UtcNow
            };

            dispositivo.EstadoConexion = "Conectado";

            _context.UbicacionesGps.Add(nuevaUbicacion);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Ubicación actualizada correctamente." });
        }
    }
}