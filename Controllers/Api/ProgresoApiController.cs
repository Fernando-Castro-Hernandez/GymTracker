using GymTracker.DTOs;
using GymTracker.Services.Progreso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers.Api
{
    // A diferencia de los otros API controllers (catálogo público), este expone
    // DATOS PERSONALES del usuario, por lo que requiere autenticación y filtra
    // por UsuarioId. Seguridad contextual: el nivel de protección corresponde
    // a la sensibilidad del dato.
    [ApiController]
    [Route("api/progreso")]
    [Authorize]
    public class ProgresoApiController(ProgresoService progresoService) : ControllerBase
    {
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET /api/progreso/peso
        // Evolución del peso corporal del usuario.
        [HttpGet("peso")]
        public async Task<ActionResult<IEnumerable<PuntoProgresoDto>>> GetPesoCorporal()
        {
            var usuarioId = ObtenerUsuarioId();
            var datos = await progresoService.ObtenerPesoCorporalAsync(usuarioId);
            return Ok(datos);
        }

        // GET /api/progreso/ejercicio/{id}
        // Progresión de carga (peso máximo por día) de un ejercicio.
        [HttpGet("ejercicio/{id}")]
        public async Task<ActionResult<IEnumerable<PuntoProgresoDto>>> GetProgresionEjercicio(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            var datos = await progresoService.ObtenerProgresionEjercicioAsync(usuarioId, id);
            return Ok(datos);
        }

        // GET /api/progreso/volumen
        // Volumen total (tonelaje) por sesión a lo largo del tiempo.
        [HttpGet("volumen")]
        public async Task<ActionResult<IEnumerable<PuntoProgresoDto>>> GetVolumenPorSesion()
        {
            var usuarioId = ObtenerUsuarioId();
            var datos = await progresoService.ObtenerVolumenPorSesionAsync(usuarioId);
            return Ok(datos);
        }
    }
}