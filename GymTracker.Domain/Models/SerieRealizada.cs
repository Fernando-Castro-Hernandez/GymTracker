using GymTracker.Models.Enums;

namespace GymTracker.Models
{
    public class SerieRealizada
    {
        public int Id { get; set; }
        public int SesionId { get; set; }

        // Referencia al ejercicio origen (trazabilidad).
        public int EjercicioId { get; set; }

        // CONGELADO: copias del ejercicio en el momento de la sesión, para que
        // el historial sobreviva aunque el ejercicio cambie o se borre.
        public string NombreEjercicio { get; set; } = string.Empty;
        public GrupoMuscular GrupoMuscular { get; set; }

        // Qué número de serie es dentro del ejercicio (1, 2, 3...).
        public int NumeroSerie { get; set; }

        // CONGELADO: las metas que tenía la rutina ese día.
        public int RepeticionesObjetivo { get; set; }
        public decimal PesoObjetivo { get; set; }

        // Lo que realmente se ejecutó.
        public int RepeticionesReales { get; set; }
        public decimal PesoReal { get; set; }

        // Propiedad de navegación hacia la sesión padre.
        public Sesion Sesion { get; set; } = null!;
    }
}