namespace GymTracker.DTOs
{
    // Representa una rutina con sus ejercicios asignados
    public class RutinaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<RutinaEjercicioDto> Ejercicios { get; set; } = new();
    }
}
