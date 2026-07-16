namespace GymTracker.DTOs
{
    // Resultado del cálculo de volumen de una rutina
    public class VolumenDto
    {
        public int RutinaId { get; set; }
        public string NombreRutina { get; set; } = string.Empty;
        public string TipoCalculo { get; set; } = string.Empty;
        public double Volumen { get; set; }
    }
}
