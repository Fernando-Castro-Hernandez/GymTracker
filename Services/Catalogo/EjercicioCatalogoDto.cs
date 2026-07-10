using System.Text.Json.Serialization;

namespace GymTracker.Services.Catalogo
{
    // Un ejercicio del catálogo externo (ExerciseDB). Los nombres JSON se mapean
    // con [JsonPropertyName] porque la API usa camelCase y queremos PascalCase en C#.
    public class EjercicioCatalogoDto
    {
        [JsonPropertyName("exerciseId")]
        public string ExerciseId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("gifUrl")]
        public string GifUrl { get; set; } = string.Empty;

        [JsonPropertyName("bodyParts")]
        public List<string> BodyParts { get; set; } = new();

        [JsonPropertyName("equipments")]
        public List<string> Equipments { get; set; } = new();

        [JsonPropertyName("targetMuscles")]
        public List<string> TargetMuscles { get; set; } = new();

        [JsonPropertyName("secondaryMuscles")]
        public List<string> SecondaryMuscles { get; set; } = new();

        [JsonPropertyName("instructions")]
        public List<string> Instructions { get; set; } = new();
    }
}