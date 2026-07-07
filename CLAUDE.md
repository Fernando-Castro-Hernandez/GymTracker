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

1. Levantar la base de datos: `docker compose up -d` (el contenedor se detiene
   entre sesiones; si la app da error de conexión al puerto 5432, es que el
   contenedor está apagado).
2. Aplicar migraciones si hace falta: `dotnet ef database update`.
   IMPORTANTE: detener la app antes de correr comandos `dotnet ef` o `dotnet
   build`, porque IIS Express bloquea el .dll mientras la app está en ejecución.
3. Correr: `dotnet run` o F5 en Visual Studio.
4. App en `https://localhost:44353`, Swagger en `https://localhost:44353/swagger`.

Credenciales de la base de datos (docker-compose): usuario `gymtracker_user`,
base `gymtracker`. Para inspeccionar la BD:
`docker exec -it gymtracker-db psql -U gymtracker_user -d gymtracker`.

## Estructura del proyecto

```
GymTracker/
├── Controllers/          # MVC: Home, Ejercicios, Rutinas, Sesiones, Mediciones, Progreso
│   └── Api/              # API REST: EjerciciosApi, RutinasApi, ProgresoApi
├── Models/              # Entidades: Ejercicio, Rutina, RutinaEjercicio,
│   │                    #   Sesion, SerieRealizada, Medicion
│   ├── Enums/           # GrupoMuscular
│   └── ViewModels/      # CrearRutinaViewModel, EditarRutinaViewModel,
│                        #   EjercicioEnRutinaViewModel, RegistrarSesionViewModel,
│                        #   SerieEditableViewModel
├── DTOs/                # Respuestas de la API (JSON limpio): EjercicioDto, RutinaDto,
│                        #   RutinaEjercicioDto, VolumenDto, PuntoProgresoDto, SerieProgresoDto
├── Services/
│   ├── Volumen/         # Patrones GOF: Strategy (ICalculoVolumen + 3 estrategias)
│   │                    #   y Factory Method (CalculoVolumenFactory) — ADR-05
│   └── Progreso/        # ProgresoService: agregación de datos para las gráficas
├── Data/                # ApplicationDbContext (EF Core)
├── Migrations/          # Migraciones de EF Core
├── Views/               # Vistas Razor (Ejercicios/, Rutinas/, Sesiones/,
│                        #   Mediciones/, Progreso/, Home/, Shared/)
├── Areas/Identity/      # Páginas de login/registro (scaffolded)
├── docs/                # ADRs y documentación (incluye PLAN-integraciones-IA.md)
└── wwwroot/             # Estáticos: css/, js/, lib/ (Bootstrap 5, Chart.js)
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

## Arquitectura y decisiones (ADR)

El proyecto documenta sus decisiones en `docs/ADR/`. Resumen:

- **ADR-01** — Patrón MVC con ASP.NET Core como arquitectura base.
- **ADR-02** — Vistas arquitectónicas y trade-offs.
- **ADR-03** — Decisión (propuesta, aún no implementada) de migrar a una
  arquitectura en capas con proyectos separados. **El código actual sigue siendo
  un monolito de un solo proyecto**; los controllers acceden al
  `ApplicationDbContext` directamente. El ADR sí prevé añadir servicios, patrón
  ya usado por `Services/Volumen/` y `Services/Progreso/`.
- **ADR-04** — Incorporación de la API REST.
- **ADR-05** — Patrones de diseño GOF: Strategy + Factory Method para el cálculo
  de volumen de entrenamiento (sobre las metas de la rutina). La gráfica de
  volumen real reutiliza el mismo concepto de tonelaje sobre series ejecutadas.

Las integraciones de IA y API de terceros (Coach IA, catálogo enriquecido, etc.)
están documentadas como propuesta futura en `docs/PLAN-integraciones-IA.md`. NO
están implementadas y no deben implementarse sin planeación explícita.

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
- **Controllers, Models, Data, Migrations, DTOs y Services**: contienen la lógica
  y el acceso a datos. No modificar al hacer cambios de diseño.
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
- La configuración de la API, Identity y Swagger en `Program.cs`, y el registro
  de servicios (`AddScoped`).

### Al terminar cualquier cambio
- Confirmar que el proyecto **compila** (`dotnet build`, con la app detenida).
- Verificar que ningún comportamiento cambió: login, CRUD de ejercicios, rutinas,
  mediciones; iniciar y registrar una sesión; la asignación dinámica de
  ejercicios; las tres gráficas de progreso; y que la API + Swagger respondan.
- Hacer commits pequeños y descriptivos.

## Nota sobre el rediseño visual

Cualquier mejora de diseño es **puramente visual** y no cambia la arquitectura,
por lo que **no requiere un ADR nuevo**. Mantener Bootstrap 5 como base (no
cambiar de framework de CSS). Preferir cambios por pantalla en lugar de todo de
golpe. No introducir dependencias nuevas de front-end sin confirmación.