using GymTracker.Models;

namespace GymTracker.Services.Volumen
{
    // Tonelaje total: suma de (series × reps × peso) de cada ejercicio.
    public class VolumenSimpleStrategy : ICalculoVolumen
    {
        public string Nombre => "Volumen simple - Tonelaje total en kg";

        public double Calcular(IEnumerable<RutinaEjercicio> ejercicios)
        {
            return ejercicios.Sum(e =>
                e.SeriesObjetivo * e.RepeticionesObjetivo * (double)e.PesoObjetivo);
        }
    }
}
