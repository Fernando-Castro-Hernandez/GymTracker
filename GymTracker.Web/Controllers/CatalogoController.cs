using GymTracker.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Controllers
{
    // Sirve la página "Explorar ejercicios" (catálogo externo con GIFs).
    [Authorize]
    public class CatalogoController(CatalogoService catalogoService) : Controller
    {
        // GET /Catalogo
        // Muestra la galería. El filtrado (texto, zona, equipamiento, patrón,
        // sub-músculo) y los contadores dinámicos se hacen en el cliente sobre
        // los datos que entrega /Catalogo/Datos (ver catalogo-explorar.js).
        public IActionResult Index() => View();

        // GET /Catalogo/Datos
        // Devuelve el catálogo completo en JSON compacto para el filtrado en
        // cliente. Nombres de campo cortos para reducir el tamaño de la carga.
        [HttpGet]
        public IActionResult Datos()
        {
            var datos = catalogoService.ObtenerTodos().Select(e => new
            {
                id = e.ExerciseId,
                name = e.Name,
                gif = e.GifUrl,
                body = e.BodyParts,
                equip = e.Equipments,
                target = e.TargetMuscles
            });

            return Json(datos);
        }

        // GET /Catalogo/Detalle/{id}
        // Muestra el detalle de un ejercicio (GIF grande + instrucciones).
        public async Task<IActionResult> Detalle(string id)
        {
            var ejercicio = await catalogoService.ObtenerDetalleAsync(id);
            if (ejercicio == null) return NotFound();
            return View(ejercicio);
        }
    }
}
