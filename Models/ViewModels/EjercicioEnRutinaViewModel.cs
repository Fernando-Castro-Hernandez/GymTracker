namespace GymTracker.Models.ViewModels
{
    public class EjercicioEnRutinaViewModel
    {
        public int EjercicioId { get; set; }
        public string NombreEjercicio { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public int SeriesObjetivo { get; set; }
        public int RepeticionesObjetivo { get; set; }
        public decimal PesoObjetivo { get; set; }
    }
}