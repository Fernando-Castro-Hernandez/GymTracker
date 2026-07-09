namespace GymTracker.Services.IA
{
    // Datos de una rutina que se le entregan a la IA como contexto.
    // Se construye en el CoachService a partir de la BD + el cálculo de
    // volumen del ADR-05.
    public class ContextoRutina
    {
        public string NombreRutina { get; set; } = string.Empty;
        public List<EjercicioContexto> Ejercicios { get; set; } = new();
        public double VolumenTotal { get; set; }
        public Dictionary<string, double> VolumenPorGrupo { get; set; } = new();
    }

    public class EjercicioContexto
    {
        public string Nombre { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public decimal Peso { get; set; }
    }
}