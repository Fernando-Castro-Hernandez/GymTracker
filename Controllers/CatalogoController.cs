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
        // Muestra la galería con filtros: zona del cuerpo, equipamiento y texto.
        // Los filtros de zona/equipamiento recargan la página (server-side); la
        // búsqueda por texto además filtra en vivo en el cliente (JS).
        public async Task<IActionResult> Index(
            string? bodyPart = null, string? equipment = null, string? texto = null)
        {
            // Ejercicios a mostrar, aplicando todos los filtros activos.
            var ejercicios = await catalogoService.FiltrarAsync(
                bodyPart: bodyPart, equipment: equipment, texto: texto);

            // Contadores dinámicos por zona del cuerpo (reflejan equipamiento y
            // texto, pero no el bodyPart seleccionado).
            ViewBag.Conteos = await catalogoService.ContarPorBodyPartAsync(equipment, texto);

            // Partes del cuerpo para los botones de filtro.
            ViewBag.BodyParts = await catalogoService.ListarBodyPartsAsync();

            // Estado activo de los filtros (para resaltar y para los enlaces).
            ViewBag.BodyPartActivo = bodyPart;
            ViewBag.EquipmentActivo = equipment;
            ViewBag.TextoActivo = texto;

            return View(ejercicios);
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