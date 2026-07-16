using GymTracker.Application.Services.Ejercicios;
using GymTracker.DTOs;
using GymTracker.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers.Api
{
    [ApiController]
    [Route("api/ejercicios")]
    public class EjerciciosApiController(IEjercicioService ejercicios) : ControllerBase
    {
        // GET /api/ejercicios            -> lista todos
        // GET /api/ejercicios?grupo=Pecho -> lista filtrada por grupo muscular
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EjercicioDto>>> GetEjercicios(string? grupo)
        {
            GrupoMuscular? grupoFiltro = null;

            // Validar y parsear el filtro opcional por grupo muscular.
            if (!string.IsNullOrEmpty(grupo))
            {
                if (Enum.TryParse<GrupoMuscular>(grupo, ignoreCase: true, out var grupoEnum))
                    grupoFiltro = grupoEnum;
                else
                    return BadRequest($"El grupo muscular '{grupo}' no es válido.");
            }

            var lista = await ejercicios.ListarDtoAsync(grupoFiltro);
            return Ok(lista);
        }

        // GET /api/ejercicios/5 -> uno por id
        [HttpGet("{id}")]
        public async Task<ActionResult<EjercicioDto>> GetEjercicio(int id)
        {
            var ejercicio = await ejercicios.ObtenerDtoAsync(id);
            return ejercicio == null ? NotFound() : Ok(ejercicio);
        }
    }
}
