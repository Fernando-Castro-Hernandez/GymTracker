using GymTracker.Services.IA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers.Api
{
    // Endpoint del Coach IA. Requiere autenticación porque analiza datos
    // personales del usuario y dispara llamadas (de pago) a la IA.
    [ApiController]
    [Route("api/coach")]
    [Authorize]
    public class CoachApiController(CoachService coachService) : ControllerBase
    {
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // POST /api/coach/analizar/{rutinaId}
        // Genera el análisis de la rutina indicada.
        [HttpPost("analizar/{rutinaId}")]
        public async Task<ActionResult<AnalisisRutinaDto>> Analizar(int rutinaId)
        {
            var usuarioId = ObtenerUsuarioId();

            try
            {
                var analisis = await coachService.AnalizarAsync(rutinaId, usuarioId);
                return Ok(analisis);
            }
            catch (InvalidOperationException ex)
            {
                // Rutina no encontrada / sin ejercicios: error de solicitud.
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                // Fallo del proveedor de IA (red, parseo, API caída).
                // En la Fase 2, esto se cubrirá con el fallback a otro proveedor.
                return StatusCode(503, new
                {
                    error = "No se pudo generar el análisis en este momento. Intenta de nuevo."
                });
            }
        }
    }
}