using GymTracker.Application.Abstractions;
using GymTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GymTracker.Services.IA
{
    // Construye el CONTEXTO del chatbot a partir de los datos reales del usuario
    // (ADR-07, etapa 1 — "context construction", SIN RAG).
    //
    // Por qué no RAG: los datos son estructurados y relacionales, así que el
    // "retrieval" correcto es SQL determinista + agregación, no búsqueda semántica.
    // El riesgo de que el historial crezca sin límite se controla con PODA por
    // ventana de tiempo: las sesiones se recortan a las últimas 3 semanas y se
    // ENTREGAN YA AGREGADAS (tonelaje por sesión y por grupo), no serie por serie.
    // Así el contexto se mantiene en ~1-1.5K tokens aunque el usuario acumule meses
    // de entrenamiento.
    //
    // TODA consulta filtra por UsuarioId: el ownership se aplica en SQL ANTES de
    // que nada llegue al modelo, de modo que un prompt-injection no puede exfiltrar
    // datos de otro usuario (no están en el contexto).
    public class ContextoChatBuilder(IApplicationDbContext context)
    {
        private const int SemanasHistorial = 3;

        public async Task<string> ConstruirAsync(string usuarioId, TipoConsulta tipo)
        {
            var sb = new StringBuilder();

            await AgregarRutinas(sb, usuarioId);

            // Para un saludo/pregunta general no cargamos sesiones ni mediciones:
            // es la poda que ahorra la mayor parte de los tokens.
            if (tipo != TipoConsulta.General)
            {
                await AgregarSesionesRecientes(sb, usuarioId);
                await AgregarUltimaMedicion(sb, usuarioId);
            }

            return sb.ToString();
        }

        // Rutinas activas del usuario con sus metas. Siempre se incluyen: son pocas
        // y son el marco de casi cualquier respuesta.
        private async Task AgregarRutinas(StringBuilder sb, string usuarioId)
        {
            var rutinas = await context.Rutinas
                .Where(r => r.UsuarioId == usuarioId)
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            sb.AppendLine("RUTINAS DEL USUARIO:");
            if (rutinas.Count == 0)
            {
                sb.AppendLine("- (No tiene rutinas creadas todavía.)");
                sb.AppendLine();
                return;
            }

            foreach (var r in rutinas)
            {
                sb.AppendLine($"• {r.Nombre} ({r.Ejercicios.Count} ejercicios):");
                foreach (var re in r.Ejercicios.OrderBy(e => e.Orden))
                {
                    sb.AppendLine(
                        $"   - {re.Ejercicio.Nombre} [{re.Ejercicio.GrupoMuscular}]: " +
                        $"{re.SeriesObjetivo}x{re.RepeticionesObjetivo} @ {re.PesoObjetivo:0.##} kg");
                }
            }
            sb.AppendLine();
        }

        // Sesiones de las últimas N semanas, AGREGADAS (poda). Por cada sesión se
        // da el tonelaje total (reps reales × peso real); al final, el tonelaje por
        // grupo muscular en la ventana. Nunca se listan las series una por una.
        private async Task AgregarSesionesRecientes(StringBuilder sb, string usuarioId)
        {
            var desde = DateTime.UtcNow.AddDays(-7 * SemanasHistorial);

            var sesiones = await context.Sesiones
                .Where(s => s.UsuarioId == usuarioId && s.Fecha >= desde)
                .Include(s => s.Series)
                .OrderBy(s => s.Fecha)
                .ToListAsync();

            sb.AppendLine($"SESIONES DE LAS ÚLTIMAS {SemanasHistorial} SEMANAS " +
                          $"(total: {sesiones.Count}):");

            if (sesiones.Count == 0)
            {
                sb.AppendLine("- (No hay entrenamientos registrados en este periodo.)");
                sb.AppendLine();
                return;
            }

            var tonelajePorGrupo = new Dictionary<string, double>();

            foreach (var s in sesiones)
            {
                double tonelajeSesion = 0;
                foreach (var serie in s.Series)
                {
                    var t = serie.RepeticionesReales * (double)serie.PesoReal;
                    tonelajeSesion += t;
                    var grupo = serie.GrupoMuscular.ToString();
                    tonelajePorGrupo[grupo] = tonelajePorGrupo.GetValueOrDefault(grupo) + t;
                }

                sb.AppendLine(
                    $"• {s.Fecha.ToLocalTime():dd/MM} — {s.NombreRutina}: " +
                    $"{s.Series.Count} series, tonelaje {tonelajeSesion:0.##} kg");
            }

            if (tonelajePorGrupo.Count > 0)
            {
                sb.AppendLine("Tonelaje por grupo muscular en el periodo:");
                foreach (var kv in tonelajePorGrupo.OrderByDescending(k => k.Value))
                    sb.AppendLine($"   - {kv.Key}: {kv.Value:0.##} kg");
            }
            sb.AppendLine();
        }

        // Última medición corporal (poda: solo la más reciente, no el histórico).
        private async Task AgregarUltimaMedicion(StringBuilder sb, string usuarioId)
        {
            var m = await context.Mediciones
                .Where(x => x.UsuarioId == usuarioId)
                .OrderByDescending(x => x.Fecha)
                .FirstOrDefaultAsync();

            sb.AppendLine("ÚLTIMA MEDICIÓN CORPORAL:");
            if (m == null)
            {
                sb.AppendLine("- (No hay mediciones registradas.)");
                return;
            }

            sb.Append($"• {m.Fecha.ToLocalTime():dd/MM/yyyy}: {m.Peso:0.##} kg");
            if (m.PorcentajeGrasa.HasValue) sb.Append($", grasa {m.PorcentajeGrasa:0.##}%");
            if (m.MasaMuscular.HasValue) sb.Append($", músculo {m.MasaMuscular:0.##} kg");
            sb.AppendLine();
        }
    }
}
