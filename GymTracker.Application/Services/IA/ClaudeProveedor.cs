using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace GymTracker.Services.IA
{
    // Implementación de IProveedorIA usando Claude Haiku vía el SDK oficial.
    // Usa "prompted JSON": se le pide al modelo que responda solo con un JSON
    // con la estructura de AnalisisRutinaDto, y se parsea de forma defensiva.
    public class ClaudeProveedor : IProveedorIA
    {
        private readonly AnthropicClient _client;

        public string Nombre => "Claude Haiku";

        public ClaudeProveedor(string apiKey)
        {
            // El cliente se configura con la API key (viene de User Secrets /
            // variable de entorno, nunca del código).
            _client = new AnthropicClient(new Anthropic.Core.ClientOptions { ApiKey = apiKey });
        }

        public async Task<AnalisisRutinaDto> AnalizarRutinaAsync(string systemPrompt, string datosRutina)
        {
            var parameters = new MessageCreateParams
            {
                Model = "claude-haiku-4-5",
                MaxTokens = 1024,
                System = systemPrompt,
                Messages =
                [
                    new()
                    {
                        Role = Role.User,
                        Content = datosRutina
                    }
                ]
            };

            var respuesta = await _client.Messages.Create(parameters);

            // Extraer el texto de la respuesta (el modelo devuelve bloques de
            // contenido; tomamos el texto).
            var textoJson = ExtraerTexto(respuesta);

            // Limpiar posibles cercas de código markdown (```json ... ```).
            textoJson = LimpiarJson(textoJson);

            // Parsear de forma defensiva. Si falla, lanzamos excepción para que
            // (en Fase 2) se dispare el fallback a otro proveedor.
            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var analisis = JsonSerializer.Deserialize<AnalisisRutinaDto>(textoJson, opciones)
                ?? throw new InvalidOperationException("La respuesta de la IA no pudo interpretarse.");

            return analisis;
        }

        // Chatbot con contexto (Integración 4). A diferencia del análisis, la
        // respuesta es texto libre (un chat no necesita JSON). Aplica prompt
        // caching sobre el bloque system (instrucciones + contexto del usuario):
        // dentro de una misma conversación ese prefijo es idéntico entre turnos,
        // así que los turnos 2..N lo leen de la caché en vez de re-tokenizarlo.
        public async Task<RespuestaChat> ChatearAsync(
            string systemPrompt, IReadOnlyList<MensajeChat> historial)
        {
            // El system se pasa como un bloque de texto marcado con cache_control
            // "ephemeral" (la caché de Anthropic dura ~5 min, justo el ritmo de una
            // conversación). El SDK solo permite cache_control en la forma de lista
            // de bloques, no en el string simple.
            var bloqueSystem = new TextBlockParam
            {
                Text = systemPrompt,
                CacheControl = new CacheControlEphemeral()
            };

            var mensajes = historial
                .Select(m => new MessageParam
                {
                    Role = m.EsDelUsuario ? Role.User : Role.Assistant,
                    Content = m.Contenido
                })
                .ToList();

            var parameters = new MessageCreateParams
            {
                Model = "claude-haiku-4-5",
                MaxTokens = 800,
                System = new List<TextBlockParam> { bloqueSystem },
                Messages = mensajes
            };

            var respuesta = await _client.Messages.Create(parameters);

            var texto = ExtraerTexto(respuesta).Trim();
            var uso = respuesta.Usage;

            // CacheReadInputTokens: cuántos tokens de entrada se sirvieron desde la
            // caché (0/null en el primer turno; >0 en los siguientes). Evidencia
            // empírica del ahorro para el ADR-07.
            return new RespuestaChat(
                Texto: texto,
                Proveedor: Nombre,
                TokensEntrada: (int)uso.InputTokens,
                TokensSalida: (int)uso.OutputTokens,
                TokensCacheados: (int?)uso.CacheReadInputTokens);
        }

        // Recorre los bloques de contenido y concatena el texto.
        private static string ExtraerTexto(Message respuesta)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var bloque in respuesta.Content)
            {
                if (bloque.TryPickText(out var textBlock))
                {
                    sb.Append(textBlock.Text);
                }
            }
            return sb.ToString();
        }

        // Quita cercas de markdown que el modelo a veces añade alrededor del JSON.
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