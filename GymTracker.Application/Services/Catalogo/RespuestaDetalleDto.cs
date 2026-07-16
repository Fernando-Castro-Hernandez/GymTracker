using System.Text.Json.Serialization;

namespace GymTracker.Services.Catalogo
{
    // Envoltorio del detalle de un ejercicio: { success, data: {...} }.
    public class RespuestaDetalleDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public EjercicioCatalogoDto? Data { get; set; }
    }

    // Envoltorio de listas de nombres (bodyparts, muscles): { success, data: [{name}] }.
    public class RespuestaNombresDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public List<NombreDto> Data { get; set; } = new();
    }

    public class NombreDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}