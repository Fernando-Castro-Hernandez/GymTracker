using System.Text.Json;
using Google.GenAI;

namespace GymTracker.Services.IA
{
    // Proveedor secundario (fallback) usando Gemini vía el SDK oficial Google.GenAI.
    // Implementa la misma interfaz que ClaudeProveedor: el orquestador los trata igual.
    // Usa "prompted JSON" con parseo defensivo, igual que Claude.
    public class GeminiProveedor : IProveedorIA
    {
        private readonly Client _client;

        public string Nombre => "Gemini Flash";

        public GeminiProveedor(string apiKey)
        {
            _client = new Client(apiKey: apiKey);
        }

        public async Task<AnalisisRutinaDto> AnalizarRutinaAsync(string systemPrompt, string datosRutina)
        {
            // Gemini no separa system/user como Anthropic; combinamos las
            // instrucciones y los datos en un solo prompt.
            var promptCompleto = systemPrompt + "\n\n---\n\nDATOS DE LA RUTINA:\n" + datosRutina;

            var respuesta = await _client.Models.GenerateContentAsync(
                model: "gemini-flash-latest",
                contents: promptCompleto);

            var textoJson = respuesta.Text ?? string.Empty;

            // Limpiar cercas markdown, igual que en Claude.
            textoJson = LimpiarJson(textoJson);

            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var analisis = JsonSerializer.Deserialize<AnalisisRutinaDto>(textoJson, opciones)
                ?? throw new InvalidOperationException("La respuesta de Gemini no pudo interpretarse.");

            return analisis;
        }

        private static string LimpiarJson(string texto)
        {
            texto = texto.Trim();
            if (texto.StartsWith("```"))
            {
                var primerSalto = texto.IndexOf('\n');
                if (primerSalto >= 0) texto = texto[(primerSalto + 1)..];
                if (texto.EndsWith("```")) texto = texto[..^3];
            }
            return texto.Trim();
        }
    }
}