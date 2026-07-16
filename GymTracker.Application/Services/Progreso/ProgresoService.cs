using GymTracker.Application.Abstractions;
using GymTracker.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Services.Progreso
{
    // Servicio que consulta y agrega datos para las gráficas de progreso.
    // Mantiene la lógica fuera de los controllers (coherente con el ADR-03).
    public class ProgresoService(IApplicationDbContext context)
    {
        // ===== Gráfica A: evolución del peso corporal =====
        // Devuelve un punto por cada medición del usuario, ordenado por fecha.
        public async Task<List<PuntoProgresoDto>> ObtenerPesoCorporalAsync(string usuarioId)
        {
            var mediciones = await context.Mediciones
                .Where(m => m.UsuarioId == usuarioId)
                .OrderBy(m => m.Fecha)
                .Select(m => new PuntoProgresoDto
                {
                    Fecha = m.Fecha.ToString("yyyy-MM-dd"),
                    Valor = (double)m.Peso
                })
                .ToListAsync();

            return mediciones;
        }

        // ===== Gráfica B: progresión de carga de un ejercicio =====
        // Por cada sesión donde aparece el ejercicio, toma el PESO MÁXIMO
        // levantado en ese día. Es la métrica más representativa de fuerza.
        public async Task<List<PuntoProgresoDto>> ObtenerProgresionEjercicioAsync(
            string usuarioId, int ejercicioId)
        {
            // Traer todas las series reales de ese ejercicio, del usuario,
            // junto con la fecha de su sesión.
            var series = await context.SeriesRealizadas
                .Where(s => s.EjercicioId == ejercicioId
                            && s.Sesion.UsuarioId == usuarioId)
                .Select(s => new
                {
                    s.Sesion.Fecha,
                    s.PesoReal
                })
                .ToListAsync();

            // Agrupar por día y quedarse con el peso máximo de cada día.
            var puntos = series
                .GroupBy(s => s.Fecha.ToString("yyyy-MM-dd"))
                .Select(g => new PuntoProgresoDto
                {
                    Fecha = g.Key,
                    Valor = (double)g.Max(s => s.PesoReal)
                })
                .OrderBy(p => p.Fecha)
                .ToList();

            return puntos;
        }

        // ===== Gráfica C: volumen total por sesión =====
        // Volumen = suma de (reps reales × peso real) de todas las series de la sesión.
        // Aplica el concepto de tonelaje del ADR-05 sobre datos REALES.
        public async Task<List<PuntoProgresoDto>> ObtenerVolumenPorSesionAsync(string usuarioId)
        {
            // Traer las series de todas las sesiones del usuario con su fecha.
            var series = await context.SeriesRealizadas
                .Where(s => s.Sesion.UsuarioId == usuarioId)
                .Select(s => new
                {
                    s.SesionId,
                    s.Sesion.Fecha,
                    s.RepeticionesReales,
                    s.PesoReal
                })
                .ToListAsync();

            // Agrupar por sesión y sumar el tonelaje (reps × peso) de cada serie.
            var puntos = series
                .GroupBy(s => new { s.SesionId, s.Fecha })
                .Select(g => new PuntoProgresoDto
                {
                    Fecha = g.Key.Fecha.ToString("yyyy-MM-dd"),
                    Valor = (double)g.Sum(s => s.RepeticionesReales * s.PesoReal)
                })
                .OrderBy(p => p.Fecha)
                .ToList();

            return puntos;
        }
    }
}