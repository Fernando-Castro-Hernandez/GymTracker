# GymTracker — Rama `05-patrones`

Esta rama integra **dos patrones de diseño GOF** al proyecto, partiendo del estado
de la rama `04-api` (que añadió la API REST). El objetivo es resolver un problema
real del sistema —el cálculo del volumen de entrenamiento— de forma extensible y
bien estructurada.

La decisión completa está documentada en `docs/ADR/ADR-05-Fernando-Castro.md`.

## ¿Qué problema resuelve?

Una rutina tiene ejercicios con series, repeticiones y peso, pero el sistema no
ofrecía ninguna forma de medir "qué tan exigente" es. El **volumen de
entrenamiento** es esa métrica, y tiene una particularidad clave: **no existe una
única fórmula correcta**. Depende del objetivo del usuario:

- **Volumen simple (tonelaje):** `series × repeticiones × peso`. Enfoque de fuerza.
- **Volumen por series efectivas:** el total de series. Métrica estándar en
  hipertrofia.
- **Volumen relativo:** tonelaje promedio por grupo muscular. Mide el balance de
  la rutina.

## Patrones implementados

Se integraron **dos patrones de categorías distintas**, que trabajan juntos:

### 1. Strategy (patrón de comportamiento)

Encapsula cada fórmula de cálculo en su propia clase, todas bajo la interfaz común
`ICalculoVolumen`. El código que las usa habla solo con la interfaz y no conoce la
fórmula concreta que ejecuta.

Archivos en `Services/Volumen/`:

- `ICalculoVolumen.cs` — la interfaz (el contrato): un método `Calcular(...)` y un
  `Nombre` descriptivo.
- `VolumenSimpleStrategy.cs` — suma de `series × reps × peso`.
- `VolumenPorSeriesStrategy.cs` — suma del total de series.
- `VolumenRelativoStrategy.cs` — tonelaje promedio por grupo muscular distinto.
- `TipoVolumen.cs` — enum que identifica cada tipo (`Simple`, `PorSeries`,
  `Relativo`).

**Beneficio:** agregar una fórmula nueva es crear una clase que implemente
`ICalculoVolumen`, sin modificar las existentes (principio abierto/cerrado).

### 2. Factory Method (patrón creacional)

Centraliza la creación de la estrategia adecuada según el tipo solicitado, para
que el resto del sistema no use `new` con clases concretas.

- `CalculoVolumenFactory.cs` — su método `Crear(TipoVolumen tipo)` devuelve la
  estrategia correspondiente.

**Beneficio:** la decisión de "qué estrategia instanciar" vive en un único lugar.

### Cómo trabajan juntos

La **Factory produce** la estrategia y la **Strategy ejecuta** la fórmula. Son de
categorías distintas (creacional + comportamiento), conectadas de forma natural.

## Cómo se usa (endpoint)

Los patrones se exponen a través de la API REST en un endpoint nuevo:

```
GET /api/rutinas/{id}/volumen?tipo=Simple
GET /api/rutinas/{id}/volumen?tipo=PorSeries
GET /api/rutinas/{id}/volumen?tipo=Relativo
```

El controller pide a la factory la estrategia del tipo solicitado y la ejecuta,
sin conocer ninguna fórmula:

```csharp
var factory = new CalculoVolumenFactory();         // Factory: decide y crea
ICalculoVolumen estrategia = factory.Crear(tipo);  // entrega la Strategy
double resultado = estrategia.Calcular(ejercicios);// Strategy: ejecuta la fórmula
```

La respuesta es un `VolumenDto` con el id y nombre de la rutina, el tipo de cálculo
aplicado y el volumen resultante. El mismo endpoint devuelve resultados distintos
según el `tipo`, lo que demuestra el patrón Strategy en acción.

## Probarlo localmente

1. Levantar la base de datos: `docker compose up -d`.
2. Correr el proyecto (`dotnet run` o F5).
3. Abrir Swagger en `https://localhost:7192/swagger`.
4. Usar `GET /api/rutinas` para obtener el id de una rutina con ejercicios.
5. Probar `GET /api/rutinas/{id}/volumen` con los tres tipos y comparar resultados.

## Archivos añadidos en esta rama

```
Services/Volumen/
├── ICalculoVolumen.cs
├── VolumenSimpleStrategy.cs
├── VolumenPorSeriesStrategy.cs
├── VolumenRelativoStrategy.cs
├── TipoVolumen.cs
└── CalculoVolumenFactory.cs
DTOs/
└── VolumenDto.cs
docs/ADR/
└── ADR-05-Fernando-Castro.md
```

Además, se añadió el endpoint `GetVolumen` en `Controllers/Api/RutinasApiController.cs`.

## Uso responsable de IA

En el desarrollo de este proyecto se utilizó **Claude** como
herramienta de apoyo, bajo un uso responsable:

- Sirvió como apoyo para **entender conceptos** (arquitectura, patrones de diseño,
  API REST), redactar los ADR y guiar la implementación paso a paso.
- **Todo el código fue revisado, comprendido y probado** por el autor antes de
  integrarse; la IA no se usó como sustituto del aprendizaje ni del criterio
  propio.
- Las **decisiones de diseño y arquitectura son propias**, tomadas con base en las
  necesidades reales del sistema.
- Se verificó que cada cambio funcionara correctamente y no comprometiera la
  integridad del proyecto.

La IA se empleó como un asistente de aprendizaje y productividad, manteniendo la
responsabilidad y la autoría del trabajo en todo momento.
