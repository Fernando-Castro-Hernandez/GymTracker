using GymTracker.Data;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GymTracker.Models.ViewModels;

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
        public async Task<IActionResult> Agregar()
        {
            var usuarioId = ObtenerUsuarioId();

            // Cargar los ejercicios disponibles del usuario para el dropdown
            var ejerciciosDisponibles = await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.EjerciciosDisponibles = ejerciciosDisponibles;
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
            if (modelo.Ejercicios.Any())
            {
                var idsEnviados = modelo.Ejercicios.Select(e => e.EjercicioId).Distinct().ToList();

                var idsValidos = await context.Ejercicios
                    .Where(e => e.UsuarioId == usuarioId && idsEnviados.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToListAsync();

                if (idsValidos.Count != idsEnviados.Count)
                {
                    return BadRequest("Uno o más ejercicios no son válidos o no te pertenecen.");
                }
            }

            // Crear la entidad Rutina
            var rutina = new Rutina
            {
                Nombre = modelo.Nombre.Trim(),
                Descripcion = modelo.Descripcion?.Trim(),
                UsuarioId = usuarioId,
                FechaCreacion = DateTime.UtcNow
            };

            // Agregar los ejercicios asignados (si los hay)
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

            // Guardar todo en una sola transacción
            context.Rutinas.Add(rutina);
            await context.SaveChangesAsync();

            return Ok(new { rutinaId = rutina.Id });
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