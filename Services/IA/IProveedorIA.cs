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
    }
}
