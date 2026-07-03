namespace GymTracker.Models.ViewModels
{
    public class SerieEditableViewModel
    {
        public int Id { get; set; }
        public string NombreEjercicio { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public int NumeroSerie { get; set; }

        // Metas congeladas (solo lectura en la vista, como referencia).
        public int RepeticionesObjetivo { get; set; }
        public decimal PesoObjetivo { get; set; }

        // Valores reales (editables por el usuario).
        public int RepeticionesReales { get; set; }
        public decimal PesoReal { get; set; }
    }
}