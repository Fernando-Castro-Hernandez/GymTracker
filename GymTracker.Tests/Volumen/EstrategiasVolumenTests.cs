using GymTracker.Models;
using GymTracker.Models.Enums;
using GymTracker.Services.Volumen;

namespace GymTracker.Tests.Volumen
{
    // Pruebas de las tres estrategias de cálculo del ADR-05 (patrón Strategy).
    //
    // Mientras CalculoVolumenFactoryTests protege el CABLEADO (que cada tipo
    // devuelva su clase), esto protege la ARITMÉTICA. Los dos fallos son
    // silenciosos: una fórmula alterada no lanza excepción, solo produce un
    // número plausible pero falso en la gráfica de progreso.
    public class EstrategiasVolumenTests
    {
        // Helper: arma un RutinaEjercicio completo, con su navegación Ejercicio
        // poblada, como lo haría una consulta con .Include(e => e.Ejercicio).
        private static RutinaEjercicio CrearEjercicio(
            int series, int repeticiones, decimal peso, GrupoMuscular grupo)
        {
            return new RutinaEjercicio
            {
                SeriesObjetivo = series,
                RepeticionesObjetivo = repeticiones,
                PesoObjetivo = peso,
                Ejercicio = new Ejercicio { GrupoMuscular = grupo }
            };
        }

        // Rutina de referencia usada por varias pruebas:
        //   Press banca : 4 series × 10 reps × 60 kg = 2400 kg  (Pecho)
        //   Remo        : 3 series × 12 reps × 40 kg = 1440 kg  (Espalda)
        //   -------------------------------------------------
        //   Tonelaje total ........................... 3840 kg
        //   Series totales ...............................  7
        //   Grupos musculares distintos ..................  2
        private static List<RutinaEjercicio> RutinaDeReferencia() =>
        [
            CrearEjercicio(4, 10, 60m, GrupoMuscular.Pecho),
            CrearEjercicio(3, 12, 40m, GrupoMuscular.Espalda)
        ];

        // ===== VolumenSimpleStrategy — tonelaje total =====

        [Fact]
        public void VolumenSimple_ConDosEjercicios_SumaElTonelajeDeAmbos()
        {
            // Arrange
            var estrategia = new VolumenSimpleStrategy();
            var ejercicios = RutinaDeReferencia();

            // Act
            var volumen = estrategia.Calcular(ejercicios);

            // Assert — 4*10*60 + 3*12*40
            Assert.Equal(3840d, volumen);
        }

        [Fact]
        public void VolumenSimple_ConRutinaVacia_DevuelveCero()
        {
            // Arrange
            var estrategia = new VolumenSimpleStrategy();

            // Act
            var volumen = estrategia.Calcular([]);

            // Assert
            Assert.Equal(0d, volumen);
        }

        // Un ejercicio de peso corporal (dominadas, plancha) se registra con
        // PesoObjetivo = 0. En tonelaje aporta 0 kg: es correcto, y es
        // justamente la razón de que exista la estrategia PorSeries.
        [Fact]
        public void VolumenSimple_ConEjercicioDePesoCorporal_NoAportaTonelaje()
        {
            // Arrange
            var estrategia = new VolumenSimpleStrategy();
            var ejercicios = new List<RutinaEjercicio>
            {
                CrearEjercicio(3, 10, 0m, GrupoMuscular.Espalda)
            };

            // Act
            var volumen = estrategia.Calcular(ejercicios);

            // Assert
            Assert.Equal(0d, volumen);
        }

        // ===== VolumenPorSeriesStrategy — series efectivas =====

        [Fact]
        public void VolumenPorSeries_ConDosEjercicios_SumaSoloLasSeries()
        {
            // Arrange
            var estrategia = new VolumenPorSeriesStrategy();
            var ejercicios = RutinaDeReferencia();

            // Act
            var volumen = estrategia.Calcular(ejercicios);

            // Assert — 4 + 3, sin importar reps ni peso
            Assert.Equal(7d, volumen);
        }

        [Fact]
        public void VolumenPorSeries_ConRutinaVacia_DevuelveCero()
        {
            // Arrange
            var estrategia = new VolumenPorSeriesStrategy();

            // Act
            var volumen = estrategia.Calcular([]);

            // Assert
            Assert.Equal(0d, volumen);
        }

        // Esta es la diferencia de fondo entre las dos primeras estrategias:
        // el conteo de series ignora la carga por completo. Es la métrica
        // estándar en hipertrofia y por eso convive con el tonelaje.
        [Fact]
        public void VolumenPorSeries_ConPesoCorporal_CuentaIgualQueConCarga()
        {
            // Arrange
            var estrategia = new VolumenPorSeriesStrategy();
            var conCarga = new List<RutinaEjercicio> { CrearEjercicio(3, 10, 80m, GrupoMuscular.Pierna) };
            var sinCarga = new List<RutinaEjercicio> { CrearEjercicio(3, 10, 0m, GrupoMuscular.Pierna) };

            // Act
            var volumenConCarga = estrategia.Calcular(conCarga);
            var volumenSinCarga = estrategia.Calcular(sinCarga);

            // Assert
            Assert.Equal(volumenConCarga, volumenSinCarga);
            Assert.Equal(3d, volumenConCarga);
        }

        // ===== VolumenRelativoStrategy — tonelaje por grupo muscular =====

        [Fact]
        public void VolumenRelativo_ConDosGruposDistintos_DivideElTonelajeEntreDos()
        {
            // Arrange
            var estrategia = new VolumenRelativoStrategy();
            var ejercicios = RutinaDeReferencia();

            // Act
            var volumen = estrategia.Calcular(ejercicios);

            // Assert — 3840 kg repartidos entre Pecho y Espalda
            Assert.Equal(1920d, volumen);
        }

        // El punto de esta estrategia: detectar desbalance. Concentrar todo el
        // tonelaje en un solo grupo da un relativo el doble de alto que
        // repartirlo en dos, aunque el tonelaje bruto sea idéntico.
        [Fact]
        public void VolumenRelativo_ConUnSoloGrupo_DevuelveElTonelajeCompleto()
        {
            // Arrange
            var estrategia = new VolumenRelativoStrategy();
            var ejercicios = new List<RutinaEjercicio>
            {
                CrearEjercicio(4, 10, 60m, GrupoMuscular.Pecho),
                CrearEjercicio(3, 12, 40m, GrupoMuscular.Pecho)
            };

            // Act
            var volumen = estrategia.Calcular(ejercicios);

            // Assert — mismo tonelaje que la rutina de referencia (3840), pero
            // sin dividir, porque solo se trabajó un grupo muscular
            Assert.Equal(3840d, volumen);
        }

        // Dos ejercicios del MISMO grupo cuentan como un solo grupo: el divisor
        // usa Distinct(), no el número de ejercicios.
        [Fact]
        public void VolumenRelativo_ConGruposRepetidos_NoInflaElDivisor()
        {
            // Arrange
            var estrategia = new VolumenRelativoStrategy();
            var ejercicios = new List<RutinaEjercicio>
            {
                CrearEjercicio(1, 10, 100m, GrupoMuscular.Pierna),  // 1000 kg
                CrearEjercicio(1, 10, 100m, GrupoMuscular.Pierna),  // 1000 kg
                CrearEjercicio(1, 10, 100m, GrupoMuscular.Core)     // 1000 kg
            };

            // Act
            var volumen = estrategia.Calcular(ejercicios);

            // Assert — 3000 kg entre 2 grupos (Pierna y Core), no entre 3
            Assert.Equal(1500d, volumen);
        }

        [Fact]
        public void VolumenRelativo_ConRutinaVacia_DevuelveCero()
        {
            // Arrange
            var estrategia = new VolumenRelativoStrategy();

            // Act
            var volumen = estrategia.Calcular([]);

            // Assert
            Assert.Equal(0d, volumen);
        }

        // CARACTERIZACIÓN DEL COMPORTAMIENTO ACTUAL, no una aprobación de él.
        //
        // VolumenRelativoStrategy es la única de las tres que lee la navegación
        // e.Ejercicio (declarada null! en la entidad). Si los datos llegan sin
        // .Include(e => e.Ejercicio), la estrategia lanza NullReferenceException.
        // Hoy nadie la llama así, pero es una precondición implícita y no escrita.
        //
        // Esta prueba la deja explícita: si algún día se decide blindarla
        // (ignorar los nulos o lanzar una excepción con mensaje claro), esta
        // prueba fallará y obligará a tomar la decisión a conciencia en vez de
        // cambiar el comportamiento por accidente.
        [Fact]
        public void VolumenRelativo_SinLaNavegacionEjercicioCargada_LanzaNullReference()
        {
            // Arrange — un RutinaEjercicio tal como vendría de una consulta
            // SIN Include: los datos escalares están, la navegación no.
            var estrategia = new VolumenRelativoStrategy();
            var ejercicios = new List<RutinaEjercicio>
            {
                new() { SeriesObjetivo = 4, RepeticionesObjetivo = 10, PesoObjetivo = 60m }
            };

            // Act + Assert
            Assert.Throws<NullReferenceException>(() => estrategia.Calcular(ejercicios));
        }

        // ===== Comparación entre estrategias =====

        // Las tres responden preguntas distintas sobre la MISMA rutina. Que sus
        // resultados difieran no es un error: es la razón de ser del Strategy.
        [Fact]
        public void LasTresEstrategias_SobreLaMismaRutina_DanResultadosDistintos()
        {
            // Arrange
            var factory = new CalculoVolumenFactory();
            var ejercicios = RutinaDeReferencia();

            // Act
            var simple = factory.Crear(TipoVolumen.Simple).Calcular(ejercicios);
            var porSeries = factory.Crear(TipoVolumen.PorSeries).Calcular(ejercicios);
            var relativo = factory.Crear(TipoVolumen.Relativo).Calcular(ejercicios);

            // Assert
            Assert.Equal(3840d, simple);
            Assert.Equal(7d, porSeries);
            Assert.Equal(1920d, relativo);
        }
    }
}
