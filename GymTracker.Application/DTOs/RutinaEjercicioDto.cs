namespace GymTracker.DTOs
{
    // Un ejercicio dentro de una rutina, con sus metas
    public class RutinaEjercicioDto
    {
        public int EjercicioId { get; set; }
        public string NombreEjercicio { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public int SeriesObjetivo { get; set; }
        public int RepeticionesObjetivo { get; set; }
        public decimal PesoObjetivo { get; set; }
        public int Orden { get; set; }
    }
}