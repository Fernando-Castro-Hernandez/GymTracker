using GymTracker.Models;

namespace GymTracker.Services.Volumen
{
    // Volumen relativo: tonelaje promedio por grupo muscular distinto.
    // Sirve para ver el balance entre músculos en lugar de un total bruto.
    public class VolumenRelativoStrategy : ICalculoVolumen
    {
        public string Nombre => "Volumen relativo - Tonelaje promedio por grupo muscular";

        public double Calcular(IEnumerable<RutinaEjercicio> ejercicios)
        {
            var lista = ejercicios.ToList();
            if (lista.Count == 0) return 0;

            // Cuántos grupos musculares distintos toca la rutina
            var gruposDistintos = lista
                .Select(e => e.Ejercicio.GrupoMuscular)
                .Distinct()
                .Count();

            if (gruposDistintos == 0) return 0;

            double tonelajeTotal = lista.Sum(e =>
                e.SeriesObjetivo * e.RepeticionesObjetivo * (double)e.PesoObjetivo);

            // Tonelaje promedio por grupo muscular trabajado
            return tonelajeTotal / gruposDistintos;
        }
    }
}
