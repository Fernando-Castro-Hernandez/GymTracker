using GymTracker.Services.IA;

namespace GymTracker.Tests.IA
{
    // Pruebas del router de CONTEXTO del chatbot (ADR-07, etapa 3).
    //
    // Este método decide cuántos datos se cargan antes de llamar al modelo, o
    // sea, cuántos tokens se pagan por cada mensaje. Su fallo es silencioso: si
    // se degradara y clasificara todo como Datos, nada lanzaría excepción y el
    // chat seguiría respondiendo — solo subiría el costo. Y al revés, si dejara
    // de reconocer preguntas de datos, el modelo respondería sin los números del
    // usuario y daría consejos genéricos.
    //
    // ContextoChatBuilder ramifica con `if (tipo != TipoConsulta.General)`, así
    // que la frontera General / no-General es la que más impacto tiene.
    public class RouterContextoTests
    {
        // ===== Preguntas sobre datos concretos =====

        [Theory]
        [InlineData("¿cuánto entrené esta semana?")]
        [InlineData("¿cuántas series hice de pecho?")]
        [InlineData("¿cuál fue mi volumen la semana pasada?")]
        [InlineData("¿cuánto tonelaje llevo?")]
        [InlineData("muéstrame mi progreso")]
        [InlineData("¿cuál es mi peso corporal actual?")]
        [InlineData("¿cuándo fue mi última medición?")]
        [InlineData("dame mi historial de sesiones")]
        [InlineData("¿cuántas veces entrené pierna?")]
        public void Clasificar_ConPreguntaSobreDatosDelUsuario_DevuelveDatos(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert
            Assert.Equal(TipoConsulta.Datos, tipo);
        }

        // Las listas incluyen variantes con y sin tilde a propósito, porque los
        // usuarios escriben de las dos formas en un chat.
        [Theory]
        [InlineData("¿cuánto entrené?")]
        [InlineData("¿cuanto entrene?")]
        [InlineData("¿cuál fue mi última sesión?")]
        [InlineData("¿cual fue mi ultima sesion?")]
        public void Clasificar_ConOSinTildes_DevuelveDatosIgual(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert
            Assert.Equal(TipoConsulta.Datos, tipo);
        }

        // ===== Preguntas que piden consejo =====

        [Theory]
        [InlineData("¿cómo mejoro mi rutina de pierna?")]
        [InlineData("¿qué me recomiendas para hipertrofia?")]
        [InlineData("recomiéndame ejercicios de espalda")]
        [InlineData("dame un consejo para el press de banca")]
        [InlineData("¿debería entrenar más pecho?")]
        [InlineData("sugiere una variante de sentadilla")]
        [InlineData("¿cómo optimizo mi entrenamiento?")]
        [InlineData("necesito balancear mi rutina")]
        [InlineData("¿cuál es la técnica correcta del peso muerto?")]
        public void Clasificar_ConPeticionDeConsejo_DevuelveConsejo(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert
            Assert.Equal(TipoConsulta.Consejo, tipo);
        }

        // El usuario pregunta desde su propio punto de vista, en primera persona.
        // Estas formas faltaban en PalabrasConsejo (había "mejora"/"mejorar" pero
        // no "mejoro") y caían en General, así que el modelo respondía sin el
        // contexto de las rutinas del usuario. Lo detectaron estas pruebas.
        [Theory]
        [InlineData("¿cómo mejoro mi rutina de pierna?")]
        [InlineData("¿cómo optimizo mi entrenamiento?")]
        [InlineData("¿cómo equilibro pecho y espalda?")]
        public void Clasificar_ConConsejoEnPrimeraPersona_DevuelveConsejo(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert
            Assert.Equal(TipoConsulta.Consejo, tipo);
        }

        // ===== Saludos y preguntas generales =====

        [Theory]
        [InlineData("hola")]
        [InlineData("buenos días")]
        [InlineData("¿qué puedes hacer?")]
        [InlineData("gracias")]
        [InlineData("¿quién eres?")]
        public void Clasificar_ConSaludoOPreguntaGeneral_DevuelveGeneral(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert — General es el único tipo que NO carga contexto de BD
            Assert.Equal(TipoConsulta.General, tipo);
        }

        // ===== La regla de precedencia: Datos gana sobre Consejo =====
        //
        // Este bloque protege el orden de los dos `if` del método. El comentario
        // del código lo declara: "Datos tiene prioridad: si la pregunta menciona
        // datos, necesitamos el contexto completo aunque también pida consejo".
        //
        // Si alguien invirtiera esos dos if, el código compilaría y el chat
        // seguiría funcionando, pero empezaría a dar consejos genéricos sin
        // mirar los números reales del usuario. Estas pruebas lo impiden.

        [Theory]
        [InlineData("mi volumen bajó, ¿cómo lo mejoro?")]
        [InlineData("¿cuánto entrené esta semana y qué me recomiendas?")]
        [InlineData("dame un consejo según mi progreso")]
        [InlineData("¿debería cambiar mi rutina según mi historial?")]
        public void Clasificar_ConPalabrasDeDatosYDeConsejo_PrefiereDatos(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert — ante la duda se carga MÁS contexto: responder sin datos
            // es peor que gastar algunos tokens de más
            Assert.Equal(TipoConsulta.Datos, tipo);
        }

        // ===== Robustez de la entrada =====

        [Fact]
        public void Clasificar_EsInsensibleAMayusculas()
        {
            // Act
            var minusculas = RouterContexto.Clasificar("¿cuánto entrené?");
            var mayusculas = RouterContexto.Clasificar("¿CUÁNTO ENTRENÉ?");
            var mixto = RouterContexto.Clasificar("¿CuÁnTo EnTrEnÉ?");

            // Assert
            Assert.Equal(TipoConsulta.Datos, minusculas);
            Assert.Equal(TipoConsulta.Datos, mayusculas);
            Assert.Equal(TipoConsulta.Datos, mixto);
        }

        // El router corre DESPUÉS del guardarriel, que ya rechazó los mensajes
        // vacíos. Aun así no debe romperse: una excepción aquí tumbaría la
        // petición del chat entera.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("?")]
        [InlineData("12345")]
        public void Clasificar_ConEntradaDegenerada_NoLanzaYCaeEnGeneral(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert
            Assert.Equal(TipoConsulta.General, tipo);
        }

        // General es el caso por defecto: lo que no reconoce ninguna lista cae
        // aquí y carga el contexto mínimo. Es la opción barata y segura.
        [Fact]
        public void Clasificar_ConTextoSinPalabrasClave_CaeEnGeneralPorDefecto()
        {
            // Act
            var tipo = RouterContexto.Clasificar("el clima está agradable hoy");

            // Assert
            Assert.Equal(TipoConsulta.General, tipo);
        }

        // ===== Caracterización: coincidencia por subcadena =====
        //
        // Las listas se evalúan con string.Contains, no por palabra completa.
        // Eso hace que "serie" coincida dentro de "seriedad" y "cuando" dentro
        // de "cuandoquiera", clasificando como Datos frases que no lo son.
        //
        // NO es un error grave: el costo de un falso positivo aquí es cargar
        // contexto de más (unos tokens), no una respuesta incorrecta, y encaja
        // con la política de "ante la duda, más contexto". Estas pruebas lo
        // dejan por escrito para que sea una decisión visible y no un accidente:
        // si algún día se pasa a coincidencia por palabra completa, fallarán y
        // obligarán a confirmar el cambio a conciencia.

        [Theory]
        [InlineData("me tomo el gimnasio con seriedad")]   // "serie" dentro de "seriedad"
        [InlineData("cuandoquiera que entreno me canso")]  // "cuando" dentro de "cuandoquiera"
        public void Clasificar_ConSubcadenaDentroDeOtraPalabra_ClasificaComoDatos(string mensaje)
        {
            // Act
            var tipo = RouterContexto.Clasificar(mensaje);

            // Assert — comportamiento ACTUAL documentado, no aprobado
            Assert.Equal(TipoConsulta.Datos, tipo);
        }

        // ===== Cobertura del enum =====

        // Las tres ramas son alcanzables. Si alguien añadiera un valor nuevo a
        // TipoConsulta sin cablearlo en Clasificar, quedaría muerto y esta
        // prueba lo haría evidente al no poder producirse.
        [Fact]
        public void Clasificar_ProduceLosTresTiposDeConsulta()
        {
            // Act
            var tipos = new[]
            {
                RouterContexto.Clasificar("¿cuánto entrené esta semana?"),
                RouterContexto.Clasificar("¿cómo mejoro mi técnica?"),
                RouterContexto.Clasificar("hola")
            };

            // Assert
            Assert.Equal(
                Enum.GetValues<TipoConsulta>().OrderBy(t => t),
                tipos.Distinct().OrderBy(t => t));
        }
    }
}
