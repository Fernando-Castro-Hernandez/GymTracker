using GymTracker.Application.Services.Rutinas;
using GymTracker.Models;
using GymTracker.Models.Enums;
using GymTracker.Services.Volumen;
using GymTracker.Tests.Infraestructura;

namespace GymTracker.Tests.Rutinas
{
    // Pruebas del INVARIANTE DE OWNERSHIP (ADR-03 + regla obligatoria de CLAUDE.md:
    // "cada usuario solo ve y edita sus propios datos").
    //
    // Por qué esta clase es la más importante de la suite: las demás protegen
    // cálculos, y su fallo produce un número raro. Esta protege una regla de
    // PRIVACIDAD, y su fallo hace que un usuario vea los datos de otro.
    //
    // El fallo típico es una OMISIÓN, no un error: alguien añade un método y
    // olvida el .Where(r => r.UsuarioId == usuarioId). El código compila, corre y
    // devuelve datos. Con un solo usuario en la base de desarrollo es invisible:
    // todo lo que ves es tuyo. Solo aparece cuando hay un segundo usuario, o sea,
    // en producción. De ahí que cada prueba use DOS usuarios con datos cruzados.
    public class RutinaServiceTests
    {
        private const string UsuarioA = "usuario-a-guid";
        private const string UsuarioB = "usuario-b-guid";

        // Arma un servicio real sobre una base en memoria ya poblada con datos de
        // los DOS usuarios. Devuelve también el contexto para poder verificar el
        // estado final de la base tras las operaciones de escritura.
        private static (RutinaService servicio, ContextoEnMemoria contexto) CrearEscenario()
        {
            var contexto = ContextoEnMemoria.Crear();

            var ejercicioA = new Ejercicio
            {
                Id = 1,
                Nombre = "Press banca",
                GrupoMuscular = GrupoMuscular.Pecho,
                UsuarioId = UsuarioA
            };
            var ejercicioB = new Ejercicio
            {
                Id = 2,
                Nombre = "Sentadilla",
                GrupoMuscular = GrupoMuscular.Pierna,
                UsuarioId = UsuarioB
            };
            contexto.Ejercicios.AddRange(ejercicioA, ejercicioB);

            contexto.Rutinas.AddRange(
                new Rutina
                {
                    Id = 10,
                    Nombre = "Pecho lunes (de A)",
                    UsuarioId = UsuarioA,
                    FechaCreacion = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                    Ejercicios =
                    [
                        new RutinaEjercicio
                        {
                            Id = 100, EjercicioId = 1, SeriesObjetivo = 4,
                            RepeticionesObjetivo = 10, PesoObjetivo = 60m, Orden = 1
                        }
                    ]
                },
                new Rutina
                {
                    Id = 11,
                    Nombre = "Espalda jueves (de A)",
                    UsuarioId = UsuarioA,
                    FechaCreacion = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc)
                },
                new Rutina
                {
                    Id = 20,
                    Nombre = "Pierna martes (de B)",
                    UsuarioId = UsuarioB,
                    FechaCreacion = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                    Ejercicios =
                    [
                        new RutinaEjercicio
                        {
                            Id = 200, EjercicioId = 2, SeriesObjetivo = 5,
                            RepeticionesObjetivo = 5, PesoObjetivo = 100m, Orden = 1
                        }
                    ]
                });

            contexto.SaveChanges();

            return (new RutinaService(contexto, new CalculoVolumenFactory()), contexto);
        }

        // ===== Lectura: nunca ver lo ajeno =====

        [Fact]
        public async Task ListarAsync_SoloDevuelveLasRutinasDelUsuarioQuePregunta()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var rutinasDeA = await servicio.ListarAsync(UsuarioA);

            // Assert
            Assert.Equal(2, rutinasDeA.Count);
            Assert.All(rutinasDeA, r => Assert.Equal(UsuarioA, r.UsuarioId));
            Assert.DoesNotContain(rutinasDeA, r => r.Id == 20);
        }

        [Fact]
        public async Task ListarAsync_ConUsuarioSinRutinas_DevuelveListaVaciaYNoLasDeOtros()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var rutinas = await servicio.ListarAsync("usuario-nuevo-sin-datos");

            // Assert — vacío, NO "todas las rutinas"
            Assert.Empty(rutinas);
        }

        [Fact]
        public async Task ListarAsync_OrdenaDeLaMasRecienteALaMasAntigua()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var rutinas = await servicio.ListarAsync(UsuarioA);

            // Assert — la del 5 de julio antes que la del 1 de julio
            Assert.Equal(11, rutinas[0].Id);
            Assert.Equal(10, rutinas[1].Id);
        }

        // El caso clásico de IDOR (Insecure Direct Object Reference): el usuario A
        // escribe a mano /Rutinas/Detalle/20 en la barra de direcciones. Debe
        // recibir null (el controller lo traduce a NotFound), no la rutina de B.
        [Fact]
        public async Task ObtenerConEjerciciosAsync_ConIdDeOtroUsuario_DevuelveNull()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act — A pide la rutina 20, que es de B
            var rutina = await servicio.ObtenerConEjerciciosAsync(20, UsuarioA);

            // Assert
            Assert.Null(rutina);
        }

        [Fact]
        public async Task ObtenerConEjerciciosAsync_ConIdPropio_DevuelveLaRutinaConSusEjercicios()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var rutina = await servicio.ObtenerConEjerciciosAsync(10, UsuarioA);

            // Assert
            Assert.NotNull(rutina);
            Assert.Equal("Pecho lunes (de A)", rutina.Nombre);
            Assert.Single(rutina.Ejercicios);
            // La navegación ThenInclude debe venir poblada: VolumenRelativoStrategy
            // depende de ella (ver EstrategiasVolumenTests).
            Assert.NotNull(rutina.Ejercicios[0].Ejercicio);
        }

        [Fact]
        public async Task ObtenerParaEliminarAsync_ConIdDeOtroUsuario_DevuelveNull()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var rutina = await servicio.ObtenerParaEliminarAsync(20, UsuarioA);

            // Assert
            Assert.Null(rutina);
        }

        [Fact]
        public async Task ListarEjerciciosDisponiblesAsync_SoloDevuelveLosEjerciciosDelUsuario()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var ejercicios = await servicio.ListarEjerciciosDisponiblesAsync(UsuarioA);

            // Assert — el dropdown de asignación no debe ofrecer ejercicios ajenos
            Assert.Single(ejercicios);
            Assert.Equal("Press banca", ejercicios[0].Nombre);
        }

        // ===== Validación de negocio: no asignar ejercicios ajenos =====

        [Fact]
        public async Task EjerciciosPertenecenAlUsuarioAsync_ConEjercicioDeOtroUsuario_DevuelveFalse()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act — A intenta meter en su rutina el ejercicio 2, que es de B
            var pertenecen = await servicio.EjerciciosPertenecenAlUsuarioAsync(UsuarioA, [2]);

            // Assert
            Assert.False(pertenecen);
        }

        [Fact]
        public async Task EjerciciosPertenecenAlUsuarioAsync_ConMezclaDePropioYAjeno_DevuelveFalse()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act — uno propio (1) y uno ajeno (2): debe rechazarse el lote entero
            var pertenecen = await servicio.EjerciciosPertenecenAlUsuarioAsync(UsuarioA, [1, 2]);

            // Assert
            Assert.False(pertenecen);
        }

        [Fact]
        public async Task EjerciciosPertenecenAlUsuarioAsync_ConEjerciciosPropios_DevuelveTrue()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var pertenecen = await servicio.EjerciciosPertenecenAlUsuarioAsync(UsuarioA, [1]);

            // Assert
            Assert.True(pertenecen);
        }

        [Fact]
        public async Task EjerciciosPertenecenAlUsuarioAsync_ConListaVacia_DevuelveTrue()
        {
            // Arrange — una rutina sin ejercicios asignados es válida
            var (servicio, _) = CrearEscenario();

            // Act
            var pertenecen = await servicio.EjerciciosPertenecenAlUsuarioAsync(UsuarioA, []);

            // Assert
            Assert.True(pertenecen);
        }

        [Fact]
        public async Task EjerciciosPertenecenAlUsuarioAsync_ConIdInexistente_DevuelveFalse()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var pertenecen = await servicio.EjerciciosPertenecenAlUsuarioAsync(UsuarioA, [9999]);

            // Assert
            Assert.False(pertenecen);
        }

        // ===== Escritura: nunca modificar ni borrar lo ajeno =====

        [Fact]
        public async Task ActualizarAsync_SobreRutinaDeOtroUsuario_DevuelveFalseYNoModificaNada()
        {
            // Arrange
            var (servicio, contexto) = CrearEscenario();

            // Act — A intenta editar la rutina 20, que es de B
            var actualizada = await servicio.ActualizarAsync(
                20, UsuarioA, "Hackeada", null, []);

            // Assert
            Assert.False(actualizada);

            // Y la rutina de B quedó intacta en la base
            var rutinaDeB = contexto.Rutinas.Single(r => r.Id == 20);
            Assert.Equal("Pierna martes (de B)", rutinaDeB.Nombre);
        }

        [Fact]
        public async Task ActualizarAsync_SobreRutinaPropia_ReemplazaNombreYEjercicios()
        {
            // Arrange
            var (servicio, contexto) = CrearEscenario();
            var nuevosEjercicios = new List<RutinaEjercicio>
            {
                new()
                {
                    EjercicioId = 1, SeriesObjetivo = 5,
                    RepeticionesObjetivo = 8, PesoObjetivo = 70m, Orden = 1
                }
            };

            // Act
            var actualizada = await servicio.ActualizarAsync(
                10, UsuarioA, "Pecho lunes v2", "con más peso", nuevosEjercicios);

            // Assert
            Assert.True(actualizada);

            var rutina = contexto.Rutinas.Single(r => r.Id == 10);
            Assert.Equal("Pecho lunes v2", rutina.Nombre);
            Assert.Equal("con más peso", rutina.Descripcion);
        }

        [Fact]
        public async Task EliminarAsync_SobreRutinaDeOtroUsuario_DevuelveFalseYNoLaBorra()
        {
            // Arrange
            var (servicio, contexto) = CrearEscenario();

            // Act — A intenta borrar la rutina 20, que es de B
            var eliminada = await servicio.EliminarAsync(20, UsuarioA);

            // Assert
            Assert.False(eliminada);
            Assert.True(contexto.Rutinas.Any(r => r.Id == 20));
        }

        [Fact]
        public async Task EliminarAsync_SobreRutinaPropia_LaBorraYDejaIntactasLasDeOtros()
        {
            // Arrange
            var (servicio, contexto) = CrearEscenario();

            // Act
            var eliminada = await servicio.EliminarAsync(10, UsuarioA);

            // Assert
            Assert.True(eliminada);
            Assert.False(contexto.Rutinas.Any(r => r.Id == 10));
            // Sin daño colateral: las de B siguen ahí
            Assert.True(contexto.Rutinas.Any(r => r.Id == 20));
        }

        [Fact]
        public async Task EliminarAsync_ConIdInexistente_DevuelveFalse()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var eliminada = await servicio.EliminarAsync(9999, UsuarioA);

            // Assert
            Assert.False(eliminada);
        }

        [Fact]
        public async Task CrearAsync_GuardaLaRutinaYDevuelveSuIdNuevo()
        {
            // Arrange
            var (servicio, contexto) = CrearEscenario();
            var nueva = new Rutina { Nombre = "Full body", UsuarioId = UsuarioA };

            // Act
            var id = await servicio.CrearAsync(nueva);

            // Assert
            Assert.True(id > 0);
            var guardada = contexto.Rutinas.Single(r => r.Id == id);
            Assert.Equal(UsuarioA, guardada.UsuarioId);
        }

        // ===== La API REST pública: ausencia DELIBERADA de filtro (ADR-04) =====
        //
        // Estos tres métodos NO filtran por UsuarioId, y eso es correcto: el
        // IRutinaService lo declara explícitamente y el ADR-04 lo decide. La API
        // de catálogo es pública y de solo lectura; el ProgresoApiController, que
        // sí expone datos personales, lleva [Authorize].
        //
        // Se prueban aquí para que la ausencia de filtro quede registrada como
        // DECISIÓN y no como olvido. Si alguien "arregla" esto añadiendo un filtro
        // sin leer el ADR-04, estas pruebas fallarán y le explicarán por qué.

        [Fact]
        public async Task ListarDtoAsync_DevuelveLasRutinasDeTodosLosUsuarios_PorDisenoDelADR04()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var dtos = await servicio.ListarDtoAsync();

            // Assert — las 3 rutinas (2 de A + 1 de B): la API es pública
            Assert.Equal(3, dtos.Count);
        }

        [Fact]
        public async Task ObtenerDtoAsync_MapeaLosEjerciciosConSuNombreYGrupoMuscular()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var dto = await servicio.ObtenerDtoAsync(10);

            // Assert — el DTO evita ciclos de serialización (convención del proyecto)
            Assert.NotNull(dto);
            Assert.Equal("Pecho lunes (de A)", dto.Nombre);
            var ejercicio = Assert.Single(dto.Ejercicios);
            Assert.Equal("Press banca", ejercicio.NombreEjercicio);
            Assert.Equal("Pecho", ejercicio.GrupoMuscular);
        }

        [Fact]
        public async Task ObtenerDtoAsync_ConIdInexistente_DevuelveNull()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var dto = await servicio.ObtenerDtoAsync(9999);

            // Assert
            Assert.Null(dto);
        }

        // ===== Integración con el ADR-05: el servicio reutiliza Strategy+Factory =====

        [Fact]
        public async Task CalcularVolumenAsync_UsaLaEstrategiaPedidaSobreLosEjerciciosDeLaRutina()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act — rutina 10: 4 series × 10 reps × 60 kg
            var volumen = await servicio.CalcularVolumenAsync(10, TipoVolumen.Simple);

            // Assert
            Assert.NotNull(volumen);
            Assert.Equal(2400d, volumen.Volumen);
            Assert.Equal("Pecho lunes (de A)", volumen.NombreRutina);
        }

        [Fact]
        public async Task CalcularVolumenAsync_ConTipoPorSeries_DevuelveElConteoDeSeries()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var volumen = await servicio.CalcularVolumenAsync(10, TipoVolumen.PorSeries);

            // Assert
            Assert.NotNull(volumen);
            Assert.Equal(4d, volumen.Volumen);
        }

        [Fact]
        public async Task CalcularVolumenAsync_ConRutinaInexistente_DevuelveNull()
        {
            // Arrange
            var (servicio, _) = CrearEscenario();

            // Act
            var volumen = await servicio.CalcularVolumenAsync(9999, TipoVolumen.Simple);

            // Assert
            Assert.Null(volumen);
        }
    }
}
