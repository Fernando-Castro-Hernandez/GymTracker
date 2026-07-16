using GymTracker.Application.Services.Sesiones;
using GymTracker.Models.ViewModels;
using GymTracker.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class SesionesController(
        ISesionService sesiones,
        CatalogoService catalogo) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: historial de sesiones del usuario =====
        public async Task<IActionResult> Index()
        {
            var lista = await sesiones.ListarAsync(ObtenerUsuarioId());
            return View(lista);
        }

        // ===== Iniciar POST: crea una sesión congelando la rutina del momento =====
        // Se llama desde el Detalle de una rutina con un botón "Iniciar entrenamiento".
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar(int rutinaId)
        {
            var sesionId = await sesiones.IniciarDesdeRutinaAsync(rutinaId, ObtenerUsuarioId());
            if (sesionId == null) return NotFound();

            // Ir directo a registrar la sesión recién creada.
            return RedirectToAction(nameof(Registrar), new { id = sesionId.Value });
        }

        // ===== Registrar GET: pantalla para capturar reps y peso reales =====
        public async Task<IActionResult> Registrar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var sesion = await sesiones.ObtenerConSeriesAsync(id, usuarioId);
            if (sesion == null) return NotFound();

            // Resolver el GIF de cada ejercicio EN VIVO desde el ejercicio actual
            // (el snapshot guarda EjercicioId; el GIF es ayuda visual, no historial).
            var ejercicioIds = sesion.Series.Select(s => s.EjercicioId).Distinct();
            var vinculos = await sesiones.ObtenerVinculosGifAsync(usuarioId, ejercicioIds);
            var gifs = catalogo.ResolverGifs(vinculos);

            // Mapear la entidad a un ViewModel que solo expone lo editable.
            var modelo = new RegistrarSesionViewModel
            {
                SesionId = sesion.Id,
                NombreRutina = sesion.NombreRutina,
                Fecha = sesion.Fecha,
                Notas = sesion.Notas,
                Series = sesion.Series
                    .OrderBy(s => s.EjercicioId)
                    .ThenBy(s => s.NumeroSerie)
                    .Select(s => new SerieEditableViewModel
                    {
                        Id = s.Id,
                        NombreEjercicio = s.NombreEjercicio,
                        GrupoMuscular = s.GrupoMuscular.ToString(),
                        NumeroSerie = s.NumeroSerie,
                        RepeticionesObjetivo = s.RepeticionesObjetivo,
                        PesoObjetivo = s.PesoObjetivo,
                        RepeticionesReales = s.RepeticionesReales,
                        PesoReal = s.PesoReal,
                        GifUrl = gifs.TryGetValue(s.EjercicioId, out var g) ? g : null
                    })
                    .ToList()
            };

            return View(modelo);
        }

        // ===== Registrar POST: guarda los valores reales capturados =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarSesionViewModel modelo)
        {
            // Convertir las series editadas del formulario en un mapa
            // serieId -> (repsReales, pesoReal) que el servicio aplica sobre la
            // sesión (solo toca series que pertenezcan a esa sesión/usuario).
            var valores = modelo.Series.ToDictionary(
                s => s.Id,
                s => (s.RepeticionesReales, s.PesoReal));

            var ok = await sesiones.GuardarRealesAsync(
                modelo.SesionId, ObtenerUsuarioId(), modelo.Notas, valores);
            if (!ok) return NotFound();

            // Ir al detalle de la sesión (solo lectura) para ver el resumen.
            return RedirectToAction(nameof(Detalle), new { id = modelo.SesionId });
        }

        // ===== Detalle GET: ver una sesión pasada (solo lectura) =====
        public async Task<IActionResult> Detalle(int id)
        {
            var sesion = await sesiones.ObtenerConSeriesAsync(id, ObtenerUsuarioId());
            if (sesion == null) return NotFound();

            sesion.Series = sesion.Series
                .OrderBy(s => s.EjercicioId)
                .ThenBy(s => s.NumeroSerie)
                .ToList();

            return View(sesion);
        }

        // ===== Eliminar POST: borra una sesión (y sus series en cascada) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var ok = await sesiones.EliminarAsync(id, ObtenerUsuarioId());
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }
    }
}
