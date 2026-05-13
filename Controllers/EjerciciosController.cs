using GymTracker.Data;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class EjerciciosController(ApplicationDbContext context) : Controller
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

            return ejercicio == null ? NotFound() : View(ejercicio);
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
    }
}