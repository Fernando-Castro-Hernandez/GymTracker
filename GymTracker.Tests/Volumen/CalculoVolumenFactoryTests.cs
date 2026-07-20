using GymTracker.Services.Volumen;

namespace GymTracker.Tests.Volumen
{
    // Pruebas del Factory Method del ADR-05.
    //
    // Qué se protege aquí: el contrato "cada TipoVolumen devuelve SU estrategia".
    // Es una promesa que el compilador NO puede verificar, porque el switch
    // devuelve siempre ICalculoVolumen: intercambiar dos ramas por error compila
    // sin quejarse y solo se notaría como un número raro en la gráfica de volumen.
    public class CalculoVolumenFactoryTests
    {
        [Fact]
        public void Crear_ConTipoSimple_DevuelveEstrategiaDeTonelaje()
        {
            // Arrange
            var factory = new CalculoVolumenFactory();

            // Act
            var estrategia = factory.Crear(TipoVolumen.Simple);

            // Assert
            Assert.IsType<VolumenSimpleStrategy>(estrategia);
        }

        [Fact]
        public void Crear_ConTipoPorSeries_DevuelveEstrategiaDeSeriesEfectivas()
        {
            // Arrange
            var factory = new CalculoVolumenFactory();

            // Act
            var estrategia = factory.Crear(TipoVolumen.PorSeries);

            // Assert
            Assert.IsType<VolumenPorSeriesStrategy>(estrategia);
        }

        [Fact]
        public void Crear_ConTipoRelativo_DevuelveEstrategiaDeTonelajePorGrupo()
        {
            // Arrange
            var factory = new CalculoVolumenFactory();

            // Act
            var estrategia = factory.Crear(TipoVolumen.Relativo);

            // Assert
            Assert.IsType<VolumenRelativoStrategy>(estrategia);
        }

        // El propósito del Factory es que el resto del sistema NO use 'new' ni
        // conozca las clases concretas: le basta con recibir el contrato común.
        [Theory]
        [InlineData(TipoVolumen.Simple)]
        [InlineData(TipoVolumen.PorSeries)]
        [InlineData(TipoVolumen.Relativo)]
        public void Crear_ConCualquierTipoValido_DevuelveAlgoQueCumpleElContrato(TipoVolumen tipo)
        {
            // Arrange
            var factory = new CalculoVolumenFactory();

            // Act
            var estrategia = factory.Crear(tipo);

            // Assert
            Assert.IsAssignableFrom<ICalculoVolumen>(estrategia);
            Assert.False(string.IsNullOrWhiteSpace(estrategia.Nombre));
        }

        // Cada llamada construye una instancia nueva. Importa porque las
        // estrategias se piden por petición web: si el Factory cachera una
        // instancia compartida y alguien le añadiera estado, dos usuarios
        // concurrentes se pisarían los datos.
        [Fact]
        public void Crear_LlamadoDosVeces_DevuelveInstanciasIndependientes()
        {
            // Arrange
            var factory = new CalculoVolumenFactory();

            // Act
            var primera = factory.Crear(TipoVolumen.Simple);
            var segunda = factory.Crear(TipoVolumen.Simple);

            // Assert
            Assert.NotSame(primera, segunda);
        }

        // Un TipoVolumen fuera del enum (posible con un cast desde un int que
        // venga de la query string) debe fallar de forma explícita y no devolver
        // null silenciosamente.
        [Fact]
        public void Crear_ConTipoNoSoportado_LanzaArgumentException()
        {
            // Arrange
            var factory = new CalculoVolumenFactory();
            var tipoInexistente = (TipoVolumen)999;

            // Act + Assert
            var ex = Assert.Throws<ArgumentException>(() => factory.Crear(tipoInexistente));
            Assert.Contains("999", ex.Message);
        }
    }
}
