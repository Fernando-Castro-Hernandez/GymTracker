namespace GymTracker.Models
{
    public class Medicion
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;

        // ===== Obligatorios =====
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public decimal Peso { get; set; }   // kg

        // ===== Composición corporal (báscula de bioimpedancia) — opcionales =====
        public decimal? PorcentajeGrasa { get; set; }   // %
        public decimal? GrasaVisceral { get; set; }     // índice (1-59 aprox.)
        public decimal? MasaMuscular { get; set; }       // kg
        public decimal? PorcentajeAgua { get; set; }     // %

        // ===== Medidas con cinta (cm) — opcionales =====
        public decimal? Cintura { get; set; }
        public decimal? Cadera { get; set; }
        public decimal? Pecho { get; set; }
        public decimal? Brazo { get; set; }
        public decimal? Muslo { get; set; }

        // Notas opcionales del día.
        public string? Notas { get; set; }
    }
}
