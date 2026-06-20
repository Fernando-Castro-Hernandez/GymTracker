using GymTracker.Data;
using GymTracker.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Controllers.Api
{
    [ApiController]
    [Route("api/rutinas")]
    public class RutinasApiController(ApplicationDbContext context) : ControllerBase
    {
        // GET /api/rutinas -> lista todas (con sus ejercicios)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RutinaDto>>> GetRutinas()
        {
            var rutinas = await context.Rutinas
                .OrderByDescending(r => r.FechaCreacion)
                .Select(r => new RutinaDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    FechaCreacion = r.FechaCreacion,
                    Ejercicios = r.Ejercicios
                        .OrderBy(re => re.Orden)
                        .Select(re => new RutinaEjercicioDto
                        {
                            EjercicioId = re.EjercicioId,
                            NombreEjercicio = re.Ejercicio.Nombre,
                            GrupoMuscular = re.Ejercicio.GrupoMuscular.ToString(),
                            SeriesObjetivo = re.SeriesObjetivo,
                            RepeticionesObjetivo = re.RepeticionesObjetivo,
                            PesoObjetivo = re.PesoObjetivo,
                            Orden = re.Orden
                        }).ToList()
                })
                .ToListAsync();

            return Ok(rutinas);
        }

        // GET /api/rutinas/5 -> una por id (con sus ejercicios)
        [HttpGet("{id}")]
        public async Task<ActionResult<RutinaDto>> GetRutina(int id)
        {
            var rutina = await context.Rutinas
                .Where(r => r.Id == id)
                .Select(r => new RutinaDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    FechaCreacion = r.FechaCreacion,
                    Ejercicios = r.Ejercicios
                        .OrderBy(re => re.Orden)
                        .Select(re => new RutinaEjercicioDto
                        {
                            EjercicioId = re.EjercicioId,
                            NombreEjercicio = re.Ejercicio.Nombre,
                            GrupoMuscular = re.Ejercicio.GrupoMuscular.ToString(),
                            SeriesObjetivo = re.SeriesObjetivo,
                            RepeticionesObjetivo = re.RepeticionesObjetivo,
                            PesoObjetivo = re.PesoObjetivo,
                            Orden = re.Orden
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (rutina == null)
            {
                return NotFound();
            }

            return Ok(rutina);
        }
    }
}