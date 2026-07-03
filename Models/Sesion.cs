namespace GymTracker.Models
{
    public class Sesion
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;

        // Referencia a la rutina origen (trazabilidad). Puede quedar "huérfana"
        // si la rutina se borra: la sesión sobrevive por su cuenta (snapshot).
        public int? RutinaId { get; set; }

        // CONGELADO: copia del nombre de la rutina en el momento de la sesión.
        public string NombreRutina { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string? Notas { get; set; }

        // Propiedad de navegación: las series realizadas en esta sesión.
        public List<SerieRealizada> Series { get; set; } = new();
    }
}
