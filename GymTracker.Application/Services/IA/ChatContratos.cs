namespace GymTracker.Services.IA
{
    // Un turno de la conversación tal como se le pasa al proveedor de IA.
    // EsDelUsuario = true → rol "user"; false → rol "assistant".
    public record MensajeChat(bool EsDelUsuario, string Contenido);

    // Respuesta de un proveedor a una llamada de chat, con los datos de
    // observabilidad (ADR-07): qué proveedor respondió, cuántos tokens costó y
    // cuántos se leyeron de la caché de prompt (null si el proveedor no lo expone).
    public record RespuestaChat(
        string Texto,
        string Proveedor,
        int TokensEntrada,
        int TokensSalida,
        int? TokensCacheados);
}
