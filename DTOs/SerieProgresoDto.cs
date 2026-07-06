namespace GymTracker.DTOs
{
    // Una serie de datos con etiqueta y sus puntos. Modela un "dataset" de Chart.js.
    public class SerieProgresoDto
    {
        public string Etiqueta { get; set; } = string.Empty;
        public List<PuntoProgresoDto> Puntos { get; set; } = new();
    }
}