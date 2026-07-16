using GymTracker.Application.Services.Ejercicios;
using GymTracker.Models;
using GymTracker.Models.Enums;
using GymTracker.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class EjerciciosController(
        IEjercicioService ejercicios,
        CatalogoService catalogo) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: listado con filtro opcional por grupo muscular =====
        public async Task<IActionResult> Index(string? grupo)
        {
            // Parsear el filtro de la query (presentación) a enum antes de consultar.
            GrupoMuscular? grupoFiltro = null;
            if (!string.IsNullOrEmpty(grupo) && Enum.TryParse<GrupoMuscular>(grupo, out var g))
                grupoFiltro = g;

            var lista = await ejercicios.ListarAsync(ObtenerUsuarioId(), grupoFiltro);

            ViewBag.Grupos = Enum.GetNames(typeof(GrupoMuscular));
            ViewBag.GrupoActual = grupo;

            return View(lista);
        }

        // ===== Detalle =====
        public async Task<IActionResult> Detalle(int id)
        {
            var ejercicio = await ejercicios.ObtenerAsync(id, ObtenerUsuarioId());
            return ejercicio == null ? NotFound() : View(ejercicio);
        }

        // ===== Agregar GET =====
        public IActionResult Agregar()
        {
            return View();
        }

        // ===== Agregar POST =====
        [HttpPost]
        public async Task<IActionResult> Agregar(Ejercicio ejercicio)
        {
            ejercicio.UsuarioId = ObtenerUsuarioId();
            await ejercicios.CrearAsync(ejercicio);

            return RedirectToAction(nameof(Index));
        }

        // ===== Editar GET =====
        public async Task<IActionResult> Editar(int id)
        {
            var ejercicio = await ejercicios.ObtenerAsync(id, ObtenerUsuarioId());
            if (ejercicio == null) return NotFound();

            // Si está vinculado a un ejercicio del catálogo, resolver su nombre y GIF
            // (desde el seed) para mostrar el estado de la vinculación en la vista.
            if (!string.IsNullOrEmpty(ejercicio.ExerciseDbId))
            {
                var vinculo = await catalogo.ObtenerDetalleAsync(ejercicio.ExerciseDbId);
                ViewBag.VinculoNombre = vinculo?.Name;
                ViewBag.VinculoGif = vinculo?.GifUrl;
            }

            return View(ejercicio);
        }

        // ===== Editar POST =====
        [HttpPost]
        public async Task<IActionResult> Editar(int id, Ejercicio ejercicio)
        {
            var ok = await ejercicios.ActualizarAsync(id, ObtenerUsuarioId(), ejercicio);
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        // ===== Eliminar GET (confirmación) =====
        public async Task<IActionResult> Eliminar(int id)
        {
            var ejercicio = await ejercicios.ObtenerAsync(id, ObtenerUsuarioId());
            return ejercicio == null ? NotFound() : View(ejercicio);
        }

        // ===== Eliminar POST =====
        [HttpPost, ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var ok = await ejercicios.EliminarAsync(id, ObtenerUsuarioId());
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        // ===== Vinculación con el catálogo externo (GIFs) =====

        // Devuelve los ejercicios del usuario en JSON, para el selector del modal
        // "Vincular a mis ejercicios" en la galería "Explorar".
        [HttpGet]
        public async Task<IActionResult> Mios()
        {
            var lista = await ejercicios.ListarParaSelectorAsync(ObtenerUsuarioId());

            var mios = lista.Select(e => new
            {
                id = e.Id,
                nombre = e.Nombre,
                grupo = e.GrupoMuscular.ToString(),
                exerciseDbId = e.ExerciseDbId
            });

            return Json(mios);
        }

        // Vincula un ejercicio del usuario con uno del catálogo: guarda su
        // ExerciseDbId (el Id estable, no la URL). Filtra por UsuarioId (ownership).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vincular(int ejercicioId, string exerciseDbId)
        {
            if (string.IsNullOrWhiteSpace(exerciseDbId))
                return BadRequest(new { ok = false, error = "Falta el ejercicio del catálogo." });

            // Validar que el Id realmente exista en el catálogo (seed).
            var delCatalogo = await catalogo.ObtenerDetalleAsync(exerciseDbId);
            if (delCatalogo == null)
                return NotFound(new { ok = false, error = "Ese ejercicio del catálogo no existe." });

            var ejercicio = await ejercicios.FijarExerciseDbIdAsync(ejercicioId, ObtenerUsuarioId(), exerciseDbId);
            if (ejercicio == null)
                return NotFound(new { ok = false, error = "No se encontró tu ejercicio." });

            return Json(new { ok = true, ejercicioId, nombre = ejercicio.Nombre });
        }

        // Quita la vinculación (el ejercicio queda sin animación).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desvincular(int ejercicioId)
        {
            var ejercicio = await ejercicios.FijarExerciseDbIdAsync(ejercicioId, ObtenerUsuarioId(), null);
            if (ejercicio == null) return NotFound(new { ok = false });

            return Json(new { ok = true, ejercicioId });
        }
    }
}
