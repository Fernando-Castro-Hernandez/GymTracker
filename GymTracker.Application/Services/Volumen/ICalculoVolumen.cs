using GymTracker.Models;

namespace GymTracker.Services.Volumen
{
    // Contrato común: toda estrategia sabe calcular el volumen de una lista
    // de ejercicios de una rutina y describir qué tipo de cálculo aplicó.
    public interface ICalculoVolumen
    {
        string Nombre { get; }
        double Calcular(IEnumerable<RutinaEjercicio> ejercicios);
    }
}