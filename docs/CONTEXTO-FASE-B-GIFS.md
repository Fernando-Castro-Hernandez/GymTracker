# Contexto para Claude Code — GymTracker, Integración 2 Fase B (GIFs de ejercicios)

> **Autor:** Fernando Castro Hernández · **Curso:** Arquitectura de Software
> **Estado del repo:** revertido a Fase A. Último commit útil: `ae1e94c` (Revert de Fase B).
> **Objetivo de este doc:** dar a Claude Code todo el contexto para retomar la Fase B
> resolviendo el problema de rendimiento que nos bloqueó (carga de ~20s + errores 429).

---

## 1. Qué es GymTracker (stack)

App web ASP.NET Core 10 MVC · EF Core · PostgreSQL 16 (Docker) · Identity · Bootstrap 5 · Chart.js.
Proyecto académico individual. App en `https://localhost:44353`, Swagger en `/swagger`. Todo en `main`.

**Gotcha de infraestructura (importante):** hay dos PostgreSQL que chocan en el puerto 5432:
uno nativo Windows (`postgresql-x64-18`, para otra clase) y el de Docker (`gymtracker-db`, para GymTracker).
Antes de trabajar: `Stop-Service postgresql-x64-18` para liberar el puerto, y `docker compose up -d`.

**Gotcha de build:** IIS Express bloquea `GymTracker.dll`. Hay que detener la app antes de
`dotnet ef` / `dotnet build`. Si un build falla con `MSB3021 ... IIS Express Worker Process`,
matar el proceso: `Get-Process iisexpress | Stop-Process -Force`. Recargar con `Ctrl+F5` por caché.

**Forma de trabajo de Fernando:** paso a paso confirmando cada fase; entender el "porqué" antes del
código; aplica cambios manualmente en su Visual Studio (no copia a ciegas); commits pequeños y
descriptivos que hace él mismo; trabaja en español, código en inglés; convenciones de UI en
`DESIGN-RULES.md` (cero emojis decorativos, SVG en vez de emojis, variables `--gt-*`,
Saira Condensed en títulos, tema oscuro, acento lima `--gt-accent` #c5ff2e).

---

## 2. Objetivo de la Fase B (lo que queremos lograr)

Vincular **los ejercicios propios del usuario** (en español, ej. "Curl Bayesian") con los
**GIFs animados de un catálogo externo de ejercicios**, para que:

1. El GIF aparezca **durante el entrenamiento** en `Sesiones/Registrar.cshtml` (PRIORIDAD de producto).
2. El GIF aparezca en `Rutinas/Detalle.cshtml` (modal con el GIF).

**Decisiones de diseño ya tomadas (mantener):**
- Guardar solo el **`ExerciseDbId`** (string nullable) en la entidad `Ejercicio`, NO la URL del GIF.
  Razón: los IDs son estables; las URLs del CDN pueden cambiar. Con el ID se reconstruye la URL y se
  accede a todo el ejercicio (instrucciones, músculos, etc.). El `null` = "sin GIF vinculado".
- La vinculación se hará desde crear/editar ejercicio **y** desde el catálogo "Explorar".
- **Camino de vinculación elegido = "Opción A" (visual desde Explorar):** el usuario navega el catálogo
  con filtros, ve el GIF, presiona un botón **"Vincular a mis ejercicios"** y elige a cuál de sus
  ejercicios asociarlo. Se evita traducir español→inglés y no depende de que el buscador de la API acierte.

---

## 3. Qué se completó ANTES de esta sesión (Fase A, sí está en `main`)

**Integración 2 Fase A — Catálogo "Explorar" (COMPLETA y funcionando):**
- Fuente de datos: **ExerciseDB OSS** (`https://oss.exercisedb.dev/api/v1/`), sin API key, sin CORS
  (se llama desde el backend). Devuelve GIFs + músculos + instrucciones.
- `Services/Catalogo/`: `CatalogoService` (usa `IHttpClientFactory` cliente "ExerciseDB",
  caché en memoria), DTOs con `[JsonPropertyName]`: `EjercicioCatalogoDto`, `RespuestaCatalogoDto`,
  `RespuestaDetalleDto`.
- `CatalogoController [Authorize]`: `/Catalogo` (galería) y `/Catalogo/Detalle/{id}`.
- Vistas `Views/Catalogo/Index.cshtml` (galería con GIFs, `loading="lazy"`, filtros por bodyPart,
  botón "Ver más") y `Detalle.cshtml` (GIF grande + instrucciones, limpia prefijo "Step:N").
- `Program.cs`: `AddMemoryCache()`, `AddHttpClient("ExerciseDB", ...)` con `BaseAddress` y timeout 10s,
  `AddScoped<CatalogoService>`.
- **Paginación de la API OSS:** cursor con parámetro **`after`** (NO `cursor`). Límite `limit` máx **25**.
  Respuesta lista: `{ success, meta:{ total, hasNextPage, nextCursor }, data:[...] }`.
  Respuesta detalle: `{ success, data:{...} }`.
- **Estructura de un ejercicio:** `exerciseId`, `name`, `gifUrl`, `bodyParts[]`, `equipments[]`,
  `targetMuscles[]`, `secondaryMuscles[]`, `instructions[]`.

---

## 4. Qué se INTENTÓ esta sesión y por qué se REVIRTIÓ (aprendizajes clave)

> Todo esto se hizo en el commit `fa21df0` y luego se revirtió (`ae1e94c`). **NO está activo en el código**,
> pero el aprendizaje es crítico para no repetir errores.

### 4.1 — Se hizo (y funcionaba a nivel de datos)
- **B0 — Migración `ExerciseDbId`:** se agregó `public string? ExerciseDbId { get; set; }` a la entidad
  `Ejercicio` y se generó/aplicó la migración `AgregarExerciseDbIdAEjercicio` (un `AddColumn` nullable
  sobre tabla `Ejercicios`, tipo `text`).
  ⚠️ **La migración se APLICÓ a la base de datos y el revert de git NO la revirtió.** Es decir: la columna
  `ExerciseDbId` YA EXISTE en la tabla `Ejercicios`, aunque el código actual (Fase A) no la conozca.
  Esto es inofensivo (columna ignorada). Al retomar: el paso B0 ya está hecho a nivel de datos.
  Decidir si se mantiene o si se prefiere partir limpio revirtiendo la migración.

### 4.2 — El problema que nos bloqueó (LO MÁS IMPORTANTE)
Se refactorizó `CatalogoService` a un modelo **"dataset en memoria"**: traer TODO el catálogo
(~1,500 ejercicios) paginando internamente contra la API OSS (páginas de 25, siguiendo `after`),
acumular en una `List`, y cachearlo en `IMemoryCache` (se decidió 7 días). La idea era habilitar
filtros avanzados, contadores dinámicos y búsqueda instantánea filtrando en memoria con LINQ.

**Por qué falló:**
- El OSS aplica **rate limiting agresivo (HTTP 429 Too Many Requests)**. Traer 1,500 ÷ 25 = ~60 llamadas
  seguidas dispara el 429 con facilidad.
- Se intentaron parches: `Task.Delay` de 350ms entre páginas + retry con backoff (1.5s, 3s, 4.5s) ante 429
  + caché parcial con TTL corto. Aun así:
  - La **primera carga tardaba ~20 segundos** (inaceptable para UX).
  - Bajo 429 repetido, la carga quedaba **PARCIAL** (ej. solo 250–300 ejercicios de 1,500). Faltaban
    zonas enteras (ej. "neck" desaparecía). Los contadores mostraban ~250 en vez de ~1,500.
  - Una "salvaguarda" que reintentaba la carga completa si no estaba completa **empeoró todo**: como el 429
    nunca dejaba completar, cada visita reintentaba desde cero → lentísimo, ejercicios desaparecían al volver.

**Conclusión / diagnóstico de raíz:** traer el catálogo al vuelo desde la API OSS en runtime es
**frágil por naturaleza** — depende de decenas de llamadas seguidas a un servicio gratuito con rate limit
que no quiere ese patrón. Cada demo/reinicio/expiración de caché = otra ronda de 429. **Inaceptable
para producción (Fernando planea desplegar en AWS y compartir con compañeros de gimnasio para pruebas).**

### 4.3 — La solución acordada (NO alcanzamos a implementarla bien)
**Catálogo local "seed" (JSON):** traer los ~1,500 ejercicios UNA sola vez, guardarlos en un archivo
JSON dentro del proyecto (ej. `wwwroot/data/ejercicios.json`), y que la app lea de ese archivo local.
Cero llamadas a la API en runtime para el catálogo. Lectura de JSON local = milisegundos. Los GIFs
individuales seguirían viniendo del CDN al vuelo (URLs estables, una imagen por vez, sin rate limit).

Se intentó generar el seed con un **endpoint temporal protegido** `/Catalogo/GenerarSeed`
(solo en `env.IsDevelopment()`, escribe a `wwwroot/data/ejercicios.json`, no sobrescribe si la descarga
quedó parcial). **Falló porque el 429 seguía dando descargas parciales** (300 de ~1,500). Aquí paramos.

---

## 5. Investigación de APIs (contexto para decidir la fuente de datos)

Existen **dos sabores** del mismo proveedor **AscendAPI**:

- **OSS gratis** `oss.exercisedb.dev` (el que usa el proyecto hoy): sin key, sin límite mensual documentado,
  pero **rate limiting por ráfaga (429)** confirmado empíricamente. Licencia OSS non-commercial + atribución.
  Ideal para proyecto académico; el caching temporal es aceptable.
- **RapidAPI de pago** "EDB with videos and images by AscendAPI"
  (host `edb-with-gifs-and-images-by-ascendapi.p.rapidapi.com`): requiere `X-RapidAPI-Key`.
  - Plan **Basic gratis: 2,000 req/mes (hard limit)**, 1000/hora, 200 ejercicios, **"Caching Allowed ✗"**.
  - Planes de pago: Pro $100/mo (80,000 req/mes). Ultra/Mega, etc.
  - Trae campos extra que el OSS no tiene: `difficulty` (intermediate/advanced), `exerciseTypes`,
    `movementType` (compound/isolation), GIFs en varias resoluciones.
  - ⚠️ **Su cláusula "Caching Allowed ✗" choca con la estrategia de caché/seed local.** Por eso se DESCARTÓ
    el de pago: además de costo, prohíbe justo lo que necesitamos para escalar/resiliencia.
  - Endpoint avanzado `/exercises/filter` (name + equipments + targetMuscles + bodyParts + difficulty +
    exerciseTypes en un solo llamado, con fuzzy matching) — pero es de la versión con key.
- **OJO — NO usar datasets de terceros** (repos random tipo `exercisedb-pro/exercisedb-dataset`): sus
  `exerciseId` probablemente NO coinciden con los del OSS de AscendAPI, y ese ID es lo que se guarda en
  `ExerciseDbId` para vincular. Usar otro dataset rompería la coherencia (IDs y URLs de GIF distintos).
  **El seed debe generarse desde la MISMA fuente que consume la app (OSS AscendAPI).**

**Búsqueda por nombre en el OSS:** el endpoint correcto es `exercises/search?search=<texto>&threshold=<0..1>`
(fuzzy). El parámetro de texto es **`search`** (no `q` ni `name`). Devuelve `{success, data:[...]}` pero con
campos reducidos (`exerciseId`, `name`, `imageUrl`) — es tipo autocomplete, requiere un segundo fetch de
detalle para el GIF. NOTA: la búsqueda es en inglés; por eso se prefirió la vinculación visual (Opción A).

---

## 6. Lo que FALTA implementar (roadmap de la Fase B)

Estado: **nada de esto está en el código actual** (se revirtió). El diseño de UI de abajo se llegó a
construir y funcionaba visualmente antes de que el 429 lo tumbara; sirve como referencia.

### B1 — Fuente de datos performante (BLOQUEADOR — resolver primero)
Implementar el **catálogo local seed (JSON)** o una alternativa mejor (ver sección 7). El objetivo es que
la galería "Explorar" cargue en milisegundos, sin 429, sin depender de la API OSS en runtime.
`CatalogoService` debe leer del JSON local (una vez, cachear) y exponer métodos de filtrado/conteo/búsqueda
en memoria (LINQ).

### B2 — Filtros mejorados en "Explorar" (diseño ya definido con Fernando)
Reemplazar la fila única de píldoras por filtros agrupados con jerarquía visual. Requisitos concretos
que pidió Fernando:
1. **Filtro por equipamiento** (`Mancuernas/Dumbbell`, `Barra/Barbell`, `Polea/Cable`, `Máquina/Machine`,
   `Peso corporal/Bodyweight`, `Bandas`). Va en **dropdown** (`<select>`) porque son ~15 valores.
2. **Filtro de músculo específico** (sub-filtro dinámico): al elegir una zona (ej. Upper Legs) desplegar
   Cuádriceps / Isquiotibiales / Glúteos; al elegir Chest → Pectoral Mayor / Superior, etc.
   (deriva de `targetMuscles`).
3. **Barra de búsqueda por texto con autocompletado en tiempo real** (filtrado client-side de las tarjetas
   por nombre; instantáneo; vital para velocidad de registro). ← se construyó y funcionaba (JS + `data-nombre`).
4. **Patrón de movimiento Push/Pull/Legs** (Empuje/Tracción/Pierna/Core). ⚠️ NO existe en la API: hay que
   **derivarlo en C#** con un diccionario `bodyPart → patrón` (chest/shoulders/triceps→Push;
   back/biceps→Pull; upper legs/lower legs→Legs; etc.).
5. **Contadores dinámicos** tipo `Upper Legs (42)` en cada pill. Fernando los quiere **dinámicos**: deben
   reflejar los otros filtros activos (equipment/texto) pero NO el propio bodyPart seleccionado (si no,
   todas las demás zonas mostrarían (0) y no se podría cambiar de zona). Requiere `GroupBy/Count` en memoria.
Estándares UX pedidos: jerarquía visual y agrupación por secciones; estados hover claros; híbrido
pills (acceso rápido) + dropdowns (listas largas); todo respetando `DESIGN-RULES.md`.

### B3 — Botón "Vincular a mis ejercicios" (Opción A)
En cada tarjeta del catálogo Explorar, un botón/acción que permita asociar ese `exerciseId` a uno de los
ejercicios del usuario (elegir de una lista de sus ejercicios). Guardar el `ExerciseDbId` en la fila
correspondiente de `Ejercicio`. También permitir vincular desde crear/editar ejercicio.

### B4 — Mostrar el GIF donde importa
- `Sesiones/Registrar.cshtml` (PRIORIDAD): mostrar el GIF del ejercicio vinculado durante el entrenamiento.
- `Rutinas/Detalle.cshtml`: modal con el GIF.
- Lógica: si `ejercicio.ExerciseDbId != null` → resolver la URL del GIF (desde el seed/caché) y mostrarla.

### Entidades relevantes (actuales)
`Ejercicio` (Id, Nombre, GrupoMuscular enum, Descripcion, UsuarioId, **ExerciseDbId string? ya migrado**),
`Rutina`, `RutinaEjercicio`, `Sesion`, `SerieRealizada`, `Medicion`.

---

## 7. Caminos técnicos a EVALUAR con Claude Code (para no tardar 20s ni chocar con 429)

Claude Code tiene acceso a herramientas que el chat no tenía (puede probar la API en vivo; el chat tenía
`oss.exercisedb.dev` bloqueado en su red). Rutas ordenadas por preferencia:

1. **Seed JSON local, generado con paginación paciente (RECOMENDADO como base).**
   Generar `wwwroot/data/ejercicios.json` UNA vez desde el OSS con delay entre páginas suficientemente alto
   (probar 500ms–1s) + retry con backoff largo ante 429, y **verificar que trae el catálogo COMPLETO**
   (`hasNextPage=false`) antes de escribir. Claude Code puede correr esto desde su entorno/terminal en vez
   de un endpoint en la app. Ventaja: runtime instantáneo, cero API en producción, funciona en AWS y offline.
   Considerar además **descargar los GIFs** a `wwwroot/` para no depender del CDN (ojo licencia/atribución
   OSS) — o dejar los GIFs en el CDN (URLs estables) y solo cachear metadatos en el JSON.

2. **Seed a base de datos en vez de JSON.** Cargar el catálogo en una tabla PostgreSQL (ej. `CatalogoEjercicio`)
   vía un seeder/migración de datos. Ventaja: consultas/filtros con EF Core, joins con `Ejercicio`,
   contadores con SQL. Más "arquitectura de datos" (bueno para la defensa del curso). Ideal si el catálogo
   crece o si se quiere filtrar server-side eficiente.

3. **Probar si el OSS tolera una sola llamada grande.** Verificar si `exercises?limit=1500` (o el máximo real)
   devuelve todo en UNA petición en vez de 60. Si el `limit` máximo real es mayor que 25, una o pocas
   llamadas evitarían el 429 por completo. (En el chat se asumió máx 25; Claude Code debe verificar el
   límite real probando en vivo.)

4. **Evaluar mover el rate-limit fuera del request del usuario.** Un `IHostedService`/`BackgroundService`
   que precargue el catálogo al arrancar la app (con paciencia), de modo que ningún usuario espere los 20s.
   Combinable con seed JSON/DB.

5. **Solo si lo anterior no basta:** reconsiderar RapidAPI Basic gratis (2,000 req/mes) SOLO para generar
   el seed una vez (no en runtime) — pero recordar su "Caching Allowed ✗" y que los `exerciseId` podrían
   diferir de los del OSS (romperían vinculaciones ya guardadas). Mantener coherencia de fuente.

**Regla de oro:** la API externa NO debe estar en el camino crítico de cada request de usuario. El número
de usuarios debe estar DESACOPLADO del consumo de la API (patrón cache-aside / seed). Esto es también el
argumento de arquitectura fuerte para la defensa del curso (resiliencia + escalabilidad + trade-offs
documentados en un ADR-06 sobre caching/seed del catálogo externo).

---

## 8. Estado de git al momento de escribir esto

```
ae1e94c (HEAD -> main, origin/main) Revert "feat(catalogo): vinculacion base, dataset en memoria y filtros"
fa21df0 feat(catalogo): vinculacion base, dataset en memoria y filtros   <- Fase B intentada (revertida)
e2a7c7a feat: catalogo "Explorar ejercicios" con GIFs via ExerciseDB (Integracion 2, Fase A)  <- estado funcional
```
- Código funcional = estado de Fase A. La app "Explorar" carga con GIFs como antes.
- La columna `ExerciseDbId` sigue en la BD (migración aplicada, no revertida). Decidir si mantener.
- El trabajo de Fase B (`fa21df0`) sigue en la historia por si se quiere revivir/consultar con otro revert.

---

## 9. Instrucción sugerida para arrancar con Claude Code

> "Retoma la Integración 2 Fase B de GymTracker. Lee este documento. El bloqueador es el rendimiento del
> catálogo (20s de carga + 429 del OSS). Primero prueba EN VIVO el OSS `oss.exercisedb.dev` para: (a) ver el
> `limit` máximo real por página, (b) medir cuándo dispara 429. Luego propón e implementa la fuente de datos
> performante (evalúa seed JSON local vs seed a PostgreSQL vs una sola llamada grande), confirmando conmigo
> el enfoque antes de codear. Después seguimos con B2 (filtros + contadores dinámicos + búsqueda), B3 (botón
> Vincular) y B4 (GIF en Sesiones y Detalle de rutina). Trabaja paso a paso, explícame el porqué antes del
> código, y respeta DESIGN-RULES.md."