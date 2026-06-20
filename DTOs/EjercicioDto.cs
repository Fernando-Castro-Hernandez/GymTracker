namespace GymTracker.DTOs
{
    // Representa un ejercicio tal como lo expone la API (JSON plano y estable)
    public class EjercicioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}
