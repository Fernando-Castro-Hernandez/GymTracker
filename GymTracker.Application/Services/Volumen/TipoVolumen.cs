namespace GymTracker.Services.Volumen
{
    // Los tipos de cálculo de volumen que el usuario puede elegir
    public enum TipoVolumen
    {
        Simple,      // tonelaje: series × reps × peso
        PorSeries,   // conteo de series efectivas
        Relativo     // ajustado por grupo muscular
    }
}