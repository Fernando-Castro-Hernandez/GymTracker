using GymTracker.Data;
using GymTracker.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymTracker.Services.Volumen;

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

        // GET /api/rutinas/5/volumen?tipo=Simple
        // Calcula el volumen de entrenamiento de una rutina usando
        // el patron Strategy (la formula) + Factory Method (crea la estrategia).
        [HttpGet("{id}/volumen")]
        public async Task<ActionResult<VolumenDto>> GetVolumen(int id, TipoVolumen tipo = TipoVolumen.Simple)
        {
            // Traemos la rutina con sus ejercicios (incluyendo el Ejercicio,
            // que la estrategia Relativa necesita para leer el grupo muscular)
            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rutina == null)
            {
                return NotFound();
            }

            // Factory Method: crea la estrategia adecuada segun el tipo pedido
            var factory = new CalculoVolumenFactory();
            ICalculoVolumen estrategia = factory.Crear(tipo);

            // Strategy: ejecuta la formula correspondiente
            double resultado = estrategia.Calcular(rutina.Ejercicios);

            return Ok(new VolumenDto
            {
                RutinaId = rutina.Id,
                NombreRutina = rutina.Nombre,
                TipoCalculo = estrategia.Nombre,
                Volumen = resultado
            });
        }
    }
}