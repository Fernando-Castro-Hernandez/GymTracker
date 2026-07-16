using GymTracker.Application.Services.Rutinas;
using GymTracker.DTOs;
using GymTracker.Services.Volumen;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers.Api
{
    [ApiController]
    [Route("api/rutinas")]
    public class RutinasApiController(IRutinaService rutinas) : ControllerBase
    {
        // GET /api/rutinas -> lista todas (con sus ejercicios)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RutinaDto>>> GetRutinas()
        {
            var lista = await rutinas.ListarDtoAsync();
            return Ok(lista);
        }

        // GET /api/rutinas/5 -> una por id (con sus ejercicios)
        [HttpGet("{id}")]
        public async Task<ActionResult<RutinaDto>> GetRutina(int id)
        {
            var rutina = await rutinas.ObtenerDtoAsync(id);
            return rutina == null ? NotFound() : Ok(rutina);
        }

        // GET /api/rutinas/5/volumen?tipo=Simple
        // Calcula el volumen de entrenamiento de una rutina usando
        // el patron Strategy (la formula) + Factory Method (crea la estrategia).
        [HttpGet("{id}/volumen")]
        public async Task<ActionResult<VolumenDto>> GetVolumen(int id, TipoVolumen tipo = TipoVolumen.Simple)
        {
            var volumen = await rutinas.CalcularVolumenAsync(id, tipo);
            return volumen == null ? NotFound() : Ok(volumen);
        }
    }
}
