using System.Text.Json.Serialization;

namespace GymTracker.Services.Catalogo
{
    // Envoltorio de la respuesta de ExerciseDB: { success, meta, data: [...] }.
    public class RespuestaCatalogoDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("meta")]
        public MetaCatalogo? Meta { get; set; }

        [JsonPropertyName("data")]
        public List<EjercicioCatalogoDto> Data { get; set; } = new();
    }

    // Info de paginación por cursor.
    public class MetaCatalogo
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("nextCursor")]
        public string? NextCursor { get; set; }
    }
}