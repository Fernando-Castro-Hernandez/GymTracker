using GymTracker.Application.Services.Mediciones;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class MedicionesController(IMedicionService mediciones) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: historial de mediciones, más reciente primero =====
        public async Task<IActionResult> Index()
        {
            var lista = await mediciones.ListarAsync(ObtenerUsuarioId());
            return View(lista);
        }

        // ===== Detalle: ver una medición completa =====
        public async Task<IActionResult> Detalle(int id)
        {
            var medicion = await mediciones.ObtenerAsync(id, ObtenerUsuarioId());
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
            // Validación mínima: el peso debe ser positivo.
            if (medicion.Peso <= 0)
            {
                ModelState.AddModelError(nameof(Medicion.Peso), "El peso debe ser mayor que cero.");
            }

            if (!ModelState.IsValid) return View(medicion);

            // Asignar propietario. El resto de campos vienen del formulario;
            // los que el usuario dejó vacíos llegan como null (opcionales).
            medicion.UsuarioId = ObtenerUsuarioId();

            // Normalizar la fecha: si no se especificó, ahora (UTC); si vino del
            // input datetime-local (hora local sin Kind), convertir a UTC.
            medicion.Fecha = medicion.Fecha == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(medicion.Fecha, DateTimeKind.Local).ToUniversalTime();

            await mediciones.CrearAsync(medicion);

            return RedirectToAction(nameof(Index));
        }

        // ===== Editar GET =====
        public async Task<IActionResult> Editar(int id)
        {
            var medicion = await mediciones.ObtenerAsync(id, ObtenerUsuarioId());
            if (medicion == null) return NotFound();

            return View(medicion);
        }

        // ===== Editar POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Medicion medicion)
        {
            if (medicion.Peso <= 0)
            {
                ModelState.AddModelError(nameof(Medicion.Peso), "El peso debe ser mayor que cero.");
            }

            if (!ModelState.IsValid) return View(medicion);

            // Normalizar la fecha del formulario a UTC antes de pasarla al servicio.
            // (default => el servicio conserva la fecha original.)
            if (medicion.Fecha != default)
            {
                medicion.Fecha = DateTime.SpecifyKind(medicion.Fecha, DateTimeKind.Local).ToUniversalTime();
            }

            var ok = await mediciones.ActualizarAsync(id, ObtenerUsuarioId(), medicion);
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        // ===== Eliminar POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var ok = await mediciones.EliminarAsync(id, ObtenerUsuarioId());
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }
    }
}
