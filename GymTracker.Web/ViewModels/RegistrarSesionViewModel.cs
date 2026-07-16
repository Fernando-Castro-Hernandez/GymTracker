namespace GymTracker.Models.ViewModels
{
    public class RegistrarSesionViewModel
    {
        public int SesionId { get; set; }
        public string NombreRutina { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string? Notas { get; set; }
        public List<SerieEditableViewModel> Series { get; set; } = new();
    }
}