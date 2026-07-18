namespace GymTracker.Models
{
    // Un mensaje del historial de conversación del Chatbot IA (Integración 4).
    // Cada usuario tiene su propio hilo (filtro por UsuarioId, como el resto del
    // sistema). La API de los LLM es stateless: el historial se persiste aquí y
    // se reenvía (podado) en cada llamada — ese es el "estado conversacional"
    // que esta integración demuestra (ver ADR-07).
    public class ChatMensaje
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;

        // true = lo escribió el usuario; false = lo respondió el asistente.
        public bool EsDelUsuario { get; set; }

        public string Contenido { get; set; } = string.Empty;

        // Se guarda en UTC (como el resto del sistema); se muestra en local.
        public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

        // ===== Observabilidad (solo en mensajes del asistente; null en los del
        // usuario) — evidencia empírica de costo y rendimiento para el ADR-07. =====
        public string? Proveedor { get; set; }     // "Claude Haiku", "Gemini Flash"...
        public int? TokensEntrada { get; set; }
        public int? TokensSalida { get; set; }
        public int? LatenciaMs { get; set; }
    }
}
