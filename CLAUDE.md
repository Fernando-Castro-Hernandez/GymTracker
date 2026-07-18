# CLAUDE.md — GymTracker

Contexto del proyecto para Claude Code. Léelo antes de hacer cambios.

## Qué es este proyecto

GymTracker es una aplicación web personal para el seguimiento de entrenamientos
de gimnasio. Cubre el ciclo completo de la sobrecarga progresiva: catálogo de
ejercicios, rutinas con metas (series, repeticiones, peso), registro de sesiones
de entrenamiento reales, mediciones corporales y gráficas de progreso. Es un
proyecto académico de la materia de Arquitectura de Software (TSU Desarrollo de
Software).

Autor: Fernando Castro Hernández.

## Stack tecnológico

- **ASP.NET Core 10 MVC** (.NET 10) — aplicación web monolítica, HTML renderizado
  en el servidor con vistas Razor.
- **Entity Framework Core 10** como ORM.
- **PostgreSQL 16**, corriendo en un contenedor Docker (`gymtracker-db`).
- **ASP.NET Core Identity** para autenticación (cookies de sesión).
- **Bootstrap 5** para el front-end (ya incluido en el proyecto).
- **API REST** con ASP.NET Core Web API + **Swagger/OpenAPI** (Swashbuckle).
- **Chart.js 4.4.1** para las gráficas de progreso, servido localmente desde
  `wwwroot/lib/chart/chart.min.js` (no por CDN, para coherencia y uso offline).

## Cómo correr el proyecto

0. Configurar secretos la primera vez (no se versionan; ver "Manejo de secretos"
   más abajo): crear `.env` a partir de `.env.example` con `POSTGRES_PASSWORD`, y
   registrar la connection string completa y las API keys en User Secrets.
1. Levantar la base de datos: `docker compose up -d` (el contenedor se detiene
   entre sesiones; si la app da error de conexión, es que el contenedor está
   apagado). El contenedor se publica en el puerto **5433** del host (no 5432),
   para no chocar con un PostgreSQL nativo de Windows que suele ocupar el 5432.
2. Aplicar migraciones si hace falta. Como ahora hay 4 proyectos (ADR-03), el
   `DbContext` vive en `GymTracker.Infrastructure` y el arranque en
   `GymTracker.Web`, así que los comandos `dotnet ef` requieren los flags:
   `dotnet ef database update --project GymTracker.Infrastructure --startup-project GymTracker.Web`.
   IMPORTANTE: detener la app antes de correr comandos `dotnet ef` o `dotnet
   build`, porque IIS Express bloquea el .dll mientras la app está en ejecución.
3. Correr: `dotnet run --project GymTracker.Web` o F5 en Visual Studio. El
   `dotnet build` se hace sobre la solución (`GymTracker.slnx`).
4. App en `https://localhost:44353`, Swagger en `https://localhost:44353/swagger`.

Credenciales de la base de datos: usuario `gymtracker_user`, base `gymtracker`.
La contraseña vive en `.env` (Docker) y en User Secrets (la app), nunca en el
repo. Para inspeccionar la BD desde dentro del contenedor (no necesita puerto ni
contraseña por el socket local):
`docker exec -it gymtracker-db psql -U gymtracker_user -d gymtracker`.

## Estructura del proyecto

Desde la implementación del **ADR-03** (rama `arquitectura-capas`), la solución
está organizada en **4 proyectos separados** (`GymTracker.slnx` en la raíz), con
dependencias `Web → Application → Domain` e `Infrastructure` proveyendo la
persistencia. IMPORTANTE: los namespaces se **conservaron** (`GymTracker.Models`,
`GymTracker.Data`, `GymTracker.Services`, `GymTracker.DTOs`) aunque los archivos
vivan en proyectos distintos, para no alterar el snapshot de EF Core.

```
GymTracker/  (raíz del repo)
├── GymTracker.slnx                 # Solución con los 4 proyectos
│
├── GymTracker.Domain/              # Núcleo, sin dependencias
│   └── Models/                     #   Entidades (Ejercicio, Rutina, RutinaEjercicio,
│       └── Enums/                  #   Sesion, SerieRealizada, Medicion) + GrupoMuscular
│
├── GymTracker.Application/         # Lógica de negocio. → Domain
│   ├── Abstractions/               #   IApplicationDbContext, ISeedFileProvider
│   ├── DTOs/                       #   Respuestas de la API (EjercicioDto, RutinaDto, ...)
│   ├── DependencyInjection.cs      #   AddApplication(config): registra los servicios
│   └── Services/
│       ├── Ejercicios/ Rutinas/    #   Servicios de dominio: I{X}Service + {X}Service
│       │   Sesiones/ Mediciones/   #   (consultas, guardado, filtro de ownership)
│       ├── Volumen/                #   Strategy + Factory Method (ADR-05)
│       ├── Progreso/               #   ProgresoService (datos de las gráficas)
│       ├── IA/                     #   Coach IA + Chatbot (IProveedorIA, Claude/Gemini,
│       │                           #   fallback; ChatService, ContextoChatBuilder,
│       │                           #   GuardarrielChat, RouterContexto — ADR-07)
│       └── Catalogo/               #   CatalogoService (seed local con GIFs)
│
├── GymTracker.Infrastructure/      # Persistencia. → Domain, Application
│   ├── Data/                       #   ApplicationDbContext (implementa IApplicationDbContext)
│   ├── Migrations/                 #   Migraciones de EF Core
│   └── DependencyInjection.cs      #   AddInfrastructure(cs): DbContext + Npgsql
│
└── GymTracker.Web/                 # Presentación (composition root). → Application, Infrastructure
    ├── Controllers/                #   MVC: Home, Ejercicios, Rutinas, Sesiones, Mediciones, Progreso
    │   └── Api/                    #   API REST: EjerciciosApi, RutinasApi, ProgresoApi, CoachApi, ChatApi
    ├── ViewModels/                 #   Modelos de presentación (CrearRutinaViewModel, ...)
    ├── Views/                      #   Vistas Razor
    ├── Areas/Identity/             #   Login/registro (scaffolded)
    ├── Services/                   #   WebSeedFileProvider (implementa ISeedFileProvider)
    ├── SeedData/                   #   exercises.json (seed del catálogo, en el content root)
    ├── wwwroot/                    #   Estáticos: css/, js/, lib/ (Bootstrap 5, Chart.js)
    └── Program.cs                  #   AddInfrastructure(...) + AddApplication(...)

docs/                               # ADRs y documentación (en la raíz del repo)
```

## Funcionalidades implementadas

- **Catálogo de ejercicios** (CRUD): biblioteca personal por grupo muscular.
- **Rutinas con metas** (CRUD): asignación dinámica de ejercicios con series,
  reps y peso objetivo. La asignación usa una tabla interactiva en JavaScript
  que envía JSON al servidor (en Agregar y Editar de Rutinas).
- **Sesiones de entrenamiento**: al iniciar una sesión desde una rutina, se
  CONGELA un snapshot (nombre de rutina, ejercicios, grupo muscular y metas del
  momento) para que el historial sea inmutable aunque la rutina cambie después.
  Se registran los valores reales (reps y peso) de cada serie ejecutada.
- **Mediciones corporales** (CRUD): peso obligatorio + composición corporal
  (%grasa, grasa visceral, masa muscular, %agua) y medidas con cinta, todas
  opcionales. No se guarda IMC (es un valor derivado; se calcularía al vuelo).
- **Progreso**: tres gráficas Chart.js (peso corporal, volumen por sesión y
  progresión de carga por ejercicio) alimentadas por endpoints de API vía fetch.
- **Chatbot con contexto** (Coach conversacional, ADR-07): widget flotante que
  responde sobre los datos reales del usuario (rutinas, sesiones, volumen,
  mediciones). Pipeline de LLM con contexto podado (sin RAG), guardarrieles,
  router de contexto, prompt caching y observabilidad. Historial en `ChatMensajes`.

## Arquitectura y decisiones (ADR)

El proyecto documenta sus decisiones en `docs/ADR/`. Resumen:

- **ADR-01** — Patrón MVC con ASP.NET Core como arquitectura base.
- **ADR-02** — Vistas arquitectónicas y trade-offs.
- **ADR-03** — Arquitectura en capas con proyectos separados. **IMPLEMENTADO**
  (rama `arquitectura-capas`): la solución tiene 4 proyectos (Domain, Application,
  Infrastructure, Web) y **los controllers ya NO acceden al `ApplicationDbContext`
  directamente**: la lógica de datos vive en servicios de la capa Application,
  inyectados por DI y dependientes de `IApplicationDbContext`. Ver la estructura
  arriba y la sección "Implementación" del ADR-03.
- **ADR-04** — Incorporación de la API REST.
- **ADR-05** — Patrones de diseño GOF: Strategy + Factory Method para el cálculo
  de volumen de entrenamiento (sobre las metas de la rutina). La gráfica de
  volumen real reutiliza el mismo concepto de tonelaje sobre series ejecutadas.
- **ADR-06** — Registro de deuda técnica. Deuda #1 (credenciales en el historial,
  mitigada) y Deuda #2 (acceso directo al `DbContext` desde los controllers,
  **pagada** con el ADR-03).
- **ADR-07** — Arquitectura del Chatbot con contexto (pipeline de LLM).
  **IMPLEMENTADO** (rama `chatbot-ia`): construcción de contexto SIN RAG (retrieval
  SQL + poda por ventana de tiempo), guardarrieles en capas (system prompt como
  defensa real), router de *contexto* (no de modelo), prompt caching de Anthropic y
  observabilidad de tokens/latencia. Descarta a conciencia RAG semántico y
  capacidades agénticas.

### Integraciones de IA (Coach y Chatbot)

El **Coach IA está implementado** en `Services/IA/`. Analiza una rutina (balance
muscular y volumen) usando un LLM y devuelve recomendaciones. Diseño:

- `IProveedorIA` — interfaz común para los proveedores de LLM (métodos
  `AnalizarRutinaAsync` para el Coach y `ChatearAsync` para el Chatbot).
- `ClaudeProveedor` — proveedor principal, Claude Haiku vía el SDK oficial de
  Anthropic. Usa "prompted JSON" con parseo defensivo.
- `GeminiProveedor` — proveedor de fallback, Gemini Flash vía `Google.GenAI`.
- `ProveedorIAConFallback` — orquesta el orden Claude → Gemini: si el primero
  falla, cae al siguiente.
- `CoachService` — orquestador que prepara los datos de la rutina y llama al
  proveedor.

El **Chatbot con contexto está implementado** (Integración 4, **ADR-07**): un
widget flotante (`_ChatWidget.cshtml` + `chat.js`, solo para autenticados) que
responde sobre los datos reales del usuario. Reutiliza el gateway `IProveedorIA`
con fallback. Piezas en `Services/IA/`: `ChatService` (orquesta el pipeline y
persiste en `ChatMensajes`), `ContextoChatBuilder` (contexto podado, sin RAG),
`GuardarrielChat` (validación de entrada), `RouterContexto` (heurística de contexto).
Expuesto en `ChatApiController` (`/api/chat/*`, con rate limiting por usuario).

Las **API keys nunca van en el código ni en `appsettings.json`**: se leen de
configuración (`Anthropic:ApiKey`, `Gemini:ApiKey`) en `Program.cs`, con User
Secrets en desarrollo y variables de entorno en producción.

El **catálogo enriquecido con GIFs ya está implementado** vía un seed local
(`SeedData/exercises.json`, 1324 ejercicios), sin llamar a APIs de terceros en
runtime. El **Chatbot con contexto (Integración 4) también está implementado**
(ADR-07). El resto de la hoja de ruta (generador de rutinas con IA, etc.) sigue
como propuesta en `docs/PLAN-integraciones-IA.md` y no debe implementarse sin
planeación explícita.

## Manejo de secretos

Ningún secreto se versiona. Reglas:

- **Contraseña de PostgreSQL:** `docker-compose.yml` la lee de `${POSTGRES_PASSWORD}`,
  definida en un `.env` local (ignorado por git; hay un `.env.example` versionado
  como guía). La app la recibe en la connection string **completa** guardada en
  User Secrets (`ConnectionStrings:DefaultConnection`), que sobrescribe la de
  `appsettings.json` (esa va sin contraseña, solo como referencia).
- **API keys de LLMs:** solo en User Secrets (dev) / variables de entorno (prod).
- `appsettings.json` y `docker-compose.yml` **no deben** contener contraseñas ni
  keys en texto plano. Al añadir un secreto nuevo, seguir este mismo patrón.

## Convenciones importantes

- El proyecto usa **constructores primarios** de C# para inyección de
  dependencias (ej. `public class RutinasController(ApplicationDbContext context)`).
- Cada controller filtra por `UsuarioId` (obtenido de Identity con
  `User.FindFirstValue(ClaimTypes.NameIdentifier)`), de modo que cada usuario
  solo ve y edita sus propios datos. Este filtro de ownership es obligatorio en
  toda consulta a datos del usuario.
- Los nombres de tablas en PostgreSQL conservan mayúscula inicial
  (`"Ejercicios"`, `"Sesiones"`), por lo que requieren comillas dobles en SQL.
- Los `DateTime` se guardan en **UTC** en la base de datos (PostgreSQL exige UTC
  para `timestamp with time zone`). Al recibir fechas de formularios se
  normalizan con `SpecifyKind(..., Local).ToUniversalTime()`; al mostrar se usa
  `.ToLocalTime()`.
- La API devuelve **DTOs**, nunca las entidades de EF Core directamente, para
  evitar ciclos de serialización (las entidades tienen navegaciones cruzadas).
- Los endpoints de catálogo de la API son públicos; el `ProgresoApiController`
  lleva `[Authorize]` porque expone datos personales. Esto es deliberado
  (seguridad contextual según la sensibilidad del dato).
- Control de versiones: se fusionó todo a `main`. Las ramas con ADR se reservan
  para decisiones arquitectónicas nuevas no previstas. Commits pequeños y
  descriptivos (no un solo commit gigante), uno por avance lógico.

## Reglas para hacer cambios

### Lo que es SEGURO modificar (capa visual / presentación)
- `wwwroot/css/site.css` y demás CSS propio.
- `Views/Shared/_Layout.cshtml` (navbar, footer, estructura general).
- El HTML y las clases de estilo (Bootstrap) dentro de las vistas `.cshtml`.
- `wwwroot/js/site.js`.

### Lo que NO se debe tocar sin confirmación explícita
- **Los proyectos de capa `GymTracker.Domain`, `GymTracker.Application` e
  `GymTracker.Infrastructure`** (entidades, servicios, DTOs, `ApplicationDbContext`,
  migraciones), y **los Controllers** de `GymTracker.Web`: contienen la lógica y el
  acceso a datos. No modificar al hacer cambios de diseño visual.
- Dentro de las vistas, **no eliminar ni alterar**:
  - Las directivas `@model`, `@foreach`, `@if`, `@section`.
  - Los atributos `asp-for`, `asp-action`, `asp-controller`, `asp-route-*`,
    `asp-items`, `asp-page`, `asp-validation-*`.
  - Los `name` indexados de model binding de listas (ej. `Series[0].PesoReal`)
    en `Views/Sesiones/Registrar.cshtml`. Cambiarlos rompe el guardado.
  Son el cableado que conecta la vista con los controllers y los datos. Se puede
  reestilizar el elemento que los contiene, pero estos atributos deben permanecer.
- Los bloques `<script>` que contienen lógica de negocio con `fetch`, IDs de
  elementos o llamadas a la API. En particular:
  - `Views/Rutinas/Agregar.cshtml` y `Views/Rutinas/Editar.cshtml` (asignación
    dinámica de ejercicios).
  - `Views/Progreso/Index.cshtml` (fetch a `/api/progreso/*` y Chart.js). No
    cambiar los `id` de los `<canvas>` ni las URLs de la API.
  Se puede cambiar el CSS y el HTML alrededor, pero la lógica JS debe seguir igual.
- La configuración de la API, Identity y Swagger en `GymTracker.Web/Program.cs`, y
  el registro de servicios en los métodos `AddApplication(...)`
  (`GymTracker.Application/DependencyInjection.cs`) y `AddInfrastructure(...)`
  (`GymTracker.Infrastructure/DependencyInjection.cs`).

### Al terminar cualquier cambio
- Confirmar que la solución **compila** (`dotnet build GymTracker.slnx`, con la app
  detenida).
- Verificar que ningún comportamiento cambió: login, CRUD de ejercicios, rutinas,
  mediciones; iniciar y registrar una sesión; la asignación dinámica de
  ejercicios; las tres gráficas de progreso; y que la API + Swagger respondan.
- Hacer commits pequeños y descriptivos.

## Nota sobre el rediseño visual

Cualquier mejora de diseño es **puramente visual** y no cambia la arquitectura,
por lo que **no requiere un ADR nuevo**. Mantener Bootstrap 5 como base (no
cambiar de framework de CSS). Preferir cambios por pantalla en lugar de todo de
golpe. No introducir dependencias nuevas de front-end sin confirmación.