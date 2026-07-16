namespace GymTracker.Models.ViewModels
{
    public class EditarRutinaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public List<EjercicioEnRutinaViewModel> Ejercicios { get; set; } = new();
    }
}