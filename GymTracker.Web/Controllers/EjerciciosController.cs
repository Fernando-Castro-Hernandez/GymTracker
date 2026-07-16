using GymTracker.Data;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class EjerciciosController(
        ApplicationDbContext context,
        GymTracker.Services.Catalogo.CatalogoService catalogo) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: listado con filtro opcional por grupo muscular =====
        public async Task<IActionResult> Index(string? grupo)
        {
            var usuarioId = ObtenerUsuarioId();

            var consulta = context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId);

            if (!string.IsNullOrEmpty(grupo))
            {
                if (Enum.TryParse<Models.Enums.GrupoMuscular>(grupo, out var grupoEnum))
                {
                    consulta = consulta.Where(e => e.GrupoMuscular == grupoEnum);
                }
            }

            var ejercicios = await consulta
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Grupos = Enum.GetNames(typeof(Models.Enums.GrupoMuscular));
            ViewBag.GrupoActual = grupo;

            return View(ejercicios);
        }

        // ===== Detalle =====
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

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

            context.Ejercicios.Add(ejercicio);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Editar GET =====
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

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
            var usuarioId = ObtenerUsuarioId();

            // Validar que el ejercicio existe y es del usuario
            var original = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if (original == null) return NotFound();

            // Actualizar solo los campos editables
            original.Nombre = ejercicio.Nombre;
            original.GrupoMuscular = ejercicio.GrupoMuscular;
            original.Descripcion = ejercicio.Descripcion;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Eliminar GET (confirmación) =====
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            return ejercicio == null ? NotFound() : View(ejercicio);
        }

        // ===== Eliminar POST =====
        [HttpPost, ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if (ejercicio == null) return NotFound();

            context.Ejercicios.Remove(ejercicio);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Vinculación con el catálogo externo (GIFs) =====

        // Devuelve los ejercicios del usuario en JSON, para el selector del modal
        // "Vincular a mis ejercicios" en la galería "Explorar".
        [HttpGet]
        public async Task<IActionResult> Mios()
        {
            var usuarioId = ObtenerUsuarioId();

            var mios = await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId)
                .OrderBy(e => e.Nombre)
                .Select(e => new
                {
                    id = e.Id,
                    nombre = e.Nombre,
                    grupo = e.GrupoMuscular.ToString(),
                    exerciseDbId = e.ExerciseDbId
                })
                .ToListAsync();

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

            var usuarioId = ObtenerUsuarioId();
            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == ejercicioId && e.UsuarioId == usuarioId);
            if (ejercicio == null)
                return NotFound(new { ok = false, error = "No se encontró tu ejercicio." });

            ejercicio.ExerciseDbId = exerciseDbId;
            await context.SaveChangesAsync();

            return Json(new { ok = true, ejercicioId, nombre = ejercicio.Nombre });
        }

        // Quita la vinculación (el ejercicio queda sin animación).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desvincular(int ejercicioId)
        {
            var usuarioId = ObtenerUsuarioId();
            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == ejercicioId && e.UsuarioId == usuarioId);
            if (ejercicio == null)
                return NotFound(new { ok = false });

            ejercicio.ExerciseDbId = null;
            await context.SaveChangesAsync();

            return Json(new { ok = true, ejercicioId });
        }
    }
}