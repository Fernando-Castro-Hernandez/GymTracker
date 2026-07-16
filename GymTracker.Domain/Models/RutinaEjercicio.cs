namespace GymTracker.Models
{
    public class RutinaEjercicio
    {
        public int Id { get; set; }
        public int RutinaId { get; set; }
        public int EjercicioId { get; set; }
        public int SeriesObjetivo { get; set; }
        public int RepeticionesObjetivo { get; set; }
        public decimal PesoObjetivo { get; set; }
        public int Orden { get; set; }

        // Propiedades de navegación
        public Rutina Rutina { get; set; } = null!;
        public Ejercicio Ejercicio { get; set; } = null!;
    }
}