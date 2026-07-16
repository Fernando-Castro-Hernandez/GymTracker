using GymTracker.Application.Services.Ejercicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class ProgresoController(IEjercicioService ejercicios) : Controller
    {
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET /Progreso
        // Renderiza la página de gráficas. Los datos se cargan por fetch a la API;
        // aquí solo pasamos la lista de ejercicios del usuario para el selector.
        public async Task<IActionResult> Index()
        {
            var lista = await ejercicios.ListarAsync(ObtenerUsuarioId(), grupo: null);
            return View(lista);
        }
    }
}
