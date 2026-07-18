using GymTracker.DTOs;
using GymTracker.Services.IA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace GymTracker.Controllers.Api
{
    // Endpoint del Chatbot con contexto (Integración 4, ADR-07). Requiere
    // autenticación porque opera sobre datos personales y dispara llamadas (de
    // pago) a la IA. La política de rate limiting "chat" (definida en Program.cs)
    // acota las peticiones por usuario: un guardarriel determinista extra frente
    // a bucles o abuso.
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    [EnableRateLimiting("chat")]
    public class ChatApiController(ChatService chatService) : ControllerBase
    {
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Cuerpo del POST: el texto del usuario.
        public record MensajeRequest(string Mensaje);

        // POST /api/chat/mensaje — envía un mensaje y devuelve la respuesta.
        [HttpPost("mensaje")]
        public async Task<ActionResult<ChatRespuestaDto>> Enviar([FromBody] MensajeRequest req)
        {
            var usuarioId = ObtenerUsuarioId();

            try
            {
                var respuesta = await chatService.EnviarMensajeAsync(usuarioId, req.Mensaje);
                return Ok(respuesta);
            }
            catch (Exception)
            {
                // Todos los proveedores de IA fallaron (red, API caída, parseo).
                return StatusCode(503, new
                {
                    error = "El asistente no está disponible en este momento. Intenta de nuevo en unos segundos."
                });
            }
        }

        // GET /api/chat/historial — historial para pintar el widget al abrirlo.
        [HttpGet("historial")]
        public async Task<ActionResult<List<ChatMensajeDto>>> Historial()
        {
            var usuarioId = ObtenerUsuarioId();
            var mensajes = await chatService.ObtenerHistorialAsync(usuarioId);
            return Ok(mensajes);
        }

        // DELETE /api/chat/historial — limpia la conversación del usuario.
        [HttpDelete("historial")]
        public async Task<IActionResult> Limpiar()
        {
            var usuarioId = ObtenerUsuarioId();
            await chatService.LimpiarHistorialAsync(usuarioId);
            return NoContent();
        }
    }
}
