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
                .Include(m => m.InformacionSalud)
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
                    
                    Enfermedades = m.InformacionSalud?.Enfermedades,
                    Discapacidades = m.InformacionSalud?.Discapacidades,
                    Tratamientos = m.InformacionSalud?.Tratamientos,
                    NecesidadesEspeciales = m.InformacionSalud?.NecesidadesEspeciales,

                    FotoPerfilUrl = fotoPrincipal?.UrlFoto,
                    ObservacionesSalud = m.InformacionSalud?.Observaciones,
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

            bool tieneInformacionSalud =
                !string.IsNullOrWhiteSpace(mascotaDto.Enfermedades) ||
                !string.IsNullOrWhiteSpace(mascotaDto.Discapacidades) ||
                !string.IsNullOrWhiteSpace(mascotaDto.Tratamientos) ||
                !string.IsNullOrWhiteSpace(mascotaDto.NecesidadesEspeciales) ||
                !string.IsNullOrWhiteSpace(mascotaDto.ObservacionesSalud);

            if (tieneInformacionSalud)
            {
                nuevaMascota.InformacionSalud = new InformacionSaludMascota
                {
                    Enfermedades = mascotaDto.Enfermedades,
                    Discapacidades = mascotaDto.Discapacidades,
                    Tratamientos = mascotaDto.Tratamientos,
                    NecesidadesEspeciales = mascotaDto.NecesidadesEspeciales,
                    Observaciones = mascotaDto.ObservacionesSalud
                };
            }

            _context.Mascotas.Add(nuevaMascota);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "¡Mascota registrada con éxito!",
                Id = nuevaMascota.Id
            });
        }

        // PUT: api/mascotas/5
        [HttpPut("{id:long}")]
        public async Task<IActionResult> ActualizarMascota(long id, [FromForm] MascotaUpdateDTO mascotaDto)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var mascota = await _context.Mascotas
                .Include(m => m.Fotos)
                .Include(m => m.DispositivosGps)
                .Include(m => m.InformacionSalud)
                .FirstOrDefaultAsync(m => m.Id == id &&
                                          m.UsuarioId == usuarioId &&
                                          m.EstaActiva);
            if (mascota == null)
            {
                return NotFound(new { Mensaje = "La mascota no existe o no pertenece al usuario autenticado." });
            }

            mascota.Nombre = mascotaDto.Nombre;
            mascota.Especie = mascotaDto.Especie;
            mascota.Raza = mascotaDto.Raza;
            mascota.ColorPrincipal = mascotaDto.ColorPrincipal;
            mascota.Sexo = mascotaDto.Sexo;
            mascota.EdadAproximada = mascotaDto.EdadAproximada;
            mascota.RasgosParticulares = mascotaDto.RasgosParticulares;

            if (mascotaDto.Foto != null)
            {
                var urlFotoReal = await _storageService.SubirFotoAsync(mascotaDto.Foto);

                foreach (var foto in mascota.Fotos)
                {
                    foto.EsPrincipal = false;
                }

                mascota.Fotos.Add(new FotoMascota
                {
                    UrlFoto = urlFotoReal,
                    EsPrincipal = true,
                    FechaRegistro = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrWhiteSpace(mascotaDto.DispositivoId))
            {
                var dispositivoActivo = mascota.DispositivosGps
                    .Where(d => d.Activo)
                    .OrderByDescending(d => d.FechaAsociacion)
                    .FirstOrDefault();

                if (dispositivoActivo == null)
                {
                    mascota.DispositivosGps.Add(new DispositivoGps
                    {
                        CodigoDispositivo = mascotaDto.DispositivoId,
                        NombreDispositivo = "Collar GPS",
                        EstadoConexion = "Desconectado",
                        Activo = true,
                        FechaAsociacion = DateTime.UtcNow
                    });
                }
                else if (dispositivoActivo.CodigoDispositivo != mascotaDto.DispositivoId)
                {
                    dispositivoActivo.Activo = false;

                    mascota.DispositivosGps.Add(new DispositivoGps
                    {
                        CodigoDispositivo = mascotaDto.DispositivoId,
                        NombreDispositivo = "Collar GPS",
                        EstadoConexion = "Desconectado",
                        Activo = true,
                        FechaAsociacion = DateTime.UtcNow
                    });
                }
            }

            bool tieneInformacionSalud =
                !string.IsNullOrWhiteSpace(mascotaDto.Enfermedades) ||
                !string.IsNullOrWhiteSpace(mascotaDto.Discapacidades) ||
                !string.IsNullOrWhiteSpace(mascotaDto.Tratamientos) ||
                !string.IsNullOrWhiteSpace(mascotaDto.NecesidadesEspeciales) ||
                !string.IsNullOrWhiteSpace(mascotaDto.ObservacionesSalud);

            if (tieneInformacionSalud)
            {
                if (mascota.InformacionSalud == null)
                {
                    mascota.InformacionSalud = new InformacionSaludMascota
                    {
                        MascotaId = mascota.Id
                    };
                }

                mascota.InformacionSalud.Enfermedades = mascotaDto.Enfermedades;
                mascota.InformacionSalud.Discapacidades = mascotaDto.Discapacidades;
                mascota.InformacionSalud.Tratamientos = mascotaDto.Tratamientos;
                mascota.InformacionSalud.NecesidadesEspeciales = mascotaDto.NecesidadesEspeciales;
                mascota.InformacionSalud.Observaciones = mascotaDto.ObservacionesSalud;
            }
            else if (mascota.InformacionSalud != null)
            {
                _context.InformacionSaludMascotas.Remove(mascota.InformacionSalud);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "Mascota actualizada correctamente.",
                Id = mascota.Id
            });
        }

        // PUT: api/mascotas/5/desactivar
        [HttpPut("{id:long}/desactivar")]
        public async Task<IActionResult> DesactivarMascota(long id)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var mascota = await _context.Mascotas
                .Include(m => m.DispositivosGps)
                .FirstOrDefaultAsync(m => m.Id == id &&
                                          m.UsuarioId == usuarioId &&
                                          m.EstaActiva);

            if (mascota == null)
            {
                return NotFound(new { Mensaje = "La mascota no existe o no pertenece al usuario autenticado." });
            }

            bool tieneReporteActivo = await _context.Reportes
                .AnyAsync(r => r.MascotaId == id &&
                               r.UsuarioId == usuarioId &&
                               r.EstadoReporteId == 1 &&
                               r.Visible);

            if (tieneReporteActivo)
            {
                return BadRequest(new
                {
                    Mensaje = "No puedes desactivar esta mascota porque tiene un reporte activo. Primero resuelve o suspende el reporte."
                });
            }

            mascota.EstaActiva = false;

            foreach (var dispositivo in mascota.DispositivosGps)
            {
                dispositivo.Activo = false;
                dispositivo.EstadoConexion = "Desconectado";
            }

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Mascota desactivada correctamente." });
        }

        // PUT: api/mascotas/5/reactivar
        [HttpPut("{id:long}/reactivar")]
        public async Task<IActionResult> ReactivarMascota(long id)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario desde el token." });
            }

            var mascota = await _context.Mascotas
                .FirstOrDefaultAsync(m => m.Id == id &&
                                          m.UsuarioId == usuarioId &&
                                          !m.EstaActiva);

            if (mascota == null)
            {
                return NotFound(new { Mensaje = "La mascota no existe, no pertenece al usuario o ya está activa." });
            }

            mascota.EstaActiva = true;

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Mascota reactivada correctamente." });
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