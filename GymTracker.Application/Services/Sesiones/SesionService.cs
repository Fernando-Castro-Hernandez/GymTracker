using GymTracker.Application.Abstractions;
using GymTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Application.Services.Sesiones
{
    // Implementación del servicio de Sesiones. Depende de IApplicationDbContext.
    public class SesionService(IApplicationDbContext context) : ISesionService
    {
        public async Task<List<Sesion>> ListarAsync(string usuarioId) =>
            await context.Sesiones
                .Include(s => s.Series)
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.Fecha)
                .ToListAsync();

        public async Task<int?> IniciarDesdeRutinaAsync(int rutinaId, string usuarioId)
        {
            // Cargar la rutina CON sus ejercicios y el Ejercicio relacionado,
            // para poder copiar (congelar) todos los datos del momento.
            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == rutinaId && r.UsuarioId == usuarioId);

            if (rutina == null) return null;

            // Crear la sesión, congelando el nombre de la rutina.
            var sesion = new Sesion
            {
                UsuarioId = usuarioId,
                RutinaId = rutina.Id,
                NombreRutina = rutina.Nombre,
                Fecha = DateTime.UtcNow
            };

            // Por cada ejercicio de la rutina, y por cada serie objetivo, crear una
            // SerieRealizada pre-cargada con la meta como valor inicial (Opción A).
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

            return sesion.Id;
        }

        public async Task<Sesion?> ObtenerConSeriesAsync(int id, string usuarioId) =>
            await context.Sesiones
                .Include(s => s.Series)
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

        public async Task<List<(int EjercicioId, string? ExerciseDbId)>> ObtenerVinculosGifAsync(
            string usuarioId, IEnumerable<int> ejercicioIds)
        {
            var ids = ejercicioIds.Distinct().ToList();

            var vinculos = await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId && ids.Contains(e.Id))
                .Select(e => new { e.Id, e.ExerciseDbId })
                .ToListAsync();

            return vinculos.Select(v => (v.Id, v.ExerciseDbId)).ToList();
        }

        public async Task<bool> GuardarRealesAsync(
            int sesionId, string usuarioId, string? notas,
            IReadOnlyDictionary<int, (int RepsReales, decimal PesoReal)> valoresPorSerieId)
        {
            // Cargar la sesión real CON sus series (rastreadas por EF Core).
            var sesion = await context.Sesiones
                .Include(s => s.Series)
                .FirstOrDefaultAsync(s => s.Id == sesionId && s.UsuarioId == usuarioId);

            if (sesion == null) return false;

            sesion.Notas = notas;

            // Actualizar SOLO los valores reales de cada serie. Se busca cada serie
            // por su Id dentro de las series de ESTA sesión, de modo que no se pueda
            // tocar una serie de otra sesión/usuario.
            foreach (var serie in sesion.Series)
            {
                if (valoresPorSerieId.TryGetValue(serie.Id, out var v))
                {
                    serie.RepeticionesReales = v.RepsReales;
                    serie.PesoReal = v.PesoReal;
                }
            }

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAsync(int id, string usuarioId)
        {
            var sesion = await context.Sesiones
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

            if (sesion == null) return false;

            context.Sesiones.Remove(sesion);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
