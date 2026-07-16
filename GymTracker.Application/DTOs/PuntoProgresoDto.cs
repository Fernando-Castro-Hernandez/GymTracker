namespace GymTracker.DTOs
{
    // Un punto en una serie temporal: una fecha y un valor.
    // Sirve para las tres gráficas (peso corporal, carga, volumen).
    public class PuntoProgresoDto
    {
        // Fecha en formato ISO (yyyy-MM-dd) para que Chart.js la ordene bien.
        public string Fecha { get; set; } = string.Empty;
        public double Valor { get; set; }
    }
}