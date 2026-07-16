namespace GymTracker.Services.IA
{
    // Estructura del análisis que devuelve la IA. Se fuerza esta forma mediante
    // "tool use" para garantizar una respuesta consistente y mostrable en la UI.
    public class AnalisisRutinaDto
    {
        // Descripción del balance entre grupos musculares de la rutina.
        public string BalanceMuscular { get; set; } = string.Empty;

        // ¿El volumen total es adecuado para el objetivo (hipertrofia)?
        public bool VolumenAdecuado { get; set; }

        // Explicación sobre el volumen (cifras, contexto).
        public string ComentarioVolumen { get; set; } = string.Empty;

        // Sugerencias concretas de mejora.
        public List<string> Sugerencias { get; set; } = new();

        // Grupos musculares poco trabajados o ausentes.
        public List<string> GruposDescuidados { get; set; } = new();
    }
}