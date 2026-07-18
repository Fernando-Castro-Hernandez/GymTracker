namespace GymTracker.DTOs
{
    // Un mensaje del historial tal como lo expone la API para pintar el widget.
    // Fecha va en UTC; la vista la muestra en hora local.
    public class ChatMensajeDto
    {
        public bool EsDelUsuario { get; set; }
        public string Contenido { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
