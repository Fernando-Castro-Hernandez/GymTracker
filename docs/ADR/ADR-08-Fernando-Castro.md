# ADR-08: Estrategia de pruebas automatizadas (xUnit) e Integración Continua con GitHub Actions

| Campo  | Valor |
|--------|-------|
| Autor  | Fernando Castro Hernández |
| Fecha  | 19/07/2026 |
| Estado | Aceptado |

---

## Contexto

GymTracker llegó a este punto con siete decisiones arquitectónicas documentadas,
cuatro módulos funcionales, una API REST y un pipeline de LLM. Todo ello se
verificaba de una sola forma: **a mano**. Abrir la aplicación, iniciar sesión,
crear una rutina, registrar una sesión, mirar las gráficas y juzgar a ojo si el
resultado se veía bien.

Ese método tiene tres problemas que se agravan conforme el sistema crece:

1. **No escala.** Cada cambio obliga a volver a revisar todo. En la práctica uno
   revisa solo lo que tocó, y los fallos aparecen justo donde no se miró.
2. **Depende de la memoria.** Las reglas del sistema viven en comentarios y en la
   cabeza del autor. Un comentario que dice "esta lista se mantiene corta a
   propósito" no impide que alguien la alargue seis meses después.
3. **No detecta fallos silenciosos.** Los defectos más peligrosos de este sistema
   no lanzan excepciones: producen un número plausible pero incorrecto, o una
   respuesta menos informada. A ojo son invisibles.

El **ADR-06** ya registraba deuda técnica del proyecto, y el propio **ADR-07**
cerraba admitiendo la carencia de forma explícita:

> *"**Sin tests automatizados:** la verificación es manual, coherente con el
> estado del proyecto."*

Ese mismo ADR-07 incluía una tabla de "preguntas doradas" para verificar el
chatbot a mano. Su caso #2 era: *"¿Cómo mejoro mi rutina de pierna?" → Consejo
usando la rutina real*. Como se documenta más abajo, **ese caso estaba fallando en
producción desde que se escribió**. La prueba manual existía en el papel y nunca
se ejecutó de verdad. Es la demostración más clara del problema que este ADR
resuelve.

A esto se suma un requisito próximo: el despliegue en AWS (ADR-09). Desplegar
automáticamente sin una red de seguridad que verifique el código antes de
publicarlo sería trasladar el riesgo de "funciona en mi máquina" a producción.

---

## Decisión

Incorporo **pruebas unitarias automatizadas con xUnit** en un proyecto propio, y
un **pipeline de Integración Continua con GitHub Actions** que las ejecuta en cada
push y en cada Pull Request.

### 1. xUnit como framework, en un proyecto separado

Se añade `GymTracker.Tests` como **quinto proyecto** de la solución, con
dependencias `Tests → Application → Domain`.

Vive aparte y no dentro de `Application` por una razón concreta: **el código de
pruebas no debe viajar al servidor de producción**. El `Dockerfile` del ADR-09
empaqueta solo `GymTracker.Web` y sus dependencias. La dirección de las flechas de
dependencia definida en el ADR-03 se mantiene intacta.

### 2. Criterio de selección: probar lo que sostiene decisiones arquitectónicas

Éste es el núcleo de la decisión. Con recursos finitos, la pregunta no es *"¿qué
es fácil de probar?"* sino **"¿dónde duele más un fallo silencioso?"**.

El criterio adoptado: **se prueban las clases que sostienen decisiones ya
documentadas en un ADR, y aquellas cuyo fallo no produce una excepción visible.**

| Clase probada | Ubicación | Por qué se eligió | ADR |
|---|---|---|---|
| `CalculoVolumenFactory` | `Services/Volumen/` | Factory Method. El `switch` devuelve siempre `ICalculoVolumen`: intercambiar dos ramas **compila sin error** | ADR-05 |
| Las 3 `ICalculoVolumen` | `Services/Volumen/` | Strategy. Una fórmula alterada no lanza excepción; produce un número plausible pero falso en la gráfica | ADR-05 |
| `GuardarrielChat` | `Services/IA/` | Guardarriel de seguridad. Una defensa sin pruebas es una suposición | ADR-07 |
| `RouterContexto` | `Services/IA/` | Decide cuántos tokens se pagan por mensaje y si el modelo ve o no los datos del usuario | ADR-07 |
| `RutinaService` | `Services/Rutinas/` | Invariante de *ownership*: que un usuario nunca vea ni modifique datos de otro | ADR-03 / ADR-04 |

**Total: 123 pruebas, ejecución completa en menos de 1 segundo.**

### 3. Qué protege cada bloque de pruebas

Detalle de las cinco clases y del riesgo concreto que cubre cada grupo de asserts:

#### `CalculoVolumenFactoryTests` — 8 pruebas (ADR-05)

| Bloque | Pruebas | Qué protege |
|---|---|---|
| Mapeo tipo → estrategia | 3 | Que cada `TipoVolumen` devuelva **su** clase concreta. Cruzar dos ramas del `switch` compila y solo se notaría como un número raro en la gráfica |
| Contrato común | 3 | Que cualquier tipo válido cumpla `ICalculoVolumen` y traiga un `Nombre` no vacío: el resto del sistema no debe conocer las clases concretas |
| Instancias independientes | 1 | Que cada llamada cree un objeto nuevo. Si alguien "optimizara" cacheando una instancia compartida y ésta ganara estado, dos peticiones concurrentes se pisarían |
| Tipo no soportado | 1 | Que un valor fuera del enum (posible con un *cast* desde la query string) falle explícitamente en vez de devolver `null` en silencio |

#### `EstrategiasVolumenTests` — 12 pruebas (ADR-05)

| Bloque | Pruebas | Qué protege |
|---|---|---|
| Tonelaje (`Simple`) | 3 | La fórmula `series × reps × peso`; que una rutina vacía dé 0; que un ejercicio de peso corporal (0 kg) no aporte tonelaje |
| Series efectivas (`PorSeries`) | 3 | Que cuente **solo** series e ignore reps y peso. Una prueba compara 80 kg contra 0 kg y exige el mismo resultado: eso documenta por qué esta estrategia coexiste con el tonelaje |
| Tonelaje por grupo (`Relativo`) | 4 | Que divida entre grupos musculares **distintos**; que con un solo grupo devuelva el tonelaje íntegro; que grupos repetidos no inflen el divisor (verifica el `Distinct()`) |
| Caracterización | 1 | Documenta una **precondición implícita**: `VolumenRelativoStrategy` es la única que lee la navegación `Ejercicio`, y lanza `NullReferenceException` si los datos llegan sin `.Include(...)` |
| Comparación entre estrategias | 1 | Que las tres den resultados distintos sobre la misma rutina. Es la razón de ser del patrón Strategy, ahora escrita como contrato |

#### `GuardarrielChatTests` — 36 pruebas (ADR-07)

| Bloque | Pruebas | Qué protege |
|---|---|---|
| Entradas vacías | 4 | Que `null`, cadena vacía, espacios y tabuladores se rechacen |
| Límite de longitud | 3 | Que 1000 caracteres pasen y 1001 no (límite inclusivo), y que la medida se tome **después** del `Trim()`: el costo en tokens lo determina el contenido, no el relleno |
| Detección de *prompt injection* | 17 | Los 13 patrones reales, en mayúsculas, minúsculas y mixtas, y enterrados a mitad de frase |
| **Falsos positivos** | **9** | **Que preguntas legítimas NO se bloqueen.** Es el bloque de mayor valor: convierte en código ejecutable la decisión de mantener la lista corta a propósito |
| Contrato del resultado | 3 | Que al rechazar siempre haya `Motivo` (porque `ChatService` hace `validacion.Motivo!` y lo muestra al usuario), que al aceptar sea `null`, y que el motivo no revele qué patrones dispararon la detección |

#### `RouterContextoTests` — 43 pruebas (ADR-07)

| Bloque | Pruebas | Qué protege |
|---|---|---|
| Clasificación `Datos` | 9 | Que las preguntas sobre datos concretos carguen el contexto completo |
| Tildes | 4 | Que "cuánto"/"cuanto" clasifiquen igual: los usuarios escriben de ambas formas |
| Clasificación `Consejo` | 9 | Que las peticiones de recomendación carguen rutinas y rendimiento |
| **Primera persona** | **3** | **El defecto encontrado** (ver sección siguiente): "¿cómo mejoro…?" debe ser `Consejo`, no `General` |
| Clasificación `General` | 5 | Que los saludos carguen el contexto mínimo. `ContextoChatBuilder` ramifica con `tipo != General`, así que ésta es la frontera que más impacta el costo |
| **Precedencia** | **4** | **Que `Datos` gane sobre `Consejo`** cuando la pregunta contiene ambos. Protege el orden de los dos `if`: invertirlos compila y el chat sigue funcionando, pero da consejos sin mirar los números reales |
| Robustez | 8 | Insensibilidad a mayúsculas y que entradas degeneradas no lancen excepción (tumbaría la petición entera del chat) |
| Caracterización | 2 | Documenta que `Contains` busca **subcadenas**: "serie" coincide dentro de "seriedad". Falso positivo asumido, porque su costo es cargar contexto de más, no responder mal |
| Cobertura del enum | 1 | Que las tres ramas sean alcanzables |

#### `RutinaServiceTests` — 24 pruebas (ADR-03 / ADR-04)

La clase más importante de la suite: las demás protegen cálculos, y su fallo da un
número raro; ésta protege una regla de **privacidad**, y su fallo hace que un
usuario vea los datos de otro. Cada prueba usa **dos usuarios con datos cruzados**.

| Bloque | Pruebas | Qué protege |
|---|---|---|
| Lectura | 7 | Que sólo se devuelva lo propio; que un usuario nuevo reciba lista vacía y no "todas las rutinas"; el caso **IDOR** de escribir a mano `/Rutinas/Detalle/20` para ver una rutina ajena; que el desplegable no ofrezca ejercicios de otros |
| Validación de negocio | 5 | Que no se pueda asignar a la rutina propia un ejercicio ajeno, ni siquiera mezclado con propios: el lote entero se rechaza |
| Escritura | 6 | Que no se pueda editar ni borrar lo ajeno, verificando **el estado final de la base** y no sólo el booleano devuelto; y que borrar lo propio no cause daño colateral |
| **API pública (ADR-04)** | **6** | **La ausencia deliberada de filtro** en los métodos DTO. Se prueba para que quede registrada como decisión y no como olvido: si alguien "arregla" esto sin leer el ADR-04, la prueba falla y le explica por qué |

### 4. Pipeline de CI con GitHub Actions

`.github/workflows/ci.yml` se dispara en cada push a las ramas de trabajo y en
cada Pull Request hacia `main`. Enciende un runner Ubuntu limpio, instala el SDK
de .NET 10, y ejecuta `restore` → `build` (Release) → `test`.

Dos detalles deliberados:

- **Los pasos van separados** con `--no-restore` y `--no-build`. Si algo falla, el
  log indica exactamente en cuál de las tres etapas ocurrió: descargar paquetes,
  compilar, o una prueba en rojo. Son tres causas con tres soluciones distintas.
- **Se compila en `Release`, no en `Debug`**, para verificar sobre la misma
  configuración con la que se publicaría.

El runner arranca **vacío**: sin código, sin .NET, sin configuración local. Eso no
es una molestia sino la garantía central: si el proyecto compila y pasa ahí,
compila en cualquier máquina. Cualquier dependencia oculta del entorno de
desarrollo queda al descubierto.

---

## Evidencia: un defecto real encontrado al escribir las pruebas

El argumento habitual a favor de las pruebas es que **previenen regresiones**.
Este proyecto obtuvo una demostración más fuerte: **encontraron un defecto latente
en código ya en producción**.

Al escribir `RouterContextoTests` se descubrió que la lista `PalabrasConsejo`
estaba conjugada en infinitivo y tercera persona (`"mejora"`, `"mejorar"`,
`"optimiza"`, `"optimizar"`) pero **omitía la primera persona**, que es como el
usuario realmente pregunta en un chat:

| El usuario escribe | Clasificación previa | Consecuencia |
|---|---|---|
| "¿cómo **mejoro** mi rutina de pierna?" | `General` ❌ | El modelo respondía **sin** las rutinas del usuario |
| "¿cómo **optimizo** mi entrenamiento?" | `General` ❌ | Ídem |
| "¿cómo **equilibro** pecho y espalda?" | `General` ❌ | Ídem |

Como `ContextoChatBuilder` ramifica con `if (tipo != TipoConsulta.General)`, esas
preguntas recibían el contexto mínimo. **Fallo completamente silencioso:** no
lanzaba excepción, no rompía el chat, sólo degradaba la calidad de la respuesta de
una forma que ninguna revisión visual detecta.

Lo más revelador es que la tabla de verificación manual del **ADR-07 ya listaba
ese caso exacto** como pregunta dorada #2. La prueba manual existía en el
documento y nunca se ejecutó de verdad. **Las pruebas automatizadas no fallan en
ejecutarse: ésa es toda la diferencia.**

Corregido añadiendo `"mejoro"`, `"optimizo"` y `"equilibro"`, más tres pruebas que
cubren la primera persona.

### Verificación de que las pruebas realmente detectan fallos

Una prueba que nunca se ha visto fallar no ofrece ninguna garantía. Se
comprobaron dos escenarios introduciendo defectos a propósito y revirtiéndolos:

| Defecto introducido | Resultado |
|---|---|
| Añadir `"ignora"` y `"olvida"` sueltos a `GuardarrielChat` | 2 pruebas en rojo en 89 ms, señalando las preguntas legítimas bloqueadas |
| Quitar `&& r.UsuarioId == usuarioId` de `EliminarAsync` | 1 prueba en rojo: `Expected: False, Actual: True` — un usuario borró la rutina de otro |

Ambos cambios **compilaban sin una sola advertencia**.

---

## Alternativas consideradas

| Alternativa | Por qué se descartó |
|---|---|
| **NUnit / MSTest** como framework | Los tres implementan Arrange-Act-Assert; cambia la sintaxis, no la idea. xUnit es el estándar de facto en .NET moderno y el usado en el curso. Decisión de bajo impacto arquitectónico |
| **Testcontainers** (PostgreSQL real en Docker) | Más fiel, pero lento y con dependencia de Docker en el runner. Para verificar que el `.Where` de *ownership* está presente no aporta nada: InMemory ejecuta LINQ de verdad. Se reserva para pruebas de esquema o de SQL específico de PostgreSQL |
| **SQLite en memoria** | Punto medio razonable (SQL real en RAM). Se descartó por complejidad de configuración frente al mismo poder de detección para lo que aquí se prueba |
| **Pruebas de integración de controllers** (`WebApplicationFactory`) | Verificarían el pipeline HTTP completo, pero exigen base de datos, Identity y configuración. Mayor costo de mantenimiento; quedan como trabajo futuro |
| **Perseguir un porcentaje de cobertura** (p. ej. 80 %) | La cobertura mide líneas ejecutadas, no riesgo cubierto. Habría empujado a probar getters y mapeos triviales en lugar del invariante de *ownership*. Se prefiere un criterio explícito de riesgo |
| **Pruebas de los proveedores de LLM** (`ClaudeProveedor`, `GeminiProveedor`) | Implican I/O de red no determinista: costarían dinero en cada ejecución, exigirían la API key en el runner y se pondrían en rojo si Anthropic sufre una caída. Probar I/O externo en pruebas unitarias es un antipatrón conocido |
| **Jenkins / Azure DevOps** para CI | GitHub Actions está integrado en el repositorio que ya se usa, sin infraestructura extra, y es gratuito e ilimitado en repositorios públicos |

### Lo que deliberadamente NO se prueba

Declararlo es tan importante como declarar lo que sí:

- **Vistas Razor y JavaScript del cliente** — se verifican visualmente; automatizarlo exigiría Playwright o Selenium, desproporcionado para el alcance actual.
- **`ClaudeProveedor` y `GeminiProveedor`** — I/O de red no determinista (ver arriba).
- **Controllers** — tras el ADR-03 son capas delgadas: reciben HTTP y delegan en los servicios, que sí están probados.
- **Migraciones y esquema de EF Core** — se validan al aplicarlas contra PostgreSQL real; InMemory no modela restricciones ni claves foráneas.
- **ASP.NET Core Identity** — código de framework de terceros, ya probado por Microsoft.

---

## Encaje arquitectónico

Un beneficio del **ADR-03** que hasta ahora no se había cobrado: como los
servicios dependen de la abstracción `IApplicationDbContext` y no del
`ApplicationDbContext` concreto —que vive en `Infrastructure`—, se puede probar la
capa `Application` **sin referenciar `Infrastructure`, sin PostgreSQL y sin
Docker**.

Basta una clase `ContextoEnMemoria` en el proyecto de pruebas que implemente esa
interfaz con el proveedor InMemory de EF Core:

```text
GymTracker.Tests  ──►  GymTracker.Application  ──►  GymTracker.Domain
       │
       └── ContextoEnMemoria : DbContext, IApplicationDbContext
```

Cuando se pagó la deuda técnica #2 del **ADR-06** (los controllers accedían
directamente al `DbContext`), el objetivo declarado era separar responsabilidades.
El efecto secundario, visible sólo ahora, es que **el sistema se volvió
testeable**. Con la arquitectura previa, probar `RutinaService` habría exigido
levantar una aplicación web completa.

---

## Consecuencias

**✅ Lo que gano:**

- **Verificación en menos de 1 segundo** de 123 contratos, frente a una revisión
  manual de varios minutos que además nadie repite completa.
- **Decisiones de diseño convertidas en código ejecutable.** Tres reglas que sólo
  vivían en comentarios ahora se hacen cumplir solas: los falsos positivos del
  guardarriel, la precedencia del router y la ausencia de filtro de la API.
- **Una red de seguridad para el despliegue automático** del ADR-09: sin CI, el CD
  publicaría código roto.
- **Detección temprana:** un defecto real encontrado antes de escribir la primera
  línea del pipeline.
- **Deuda del ADR-07 saldada** (*"sin tests automatizados"*).

**⚠️ Lo que sacrifico / la complejidad que agrego:**

- **Un proyecto más que mantener.** Cuando la lógica cambie a propósito, habrá que
  actualizar las pruebas: son código, y también envejecen.
- **Falsa sensación de seguridad.** 123 pruebas en verde no significan "sin bugs",
  sino "estos 123 contratos se cumplen". Las vistas, el JavaScript y la
  integración real siguen verificándose a mano.
- **InMemory no es PostgreSQL.** No valida SQL, claves foráneas ni restricciones.
  Un fallo de esquema no lo detectaría esta suite.
- **Las pruebas de caracterización congelan comportamiento imperfecto.** Las de
  `NullReferenceException` y de coincidencia por subcadena documentan el estado
  actual, no lo aprueban. Si se decide blindarlos, esas pruebas deberán cambiarse
  a conciencia — que es exactamente su propósito.

---

## Relación con decisiones anteriores

- **ADR-03 (capas):** la abstracción `IApplicationDbContext` es lo que hace
  posible probar `Application` de forma aislada. Este ADR **cobra** un beneficio
  que aquel dejó preparado.
- **ADR-04 (API REST):** la ausencia de filtro por `UsuarioId` en los métodos DTO
  se prueba explícitamente para distinguirla de un olvido.
- **ADR-05 (Strategy + Factory):** dos de las cinco clases probadas son
  precisamente los patrones GOF que aquel ADR introdujo.
- **ADR-06 (deuda técnica):** pagar la deuda #2 hizo el sistema testeable. Este
  ADR registra además nueva deuda conocida: sin pruebas de integración, de
  esquema ni de interfaz.
- **ADR-07 (chatbot):** se salda su carencia declarada de pruebas, y se corrige un
  defecto que su propia tabla de verificación manual no llegó a detectar.
- **ADR-09 (despliegue en AWS):** este pipeline es su prerequisito. El despliegue
  continuo se encadena a este mismo workflow y **sólo publica si las pruebas
  pasan**.
