namespace GymTracker.Services.IA
{
    // Contrato que cumple cualquier proveedor de IA (Claude hoy; Gemini u otro
    // en la Fase 2 del fallback). El CoachService habla con esta interfaz, no
    // con un proveedor concreto: eso permite intercambiarlos o encadenarlos.
    public interface IProveedorIA
    {
        // Nombre legible del proveedor (para logs y para saber cuál respondió).
        string Nombre { get; }

        // Envía las instrucciones (systemPrompt) y los datos de la rutina, y
        // devuelve el análisis ya estructurado. Lanza excepción si el proveedor
        // falla (la maneja quien orqueste el fallback).
        Task<AnalisisRutinaDto> AnalizarRutinaAsync(string systemPrompt, string datosRutina);

        // Chatbot con contexto (Integración 4). Recibe el systemPrompt (con las
        // instrucciones + el contexto podado del usuario) y el historial de la
        // conversación en orden cronológico, y devuelve la respuesta en texto
        // libre más los datos de observabilidad. Lanza excepción si el proveedor
        // falla (lo maneja el fallback).
        //
        // Nota de diseño (ISP): se añade este método a la MISMA interfaz en vez de
        // crear una interfaz aparte, para conservar un único "gateway" de IA y que
        // el fallback (Claude → Gemini) cubra por igual análisis y chat. El costo
        // es que un proveedor debe implementar ambos usos; se asume a conciencia
        // porque hoy los dos proveedores existentes soportan los dos.
        Task<RespuestaChat> ChatearAsync(string systemPrompt, IReadOnlyList<MensajeChat> historial);
    }
}
