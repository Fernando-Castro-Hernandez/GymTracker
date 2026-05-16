namespace GymTracker.Models.ViewModels
{
    public class CrearRutinaViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public List<EjercicioEnRutinaViewModel> Ejercicios { get; set; } = new();
    }
}