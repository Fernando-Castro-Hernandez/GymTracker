using GymTracker.Data;
using GymTracker.DTOs;
using GymTracker.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Controllers.Api
{
    [ApiController]
    [Route("api/ejercicios")]
    public class EjerciciosApiController(ApplicationDbContext context) : ControllerBase
    {
        // GET /api/ejercicios            -> lista todos
        // GET /api/ejercicios?grupo=Pecho -> lista filtrada por grupo muscular
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EjercicioDto>>> GetEjercicios(string? grupo)
        {
            var consulta = context.Ejercicios.AsQueryable();

            // Filtro opcional por grupo muscular
            if (!string.IsNullOrEmpty(grupo))
            {
                if (Enum.TryParse<GrupoMuscular>(grupo, ignoreCase: true, out var grupoEnum))
                {
                    consulta = consulta.Where(e => e.GrupoMuscular == grupoEnum);
                }
                else
                {
                    return BadRequest($"El grupo muscular '{grupo}' no es válido.");
                }
            }

            var ejercicios = await consulta
                .OrderBy(e => e.Nombre)
                .Select(e => new EjercicioDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    GrupoMuscular = e.GrupoMuscular.ToString(),
                    Descripcion = e.Descripcion
                })
                .ToListAsync();

            return Ok(ejercicios);
        }

        // GET /api/ejercicios/5 -> uno por id
        [HttpGet("{id}")]
        public async Task<ActionResult<EjercicioDto>> GetEjercicio(int id)
        {
            var ejercicio = await context.Ejercicios
                .Where(e => e.Id == id)
                .Select(e => new EjercicioDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    GrupoMuscular = e.GrupoMuscular.ToString(),
                    Descripcion = e.Descripcion
                })
                .FirstOrDefaultAsync();

            if (ejercicio == null)
            {
                return NotFound();
            }

            return Ok(ejercicio);
        }
    }
}