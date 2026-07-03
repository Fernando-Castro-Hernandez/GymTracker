using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class SesionesController(ApplicationDbContext context) : Controller
    {
        // ===== Helper: obtener el Id del usuario logueado =====
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ===== Index: historial de sesiones del usuario =====
        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioId();

            var sesiones = await context.Sesiones
                .Include(s => s.Series)
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.Fecha)
                .ToListAsync();

            return View(sesiones);
        }

        // ===== Iniciar POST: crea una sesión congelando la rutina del momento =====
        // Se llama desde el Detalle de una rutina con un botón "Iniciar entrenamiento".
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar(int rutinaId)
        {
            var usuarioId = ObtenerUsuarioId();

            // Cargar la rutina CON sus ejercicios y el Ejercicio relacionado,
            // para poder copiar (congelar) todos los datos del momento.
            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == rutinaId && r.UsuarioId == usuarioId);

            if (rutina == null) return NotFound();

            // Crear la sesión, congelando el nombre de la rutina.
            var sesion = new Sesion
            {
                UsuarioId = usuarioId,
                RutinaId = rutina.Id,
                NombreRutina = rutina.Nombre,
                Fecha = DateTime.UtcNow
            };

            // Por cada ejercicio de la rutina, y por cada serie objetivo,
            // crear una SerieRealizada pre-cargada con la meta como valor inicial
            // (Opción A: los campos reales arrancan igualando la meta).
            foreach (var re in rutina.Ejercicios.OrderBy(re => re.Orden))
            {
                for (int numero = 1; numero <= re.SeriesObjetivo; numero++)
                {
                    sesion.Series.Add(new SerieRealizada
                    {
                        EjercicioId = re.EjercicioId,
                        NombreEjercicio = re.Ejercicio.Nombre,
                        GrupoMuscular = re.Ejercicio.GrupoMuscular,
                        NumeroSerie = numero,
                        RepeticionesObjetivo = re.RepeticionesObjetivo,
                        PesoObjetivo = re.PesoObjetivo,
                        // Valores reales pre-cargados con la meta (el usuario ajusta).
                        RepeticionesReales = re.RepeticionesObjetivo,
                        PesoReal = re.PesoObjetivo
                    });
                }
            }

            context.Sesiones.Add(sesion);
            await context.SaveChangesAsync();

            // Ir directo a registrar la sesión recién creada.
            return RedirectToAction(nameof(Registrar), new { id = sesion.Id });
        }

        // ===== Registrar GET: pantalla para capturar reps y peso reales =====
        public async Task<IActionResult> Registrar(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var sesion = await context.Sesiones
                .Include(s => s.Series)
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

            if (sesion == null) return NotFound();

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
                        PesoReal = s.PesoReal
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
            var usuarioId = ObtenerUsuarioId();

            // Cargar la sesión real CON sus series (rastreadas por EF Core).
            var sesion = await context.Sesiones
                .Include(s => s.Series)
                .FirstOrDefaultAsync(s => s.Id == modelo.SesionId && s.UsuarioId == usuarioId);

            if (sesion == null) return NotFound();

            // Guardar las notas de la sesión.
            sesion.Notas = modelo.Notas;

            // Actualizar SOLO los valores reales de cada serie.
            // Se busca cada serie por su Id dentro de las series de ESTA sesión,
            // de modo que no se pueda tocar una serie de otra sesión/usuario.
            foreach (var serieEditada in modelo.Series)
            {
                var serie = sesion.Series.FirstOrDefault(s => s.Id == serieEditada.Id);
                if (serie == null) continue; // ignora ids que no pertenezcan a la sesión

                serie.RepeticionesReales = serieEditada.RepeticionesReales;
                serie.PesoReal = serieEditada.PesoReal;
            }

            await context.SaveChangesAsync();

            // Ir al detalle de la sesión (solo lectura) para ver el resumen.
            return RedirectToAction(nameof(Detalle), new { id = sesion.Id });
        }

        // ===== Detalle GET: ver una sesión pasada (solo lectura) =====
        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = ObtenerUsuarioId();

            var sesion = await context.Sesiones
                .Include(s => s.Series)
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

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
            var usuarioId = ObtenerUsuarioId();

            var sesion = await context.Sesiones
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

            if (sesion == null) return NotFound();

            context.Sesiones.Remove(sesion);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}