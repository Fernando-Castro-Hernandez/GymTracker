# ADR-05: Integración de patrones de diseño GOF (Strategy y Factory Method) para el cálculo de volumen de entrenamiento

| Campo  | Valor |
|--------|-------|
| Autor  | Fernando Castro Hernández |
| Fecha  | 27/06/2026 |
| Estado | Propuesto |

---

## Contexto

GymTracker permite armar rutinas compuestas por ejercicios con metas de series,
repeticiones y peso objetivo. Hasta ahora, sin embargo, el sistema no ofrecía
ninguna forma de medir "qué tan exigente" es una rutina: los datos estaban, pero
no se explotaban para darle al usuario una métrica de su entrenamiento.

El **volumen de entrenamiento** es esa métrica, pero tiene una particularidad que
es clave para esta decisión: **no existe una única forma correcta de calcularlo**.
Dependiendo del objetivo del usuario, la fórmula relevante cambia:

- **Volumen simple (tonelaje):** `series × repeticiones × peso`. Útil para quien
  prioriza la carga total movida (enfoque de fuerza).
- **Volumen por series efectivas:** el conteo total de series. Es la métrica
  estándar en hipertrofia, donde el número de series semanales por grupo muscular
  es uno de los mejores predictores de crecimiento.
- **Volumen relativo:** el tonelaje promedio por grupo muscular distinto. Sirve
  para evaluar el balance de una rutina entre los músculos que trabaja.

El problema de diseño: si implementara esto con una cadena de `if/else` o un
`switch` dentro del controller, cada fórmula nueva me obligaría a modificar ese
mismo método, haciéndolo más largo y frágil, y mezclaría la lógica de cálculo con
la de atender peticiones HTTP. Necesito una forma de tener varias fórmulas
intercambiables, poder agregar nuevas sin tocar las existentes, y crear la
adecuada de forma centralizada.

---

## Decisión

Integro **dos patrones de diseño GOF de categorías distintas**, que trabajan de
forma complementaria:

### 1. Strategy (patrón de comportamiento)

Encapsulo cada fórmula de cálculo de volumen en su propia clase, todas bajo una
interfaz común `ICalculoVolumen`:

```text
ICalculoVolumen  (interfaz Strategy)
 ├── VolumenSimpleStrategy      → series × reps × peso
 ├── VolumenPorSeriesStrategy   → conteo de series efectivas
 └── VolumenRelativoStrategy    → tonelaje promedio por grupo muscular
```

Cada estrategia implementa el mismo método `Calcular(...)` con una fórmula
distinta. El código que las usa habla solo con la interfaz y no conoce la fórmula
concreta que está ejecutando.

**Problema que resuelve en mi sistema:** permite ofrecer varias métricas de
volumen intercambiables y agregar nuevas fórmulas creando una clase nueva, sin
modificar las existentes ni el controller (principio abierto/cerrado).

### 2. Factory Method (patrón creacional)

Una fábrica, `CalculoVolumenFactory`, centraliza la creación de la estrategia
adecuada según el tipo que pide el usuario (un enum `TipoVolumen`):

```text
CalculoVolumenFactory.Crear(tipo) → devuelve la ICalculoVolumen correspondiente
```

**Problema que resuelve en mi sistema:** evita que el controller use `new` con
clases concretas o repita un `switch` de creación en varios lugares. La decisión
de "qué estrategia instanciar" vive en un único punto; agregar una estrategia solo
toca la fábrica.

### Cómo se complementan

Los dos patrones funcionan juntos y por eso se eligieron como pareja: la **Factory
Method (creacional) produce** la estrategia, y la **Strategy (comportamiento)
ejecuta** la fórmula. En el endpoint `GET /api/rutinas/{id}/volumen?tipo=...`, el
controller pide a la fábrica la estrategia del tipo solicitado y la ejecuta, sin
conocer ninguna fórmula

### ¿Por qué estos patrones y no otros?

| Patrón | Por qué Strategy/Factory y no este |
|--------|-----------------------------------|
| **Template Method** (comportamiento) | También permite variar pasos de un algoritmo, pero mediante herencia y una estructura fija de pasos. Mis fórmulas no comparten una estructura común de pasos —son cálculos completamente distintos—, así que Strategy (composición) encaja mejor que la herencia rígida de Template Method. |
| **Abstract Factory** (creacional) | Está pensado para crear *familias* de objetos relacionados. Aquí solo creo un tipo de objeto (una estrategia de cálculo), así que un Factory Method simple es suficiente; Abstract Factory sería sobre-ingeniería. |
| **Singleton** (creacional) | Resolvería "una sola instancia", pero ese no es mi problema: yo necesito elegir entre varias implementaciones, no garantizar una única. No aplica. |

---

## Consecuencias

**✅ Lo que gano:**

- **Extensibilidad sin tocar lo existente.** Agregar una métrica nueva (por
  ejemplo, volumen por grupo muscular específico) es crear una clase que
  implemente `ICalculoVolumen` y registrarla en la fábrica. El código actual no se
  modifica.
- **Controller limpio.** La lógica de cálculo vive fuera del controller, que solo
  orquesta: pide la estrategia y devuelve el resultado. Esto es coherente con la
  dirección de separar responsabilidades del ADR-03.
- **Lógica testeable de forma aislada.** Cada estrategia es una clase pequeña sin
  dependencias de HTTP ni de la base de datos, fácil de probar con datos de
  ejemplo.
- **Creación centralizada.** Un solo lugar decide qué estrategia instanciar, lo
  que evita dispersión de `new` por el código.

**⚠️ Lo que sacrifico o la complejidad que agrego:**

- **Más clases y archivos.** Para tres fórmulas hay ahora una interfaz, tres
  estrategias, un enum y una fábrica, donde antes podría haber un solo método con
  un `switch`. Es más estructura para un cálculo que hoy es sencillo. *(Trade-off
  concreto #1.)*
- **Indirección.** Para entender qué hace el endpoint hay que seguir el salto
  controller → fábrica → estrategia, en lugar de leer una sola función. A cambio,
  cada pieza es más pequeña y enfocada. *(Trade-off concreto #2.)*
- **Justificable solo si crece.** El valor de los patrones se paga cuando se
  agregan o cambian fórmulas; si el sistema se quedara con una sola métrica fija,
  esta estructura sería innecesaria.

---

## Relación con decisiones anteriores

Esta decisión es coherente con la serie de ADR previos: los patrones mantienen la
lógica de negocio fuera de los controllers (en línea con la arquitectura en capas
del ADR-03) y se exponen a través de la API REST documentada en el ADR-04,
agregando un endpoint nuevo sin alterar los existentes.

## Evolución futura
 
Esta decisión no cierra la puerta a **incorporar más patrones de diseño** en el
futuro. Conforme el sistema crezca y aparezcan problemas concretos que lo
justifiquen —por ejemplo, al implementar los módulos previstos de Sesiones,
Mediciones y Progreso—, se evaluará la integración de patrones adicionales,
siempre bajo el mismo criterio aplicado aquí: que el patrón resuelva un problema
real del sistema y no se añada como un fin en sí mismo. La estructura actual queda
preparada para ello.