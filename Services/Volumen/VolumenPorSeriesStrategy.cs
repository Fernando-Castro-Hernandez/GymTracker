using GymTracker.Models;

namespace GymTracker.Services.Volumen
{
    // Conteo de series efectivas: suma del total de series de la rutina.
    // Es la métrica estándar en hipertrofia (series semanales por músculo).
    public class VolumenPorSeriesStrategy : ICalculoVolumen
    {
        public string Nombre => "Volumen por series efectivas  - Total de series";

        public double Calcular(IEnumerable<RutinaEjercicio> ejercicios)
        {
            return ejercicios.Sum(e => e.SeriesObjetivo);
        }
    }
}
