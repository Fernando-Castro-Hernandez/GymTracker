using GymTracker.Data;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class MedicionesController(ApplicationDbContext context) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: historial de mediciones, más reciente primero =====
        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioId();

            var mediciones = await context.Mediciones
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return View(mediciones);
        }

        // ===== Detalle: ver una medición completa =====
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var medicion = await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

            if (medicion == null) return NotFound();

            return View(medicion);
        }

        // ===== Crear GET =====
        public IActionResult Crear() => View();

        // ===== Crear POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Medicion medicion)
        {
            var usuarioId = ObtenerUsuarioId();

            // Validación mínima: el peso debe ser positivo.
            if (medicion.Peso <= 0)
            {
                ModelState.AddModelError(nameof(Medicion.Peso), "El peso debe ser mayor que cero.");
            }

            if (!ModelState.IsValid) return View(medicion);

            // Asignar propietario y guardar. El resto de campos vienen del formulario;
            // los que el usuario dejó vacíos llegan como null (opcionales).
            medicion.UsuarioId = usuarioId;

            // Si no se especificó fecha, usar ahora (UTC).
            if (medicion.Fecha == default)
            {
                medicion.Fecha = DateTime.UtcNow;
            }
            else
            {
                // La fecha viene del input datetime-local como hora local sin Kind.
                // La interpretamos como local y la convertimos a UTC para PostgreSQL.
                medicion.Fecha = DateTime.SpecifyKind(medicion.Fecha, DateTimeKind.Local).ToUniversalTime();
            }

            context.Mediciones.Add(medicion);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Editar GET =====
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var medicion = await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

            if (medicion == null) return NotFound();

            return View(medicion);
        }

        // ===== Editar POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Medicion medicion)
        {
            var usuarioId = ObtenerUsuarioId();

            // Cargar la medición original (rastreada) validando ownership.
            var original = await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

            if (original == null) return NotFound();

            if (medicion.Peso <= 0)
            {
                ModelState.AddModelError(nameof(Medicion.Peso), "El peso debe ser mayor que cero.");
            }

            if (!ModelState.IsValid) return View(medicion);

            // Copiar solo los campos editables sobre la entidad rastreada.
            // No se toca UsuarioId ni Id: el ownership queda blindado.
            original.Fecha = medicion.Fecha == default
                ? original.Fecha
                : DateTime.SpecifyKind(medicion.Fecha, DateTimeKind.Local).ToUniversalTime();
            original.Peso = medicion.Peso;
            original.PorcentajeGrasa = medicion.PorcentajeGrasa;
            original.GrasaVisceral = medicion.GrasaVisceral;
            original.MasaMuscular = medicion.MasaMuscular;
            original.PorcentajeAgua = medicion.PorcentajeAgua;
            original.Cintura = medicion.Cintura;
            original.Cadera = medicion.Cadera;
            original.Pecho = medicion.Pecho;
            original.Brazo = medicion.Brazo;
            original.Muslo = medicion.Muslo;
            original.Notas = medicion.Notas;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Eliminar POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var medicion = await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

            if (medicion == null) return NotFound();

            context.Mediciones.Remove(medicion);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}