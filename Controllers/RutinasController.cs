using GymTracker.Data;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class RutinasController(ApplicationDbContext context) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: listado de rutinas del usuario =====
        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioId();

            var rutinas = await context.Rutinas
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            return View(rutinas);
        }

        // ===== Detalle: muestra la rutina con sus ejercicios =====
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            return rutina == null ? NotFound() : View(rutina);
        }

        // ===== Agregar GET =====
        public IActionResult Agregar()
        {
            return View();
        }

        // ===== Agregar POST =====
        [HttpPost]
        public async Task<IActionResult> Agregar(Rutina rutina)
        {
            rutina.UsuarioId = ObtenerUsuarioId();
            rutina.FechaCreacion = DateTime.UtcNow;

            context.Rutinas.Add(rutina);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Editar GET =====
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var rutina = await context.Rutinas
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            return rutina == null ? NotFound() : View(rutina);
        }

        // ===== Editar POST =====
        [HttpPost]
        public async Task<IActionResult> Editar(int id, Rutina rutina)
        {
            var usuarioId = ObtenerUsuarioId();

            var original = await context.Rutinas
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            if (original == null) return NotFound();

            // Actualizar solo los campos editables
            original.Nombre = rutina.Nombre;
            original.Descripcion = rutina.Descripcion;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Eliminar GET (confirmación) =====
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            return rutina == null ? NotFound() : View(rutina);
        }

        // ===== Eliminar POST =====
        [HttpPost, ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var rutina = await context.Rutinas
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            if (rutina == null) return NotFound();

            // Gracias a OnDelete(Cascade), borrar la rutina elimina automáticamente
            // todos sus RutinaEjercicio. EF Core lo maneja sin que tengamos que hacer nada.
            context.Rutinas.Remove(rutina);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}