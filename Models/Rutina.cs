namespace GymTracker.Models
{
    public class Rutina
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación: los ejercicios asignados a esta rutina
        public List<RutinaEjercicio> Ejercicios { get; set; } = new();
    }
}