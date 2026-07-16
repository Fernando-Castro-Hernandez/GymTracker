using GymTracker.Application.Abstractions;
using GymTracker.Services.Volumen;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GymTracker.Services.IA
{
    // Orquestador del Coach IA. Carga la rutina, reutiliza el cálculo de volumen
    // del ADR-05, arma el contexto y el prompt, y delega la llamada al modelo en
    // un IProveedorIA (Claude hoy; con fallback en la Fase 2).
    public class CoachService(
        IApplicationDbContext context,
        CalculoVolumenFactory volumenFactory,
        IProveedorIA proveedor)
    {
        // Instrucciones para el modelo. Es explícito sobre el formato de salida
        // porque usamos "prompted JSON" (sin temperature, el prompt debe guiar).
        private const string SystemPrompt = """
            Eres un entrenador experto en hipertrofia y fuerza. Analizas rutinas de
            gimnasio de forma objetiva y das consejos concretos y accionables.

            Responde ÚNICAMENTE con un objeto JSON válido, sin texto antes ni
            después, sin cercas de código markdown. El JSON debe tener EXACTAMENTE
            esta estructura:

            {
              "balanceMuscular": "string - análisis del balance entre grupos musculares",
              "volumenAdecuado": true or false,
              "comentarioVolumen": "string - comentario sobre si el volumen es apropiado",
              "sugerencias": ["string", "string"],
              "gruposDescuidados": ["string", "string"]
            }

            Reglas:
            - "balanceMuscular": describe si la rutina está equilibrada o sesgada hacia algún grupo.
            - "volumenAdecuado": true si el volumen es razonable para hipertrofia, false si no.
            - "comentarioVolumen": explica brevemente tu evaluación del volumen.
            - "sugerencias": 2 a 4 consejos concretos de mejora.
            - "gruposDescuidados": grupos musculares poco o nada trabajados (lista vacía si está balanceada).
            - Sé directo y específico. Usa los datos reales que se te dan.
            """;

        // Genera el análisis de una rutina del usuario. Valida la propiedad
        // (ownership) de la rutina antes de procesarla.
        public async Task<AnalisisRutinaDto> AnalizarAsync(int rutinaId, string usuarioId)
        {
            var rutina = await context.Rutinas
                .Include(r => r.Ejercicios)
                    .ThenInclude(re => re.Ejercicio)
                .FirstOrDefaultAsync(r => r.Id == rutinaId && r.UsuarioId == usuarioId)
                ?? throw new InvalidOperationException("Rutina no encontrada.");

            if (!rutina.Ejercicios.Any())
                throw new InvalidOperationException("La rutina no tiene ejercicios para analizar.");

            // Reutilizar el cálculo de volumen del ADR-05 (tonelaje).
            var estrategia = volumenFactory.Crear(TipoVolumen.Simple);
            var volumenTotal = estrategia.Calcular(rutina.Ejercicios);

            // Armar el texto de contexto con los datos reales de la rutina.
            var datos = ConstruirContexto(rutina.Nombre, rutina.Ejercicios, volumenTotal);

            // Delegar en el proveedor de IA.
            return await proveedor.AnalizarRutinaAsync(SystemPrompt, datos);
        }

        // Convierte los datos de la rutina en un texto claro para el modelo.
        private static string ConstruirContexto(
            string nombreRutina,
            IEnumerable<Models.RutinaEjercicio> ejercicios,
            double volumenTotal)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Rutina: {nombreRutina}");
            sb.AppendLine($"Volumen total (tonelaje = series × reps × peso): {volumenTotal:0.##} kg");
            sb.AppendLine();
            sb.AppendLine("Ejercicios:");

            // Agrupar volumen por grupo muscular para dar contexto de balance.
            var porGrupo = new Dictionary<string, double>();

            foreach (var re in ejercicios.OrderBy(e => e.Orden))
            {
                var grupo = re.Ejercicio.GrupoMuscular.ToString();
                var vol = re.SeriesObjetivo * re.RepeticionesObjetivo * (double)re.PesoObjetivo;

                porGrupo[grupo] = porGrupo.GetValueOrDefault(grupo) + vol;

                sb.AppendLine(
                    $"- {re.Ejercicio.Nombre} ({grupo}): " +
                    $"{re.SeriesObjetivo} series × {re.RepeticionesObjetivo} reps × {re.PesoObjetivo:0.##} kg");
            }

            sb.AppendLine();
            sb.AppendLine("Distribución de volumen por grupo muscular:");
            foreach (var kv in porGrupo.OrderByDescending(k => k.Value))
            {
                var pct = volumenTotal > 0 ? kv.Value / volumenTotal * 100 : 0;
                sb.AppendLine($"- {kv.Key}: {kv.Value:0.##} kg ({pct:0}%)");
            }

            return sb.ToString();
        }
    }
}