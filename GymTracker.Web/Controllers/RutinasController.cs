using GymTracker.Application.Services.Rutinas;
using GymTracker.Models;
using GymTracker.Models.ViewModels;
using GymTracker.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class RutinasController(
        IRutinaService rutinas,
        CatalogoService catalogo) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: listado de rutinas del usuario =====
        public async Task<IActionResult> Index()
        {
            var lista = await rutinas.ListarAsync(ObtenerUsuarioId());
            return View(lista);
        }

        // ===== Detalle: muestra la rutina con sus ejercicios =====
        public async Task<IActionResult> Detalle(int id)
        {
            var rutina = await rutinas.ObtenerConEjerciciosAsync(id, ObtenerUsuarioId());
            if (rutina == null) return NotFound();

            // Resolver el GIF de cada ejercicio vinculado (para el modal de animación).
            ViewBag.Gifs = catalogo.ResolverGifs(
                rutina.Ejercicios.Select(re => (re.EjercicioId, re.Ejercicio.ExerciseDbId)));

            return View(rutina);
        }

        // ===== Agregar GET =====
        public async Task<IActionResult> Agregar()
        {
            ViewBag.EjerciciosDisponibles =
                await rutinas.ListarEjerciciosDisponiblesAsync(ObtenerUsuarioId());
            return View();
        }

        // ===== Agregar POST (recibe JSON desde JavaScript) =====
        [HttpPost]
        public async Task<IActionResult> Agregar([FromBody] CrearRutinaViewModel modelo)
        {
            var usuarioId = ObtenerUsuarioId();

            // Validación 1: nombre obligatorio
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
            {
                return BadRequest("El nombre de la rutina es obligatorio.");
            }

            // Validación 2: todos los EjercicioId deben pertenecer al usuario actual
            var ok = await rutinas.EjerciciosPertenecenAlUsuarioAsync(
                usuarioId, modelo.Ejercicios.Select(e => e.EjercicioId));
            if (!ok)
            {
                return BadRequest("Uno o más ejercicios no son válidos o no te pertenecen.");
            }

            // Crear la entidad Rutina con sus ejercicios asignados.
            var rutina = new Rutina
            {
                Nombre = modelo.Nombre.Trim(),
                Descripcion = modelo.Descripcion?.Trim(),
                UsuarioId = usuarioId,
                FechaCreacion = DateTime.UtcNow
            };

            int orden = 1;
            foreach (var ej in modelo.Ejercicios)
            {
                rutina.Ejercicios.Add(new RutinaEjercicio
                {
                    EjercicioId = ej.EjercicioId,
                    SeriesObjetivo = ej.SeriesObjetivo,
                    RepeticionesObjetivo = ej.RepeticionesObjetivo,
                    PesoObjetivo = ej.PesoObjetivo,
                    Orden = orden++
                });
            }

            var rutinaId = await rutinas.CrearAsync(rutina);
            return Ok(new { rutinaId });
        }

        // ===== Editar GET =====
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var rutina = await rutinas.ObtenerConEjerciciosAsync(id, usuarioId);
            if (rutina == null) return NotFound();

            ViewBag.EjerciciosDisponibles =
                await rutinas.ListarEjerciciosDisponiblesAsync(usuarioId);

            // Mapear la entidad a un ViewModel con sus ejercicios ya cargados,
            // para que el JavaScript de la vista pueda pre-llenar la tabla.
            var modelo = new EditarRutinaViewModel
            {
                Id = rutina.Id,
                Nombre = rutina.Nombre,
                Descripcion = rutina.Descripcion,
                Ejercicios = rutina.Ejercicios
                    .OrderBy(re => re.Orden)
                    .Select(re => new EjercicioEnRutinaViewModel
                    {
                        EjercicioId = re.EjercicioId,
                        NombreEjercicio = re.Ejercicio.Nombre,
                        GrupoMuscular = re.Ejercicio.GrupoMuscular.ToString(),
                        SeriesObjetivo = re.SeriesObjetivo,
                        RepeticionesObjetivo = re.RepeticionesObjetivo,
                        PesoObjetivo = re.PesoObjetivo
                    })
                    .ToList()
            };

            return View(modelo);
        }

        // ===== Editar POST (recibe JSON desde JavaScript) =====
        [HttpPost]
        public async Task<IActionResult> Editar(int id, [FromBody] EditarRutinaViewModel modelo)
        {
            var usuarioId = ObtenerUsuarioId();

            // Validación 1: nombre obligatorio
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
            {
                return BadRequest("El nombre de la rutina es obligatorio.");
            }

            // Validación 2: todos los EjercicioId deben pertenecer al usuario actual
            var pertenecen = await rutinas.EjerciciosPertenecenAlUsuarioAsync(
                usuarioId, modelo.Ejercicios.Select(e => e.EjercicioId));
            if (!pertenecen)
            {
                return BadRequest("Uno o más ejercicios no son válidos o no te pertenecen.");
            }

            // Construir los RutinaEjercicio nuevos desde el modelo (orden 1..N).
            int orden = 1;
            var ejercicios = modelo.Ejercicios
                .Select(ej => new RutinaEjercicio
                {
                    EjercicioId = ej.EjercicioId,
                    SeriesObjetivo = ej.SeriesObjetivo,
                    RepeticionesObjetivo = ej.RepeticionesObjetivo,
                    PesoObjetivo = ej.PesoObjetivo,
                    Orden = orden++
                })
                .ToList();

            var ok = await rutinas.ActualizarAsync(
                id, usuarioId, modelo.Nombre.Trim(), modelo.Descripcion?.Trim(), ejercicios);
            if (!ok) return NotFound();

            return Ok(new { rutinaId = id });
        }

        // ===== Eliminar GET (confirmación) =====
        public async Task<IActionResult> Eliminar(int id)
        {
            var rutina = await rutinas.ObtenerParaEliminarAsync(id, ObtenerUsuarioId());
            return rutina == null ? NotFound() : View(rutina);
        }

        // ===== Eliminar POST =====
        [HttpPost, ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var ok = await rutinas.EliminarAsync(id, ObtenerUsuarioId());
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }
    }
}
