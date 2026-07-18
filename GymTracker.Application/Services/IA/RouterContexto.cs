namespace GymTracker.Services.IA
{
    // Qué tipo de contexto necesita una pregunta, para armar solo lo necesario.
    public enum TipoConsulta
    {
        // Pregunta sobre datos concretos ("¿cuánto entrené esta semana?"):
        // requiere el contexto completo (rutinas + sesiones + medición).
        Datos,
        // Pide consejo ("¿cómo mejoro mi pierna?"): rutinas + rendimiento real.
        Consejo,
        // Saludo o pregunta general ("hola", "¿qué puedes hacer?"): contexto mínimo.
        General
    }

    // Router de CONTEXTO (ADR-07, etapa 3 — "model gateway" adaptado).
    //
    // En el video/libro, el gateway enruta a distintos MODELOS según la query. Aquí
    // se documenta un matiz honesto: al ser mono-usuario y con Haiku ya barato, el
    // ahorro por cambiar de modelo es ≈ $0, así que NO enrutamos modelos. En su
    // lugar aplicamos la misma idea al CONTEXTO: clasificamos la pregunta por
    // heurística (palabras clave) para decidir cuántos datos cargar, que es la
    // palanca real de tokens/costo en este sistema.
    public static class RouterContexto
    {
        private static readonly string[] PalabrasDatos =
        [
            "cuánto", "cuanto", "cuánta", "cuanta", "cuántas", "cuantas", "cuántos", "cuantos",
            "entrené", "entrene", "esta semana", "semana pasada", "volumen", "tonelaje",
            "progreso", "peso corporal", "medición", "medicion", "medidas",
            "última", "ultima", "último", "ultimo", "cuándo", "cuando",
            "historial", "serie", "series", "sesión", "sesion", "sesiones", "cuántas veces"
        ];

        private static readonly string[] PalabrasConsejo =
        [
            "mejora", "mejorar", "recomienda", "recomiendas", "recomiéndame", "recomiendame",
            "debería", "deberia", "cómo hago", "como hago", "consejo", "consejos",
            "sugerencia", "sugiere", "optimiza", "optimizar", "equilibra", "balance",
            "balancea", "técnica", "tecnica", "descanso", "qué hago", "que hago"
        ];

        public static TipoConsulta Clasificar(string mensaje)
        {
            var t = mensaje.ToLowerInvariant();

            // "Datos" tiene prioridad: si la pregunta menciona datos, necesitamos el
            // contexto completo aunque también pida consejo.
            if (PalabrasDatos.Any(t.Contains)) return TipoConsulta.Datos;
            if (PalabrasConsejo.Any(t.Contains)) return TipoConsulta.Consejo;
            return TipoConsulta.General;
        }
    }
}
