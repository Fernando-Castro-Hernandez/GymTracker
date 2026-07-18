namespace GymTracker.DTOs
{
    // Resultado de enviar un mensaje al chatbot.
    // - Rechazado = true → el guardarriel de entrada bloqueó el mensaje ANTES de
    //   llamar al modelo; Contenido trae el motivo y NO se guardó en el historial.
    // - Rechazado = false → Contenido es la respuesta del asistente (ya persistida).
    public class ChatRespuestaDto
    {
        public string Contenido { get; set; } = string.Empty;
        public bool Rechazado { get; set; }
        public string? Proveedor { get; set; }
    }
}
