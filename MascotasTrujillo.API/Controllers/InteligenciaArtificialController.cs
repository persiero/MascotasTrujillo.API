using MascotasTrujillo.API.Data;
using MascotasTrujillo.API.DTOs;
using MascotasTrujillo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MascotasTrujillo.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InteligenciaArtificialController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly InteligenciaArtificialService
            _inteligenciaArtificialService;

        private readonly ILogger<InteligenciaArtificialController>
            _logger;

        private const long MaxBytes = 5 * 1024 * 1024;

        public InteligenciaArtificialController(
            ApplicationDbContext context,
            InteligenciaArtificialService inteligenciaArtificialService,
            ILogger<InteligenciaArtificialController> logger)
        {
            _context = context;

            _inteligenciaArtificialService =
                inteligenciaArtificialService;

            _logger = logger;
        }

        // =====================================================
        // COMPARAR FOTO CON REPORTE DE MASCOTA PERDIDA
        // =====================================================

        [HttpPost("comparar-reporte/{reporteId:long}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(
            MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> CompararReporte(
            long reporteId,
            [FromForm] CompararMascotaDTO dto,
            CancellationToken cancellationToken)
        {
            // 1. Validar usuario autenticado
            var usuarioIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!long.TryParse(
                    usuarioIdClaim,
                    out var usuarioId))
            {
                return Unauthorized(new
                {
                    Mensaje = "No se pudo identificar al usuario."
                });
            }

            // 2. Validar fotografía encontrada
            if (dto.FotoEncontrada == null ||
                dto.FotoEncontrada.Length == 0)
            {
                return BadRequest(new
                {
                    Mensaje = "Debes seleccionar una fotografía."
                });
            }

            if (dto.FotoEncontrada.Length > MaxBytes)
            {
                return BadRequest(new
                {
                    Mensaje = "La fotografía no debe superar los 5 MB."
                });
            }

            // 3. Consultar reporte activo
            var reporte = await _context.Reportes
                .AsNoTracking()
                .Where(r =>
                    r.Id == reporteId &&
                    r.Visible &&
                    r.TipoReporteId == 1 &&
                    r.EstadoReporteId == 1)
                .Select(r => new
                {
                    r.Id,
                    r.Titulo,
                    r.NombreMascotaReferencial,
                    r.EspecieReferencial,

                    FotoUrl = r.Fotos
                        .OrderByDescending(f => f.FechaRegistro)
                        .Select(f => f.UrlFoto)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (reporte == null)
            {
                return NotFound(new
                {
                    Mensaje =
                        "No se encontró un reporte activo " +
                        "de mascota perdida."
                });
            }

            // 4. Verificar fotografía original
            if (string.IsNullOrWhiteSpace(reporte.FotoUrl))
            {
                return BadRequest(new
                {
                    Mensaje =
                        "El reporte no tiene una fotografía " +
                        "original para comparar."
                });
            }

            try
            {
                // 5. Solicitar análisis de inteligencia artificial
                var resultado =
                    await _inteligenciaArtificialService.CompararAsync(
                        reporte.FotoUrl,
                        dto.FotoEncontrada,
                        cancellationToken
                    );

                // 6. Devolver resultado a la aplicación
                return Ok(new
                {
                    reporteId = reporte.Id,

                    tituloReporte = reporte.Titulo,

                    nombreMascota =
                        reporte.NombreMascotaReferencial,

                    especie = reporte.EspecieReferencial,

                    fotoOriginalUrl = reporte.FotoUrl,

                    resultado.Exito,

                    resultado.PorcentajeSimilitud,

                    resultado.NivelSimilitud,

                    resultado.Titulo,

                    resultado.Descripcion,

                    resultado.Modelo,

                    resultado.Metodo,

                    resultado.Advertencia
                });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    Mensaje = ex.Message
                });
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504, new
                {
                    Mensaje =
                        "El análisis tardó demasiado. " +
                        "Inténtalo nuevamente."
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error de comunicación durante el análisis."
                );

                return StatusCode(503, new
                {
                    Mensaje =
                        "El servicio de inteligencia artificial " +
                        "o de fotografías no está disponible."
                });
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error al comparar el reporte {ReporteId}",
                    reporteId
                );

                return StatusCode(500, new
                {
                    Mensaje =
                        "Ocurrió un problema durante " +
                        "el análisis de las fotografías."
                });
            }
        }
    }
}
