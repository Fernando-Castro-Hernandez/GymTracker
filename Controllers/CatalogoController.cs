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
        // Muestra la galería; el filtro por grupo se pasa por query (?bodyPart=chest).
        public async Task<IActionResult> Index(string? bodyPart = null, string? cursor = null)
        {
            // DIAGNÓSTICO TEMPORAL - quitar después
            System.Diagnostics.Debug.WriteLine($"[CATALOGO] Cursor recibido: '{cursor}'");

            ViewBag.BodyParts = await catalogoService.ListarBodyPartsAsync();
            // ... resto igual

            // Cargar las partes del cuerpo para los botones de filtro.
            ViewBag.BodyParts = await catalogoService.ListarBodyPartsAsync();
            ViewBag.BodyPartActivo = bodyPart;

            // Cargar la primera (o siguiente) página de ejercicios.
            var respuesta = await catalogoService.ListarAsync(bodyPart, cursor);

            // Pasar el cursor de la siguiente página a la vista (para "Ver más").
            ViewBag.NextCursor = respuesta.Meta?.HasNextPage == true ? respuesta.Meta.NextCursor : null;

            return View(respuesta.Data);
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