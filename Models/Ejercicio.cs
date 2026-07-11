using GymTracker.Models.Enums;

namespace GymTracker.Models
{
    public class Ejercicio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public GrupoMuscular GrupoMuscular { get; set; }
        public string? Descripcion { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string? ExerciseDbId { get; set; }
    }
}