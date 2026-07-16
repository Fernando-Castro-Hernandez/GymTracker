using GymTracker.Application.Abstractions;
using GymTracker.DTOs;
using GymTracker.Models;
using GymTracker.Services.Volumen;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Application.Services.Rutinas
{
    // Implementación del servicio de Rutinas. Depende de IApplicationDbContext y de
    // la fábrica de volumen (ADR-05) para el endpoint de volumen de la API.
    public class RutinaService(
        IApplicationDbContext context,
        CalculoVolumenFactory volumenFactory) : IRutinaService
    {
        public async Task<List<Rutina>> ListarAsync(string usuarioId) =>
            await context.Rutinas
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

        public async Task<Rutina?> ObtenerConEjerciciosAsync(int id, string usuarioId) =>
            await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

        public async Task<List<Ejercicio>> ListarEjerciciosDisponiblesAsync(string usuarioId) =>
            await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

        public async Task<bool> EjerciciosPertenecenAlUsuarioAsync(
            string usuarioId, IEnumerable<int> ejercicioIds)
        {
            var ids = ejercicioIds.Distinct().ToList();
            if (ids.Count == 0) return true;

            var validos = await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId && ids.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();

            return validos.Count == ids.Count;
        }

        public async Task<int> CrearAsync(Rutina rutina)
        {
            context.Rutinas.Add(rutina);
            await context.SaveChangesAsync();
            return rutina.Id;
        }

        public async Task<bool> ActualizarAsync(
            int id, string usuarioId, string nombre, string? descripcion,
            IReadOnlyList<RutinaEjercicio> ejercicios)
        {
            // Cargar la rutina original CON sus ejercicios (para reemplazarlos),
            // validando ownership.
            var original = await context.Rutinas
                .Include(r => r.Ejercicios)
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            if (original == null) return false;

            original.Nombre = nombre;
            original.Descripcion = descripcion;

            // Estrategia delete-and-replace: EF Core rastrea las eliminaciones e
            // inserciones y las aplica en una sola transacción.
            original.Ejercicios.Clear();
            foreach (var re in ejercicios)
            {
                original.Ejercicios.Add(re);
            }

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Rutina?> ObtenerParaEliminarAsync(int id, string usuarioId) =>
            await context.Rutinas
                .Include(r => r.Ejercicios)
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

        public async Task<bool> EliminarAsync(int id, string usuarioId)
        {
            var rutina = await context.Rutinas
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId);

            if (rutina == null) return false;

            // OnDelete(Cascade) elimina sus RutinaEjercicio automáticamente.
            context.Rutinas.Remove(rutina);
            await context.SaveChangesAsync();
            return true;
        }

        // ===== Consultas de la API REST pública (sin filtro de usuario) =====

        public async Task<List<RutinaDto>> ListarDtoAsync() =>
            await context.Rutinas
                .OrderByDescending(r => r.FechaCreacion)
                .Select(r => new RutinaDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    FechaCreacion = r.FechaCreacion,
                    Ejercicios = r.Ejercicios
                        .OrderBy(re => re.Orden)
                        .Select(re => new RutinaEjercicioDto
                        {
                            EjercicioId = re.EjercicioId,
                            NombreEjercicio = re.Ejercicio.Nombre,
                            GrupoMuscular = re.Ejercicio.GrupoMuscular.ToString(),
                            SeriesObjetivo = re.SeriesObjetivo,
                            RepeticionesObjetivo = re.RepeticionesObjetivo,
                            PesoObjetivo = re.PesoObjetivo,
                            Orden = re.Orden
                        }).ToList()
                })
                .ToListAsync();

        public async Task<RutinaDto?> ObtenerDtoAsync(int id) =>
            await context.Rutinas
                .Where(r => r.Id == id)
                .Select(r => new RutinaDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    FechaCreacion = r.FechaCreacion,
                    Ejercicios = r.Ejercicios
                        .OrderBy(re => re.Orden)
                        .Select(re => new RutinaEjercicioDto
                        {
                            EjercicioId = re.EjercicioId,
                            NombreEjercicio = re.Ejercicio.Nombre,
                            GrupoMuscular = re.Ejercicio.GrupoMuscular.ToString(),
                            SeriesObjetivo = re.SeriesObjetivo,
                            RepeticionesObjetivo = re.RepeticionesObjetivo,
                            PesoObjetivo = re.PesoObjetivo,
                            Orden = re.Orden
                        }).ToList()
                })
                .FirstOrDefaultAsync();

        public async Task<VolumenDto?> CalcularVolumenAsync(int id, TipoVolumen tipo)
        {
            // Traer la rutina con sus ejercicios (incluyendo el Ejercicio, que la
            // estrategia Relativa necesita para leer el grupo muscular).
            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rutina == null) return null;

            // Factory Method crea la estrategia; Strategy ejecuta la fórmula (ADR-05).
            ICalculoVolumen estrategia = volumenFactory.Crear(tipo);
            double resultado = estrategia.Calcular(rutina.Ejercicios);

            return new VolumenDto
            {
                RutinaId = rutina.Id,
                NombreRutina = rutina.Nombre,
                TipoCalculo = estrategia.Nombre,
                Volumen = resultado
            };
        }
    }
}
