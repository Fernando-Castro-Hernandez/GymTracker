using GymTracker.Data;
using GymTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymTracker.Controllers
{
    [Authorize]
    public class ProgresoController(ApplicationDbContext context) : Controller
    {
        private string ObtenerUsuarioId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET /Progreso
        // Renderiza la página de gráficas. Los datos se cargan por fetch a la API;
        // aquí solo pasamos la lista de ejercicios del usuario para el selector.
        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioId();

            var ejercicios = await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            return View(ejercicios);
        }
    }
}