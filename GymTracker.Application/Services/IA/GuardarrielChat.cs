namespace GymTracker.Services.IA
{
    // Guardarriel de ENTRADA del chatbot (ADR-07, etapa 2 — "input protection").
    //
    // Es la PRIMERA línea de higiene, no la defensa principal: valida longitud y
    // detecta un puñado de frases de prompt-injection inequívocas. La defensa real
    // contra que el modelo se salga de dominio es el system prompt estricto del
    // ChatService (que además marca los datos del usuario como datos-no-instrucciones).
    //
    // La lista se mantiene CORTA y sin ambigüedad a propósito: patrones como
    // "actúa como" darían falsos positivos con preguntas legítimas ("actúa como si
    // fuera principiante"), así que se excluyen. Preferimos dejar pasar un intento
    // dudoso (el system prompt lo contiene) que bloquear una pregunta válida.
    public static class GuardarrielChat
    {
        public const int LongitudMaxima = 1000;

        // Frases claramente dirigidas a secuestrar o exfiltrar las instrucciones.
        // NO incluye "ignora"/"olvida" sueltos (el usuario puede decir "ignora mi
        // rutina anterior"): se exige la referencia explícita a las instrucciones.
        private static readonly string[] PatronesSospechosos =
        [
            "ignora tus instrucciones", "ignora tus reglas", "olvida tus instrucciones",
            "ignore your instructions", "ignore previous instructions", "disregard your instructions",
            "system prompt", "prompt del sistema",
            "revela tu prompt", "reveal your prompt", "muestra tus instrucciones",
            "jailbreak", "developer mode", "modo desarrollador"
        ];

        public static ResultadoGuardarriel Validar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return ResultadoGuardarriel.Rechazar("Escribe un mensaje para poder ayudarte.");

            var limpio = texto.Trim();

            if (limpio.Length > LongitudMaxima)
                return ResultadoGuardarriel.Rechazar(
                    $"Tu mensaje es muy largo (máximo {LongitudMaxima} caracteres). Resúmelo un poco.");

            var minusculas = limpio.ToLowerInvariant();
            foreach (var patron in PatronesSospechosos)
            {
                if (minusculas.Contains(patron))
                    return ResultadoGuardarriel.Rechazar(
                        "Tu mensaje parece intentar cambiar mi comportamiento. " +
                        "Solo puedo ayudarte con tus rutinas, ejercicios y progreso en GymTracker.");
            }

            return ResultadoGuardarriel.Ok();
        }
    }

    // Resultado de la validación: si es válido y, si no, el motivo (ya redactado
    // para mostrárselo al usuario).
    public record ResultadoGuardarriel(bool EsValido, string? Motivo)
    {
        public static ResultadoGuardarriel Ok() => new(true, null);
        public static ResultadoGuardarriel Rechazar(string motivo) => new(false, motivo);
    }
}
