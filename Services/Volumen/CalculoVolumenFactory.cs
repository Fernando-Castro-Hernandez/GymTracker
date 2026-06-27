namespace GymTracker.Services.Volumen
{
    // Factory Method: centraliza la creación de la estrategia de cálculo.
    // El resto del sistema pide una estrategia por su tipo y no conoce
    // las clases concretas ni usa 'new' directamente.
    public class CalculoVolumenFactory
    {
        public ICalculoVolumen Crear(TipoVolumen tipo)
        {
            return tipo switch
            {
                TipoVolumen.Simple => new VolumenSimpleStrategy(),
                TipoVolumen.PorSeries => new VolumenPorSeriesStrategy(),
                TipoVolumen.Relativo => new VolumenRelativoStrategy(),
                _ => throw new ArgumentException($"Tipo de volumen no soportado: {tipo}")
            };
        }
    }
}
