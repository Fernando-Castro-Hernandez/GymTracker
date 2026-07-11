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

        // Id del ejercicio en el catálogo externo (ExerciseDB) con el que se
        // vincula, para mostrar su GIF. null = sin animación vinculada. Se guarda
        // el Id (estable) y no la URL, que se reconstruye desde el seed.
        public string? ExerciseDbId { get; set; }
    }
}