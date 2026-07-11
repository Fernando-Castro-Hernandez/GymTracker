# Diseño — Integración 2 Fase B: GIFs de ejercicios

> Fecha: 2026-07-11 · Autor: Fernando Castro Hernández · Materia: Arquitectura de Software
> Estado: aprobado, en implementación por fases.

## Objetivo

Vincular los ejercicios propios del usuario (en español) con una demostración
visual animada (GIF) de un catálogo externo, y mostrar ese GIF **durante el
entrenamiento** (`Sesiones/Registrar`) y en el **detalle de rutina**
(`Rutinas/Detalle`).

## Contexto: intento anterior (fallido) y por qué

Un primer intento consumía la API OSS de ExerciseDB (`oss.exercisedb.dev`) en
runtime, paginando el catálogo completo en cada visita. Se revirtió porque:

- El tope de página es **25 duro** (verificado: `limit=1500` devuelve 25).
- El catálogo son ~1,500 ejercicios → **60 llamadas** paginadas.
- El OSS aplica **rate limit por ráfaga**: la llamada #11 seguida devuelve
  **HTTP 429** (verificado empíricamente). Resultado: cargas de ~20 s y catálogos
  parciales.

**Hallazgo clave:** el 429 es SOLO de la API de lista (metadatos). El CDN de GIFs
(`static.exercisedb.dev`, detrás de Cloudflare) **no** tiene rate limit — servir
GIFs individuales al vuelo siempre fue viable.

## Decisión de fuente y arquitectura (Camino A)

**Fuente:** OSS ExerciseDB (AscendAPI). Única fuente con GIFs animados reales a
escala (~1,500) y gratuita. Licencia: no comercial + atribución (compatible con
un proyecto académico no monetizado; se añade crédito).

Alternativas evaluadas y descartadas:
- **free-exercise-db (Unlicense, dominio público):** solo imágenes estáticas
  (2 JPG por ejercicio), no animación; 873 ejercicios. Descartada por no cumplir
  la prioridad de producto (GIF animado durante el entrenamiento).
- **wger (CC-BY-SA):** imágenes de comunidad, cobertura irregular; share-alike viral.
- **RapidAPI de pago:** cláusula "Caching Allowed: No" choca con el seed; costo;
  riesgo de IDs distintos.

**Arquitectura:** patrón *cache-aside* / **seed local**. La API externa NUNCA
está en el camino crítico de un request de usuario.

- **Seed una sola vez:** paginación paciente (delay ~1.5 s, backoff ante 429),
  escribe el JSON **solo si el catálogo vino completo** (`hasNextPage:false`).
- **Ubicación:** `SeedData/exercises.json` (copiado al output; no en `wwwroot`).
  Campos por ejercicio: `exerciseId`, `name`, `gifUrl`, `bodyParts`, `equipments`,
  `targetMuscles`, `secondaryMuscles`, `instructions`.
- **`CatalogoService`** se refactoriza para leer el JSON una vez (perezoso),
  cachearlo en `IMemoryCache` y filtrar/buscar **en memoria con LINQ**. Se elimina
  el `HttpClient` del runtime. Respuesta en milisegundos.
- **GIFs en runtime:** hotlink al CDN Cloudflare (`gifUrl` del seed). Ruta futura
  documentada: subir a S3 al desplegar en AWS.

## Esquema (resolver el drift detectado)

La BD local tiene la columna `ExerciseDbId` (text, nullable) pero el modelo C# no,
y un deploy limpio (RDS) no la tendría. Como la columna está **vacía**:

1. `DROP COLUMN "ExerciseDbId"` en la BD local (cero pérdida de datos).
2. Re-añadir `public string? ExerciseDbId { get; set; }` a `Ejercicio.cs`.
3. Generar una migración **limpia** (`AddColumn` nullable) que aplique igual en
   local y en un RDS nuevo.

## Fases (rebanada vertical hacia el GIF)

- **Fase 1 — Cimiento de datos:** seed JSON + refactor de `CatalogoService` a
  lectura local. *Verificable:* "Explorar" carga en milisegundos, sin 429, con
  ~1,500 ejercicios y sus GIFs.
- **Fase 2 — Vinculación (Opción A):** resolver el esquema (arriba) + botón
  "Vincular a mis ejercicios" en cada tarjeta de "Explorar" y selector al
  crear/editar ejercicio. Guarda `ExerciseDbId`.
- **Fase 3 — Mostrar el GIF (prioridad):** GIF del ejercicio vinculado en
  `Sesiones/Registrar` (durante el entrenamiento) y modal en `Rutinas/Detalle`.
  Lógica: si `ExerciseDbId != null` → buscar `gifUrl` en el seed → mostrar.
- **Fase 4 — Filtros avanzados (opcional):** filtros agrupados, contadores
  dinámicos y búsqueda instantánea client-side. Pulido de UX.

Cada fase termina con un mensaje de commit que el autor sube.

## ADR-06 y atribución

- **ADR-06 — Caching/seed del catálogo externo:** documenta cache-aside, el
  desacople usuarios↔API, trade-offs y alternativas descartadas.
- **Atribución:** línea discreta ("Datos de ejercicios: ExerciseDB") respetando
  `DESIGN-RULES.md` (sobrio, sin emojis).

## Restricciones de trabajo

Explicación antes del código; fases con confirmación; el autor hace los commits;
español en charla / inglés en código; `DESIGN-RULES.md` en toda vista.
