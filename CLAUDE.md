# CLAUDE.md — GymTracker

Contexto del proyecto para Claude Code. Léelo antes de hacer cambios.

## Qué es este proyecto

GymTracker es una aplicación web personal para el seguimiento de entrenamientos
de gimnasio: catálogo de ejercicios, rutinas con metas (series, repeticiones,
peso) y, a futuro, sesiones y progreso. Es un proyecto académico de la materia
de Arquitectura de Software (TSU Desarrollo de Software).

Autor: Fernando Castro Hernández.

## Stack tecnológico

- **ASP.NET Core 10 MVC** (.NET 10) — aplicación web monolítica, HTML renderizado
  en el servidor con vistas Razor.
- **Entity Framework Core 10** como ORM.
- **PostgreSQL 16**, corriendo en un contenedor Docker (`gymtracker-db`).
- **ASP.NET Core Identity** para autenticación (cookies de sesión).
- **Bootstrap 5** para el front-end (ya incluido en el proyecto).
- **API REST** con ASP.NET Core Web API + **Swagger/OpenAPI** (Swashbuckle).

## Cómo correr el proyecto

1. Levantar la base de datos: `docker compose up -d` (el contenedor se detiene
   entre sesiones; si la app da error de conexión al puerto 5432, es que el
   contenedor está apagado).
2. Aplicar migraciones si hace falta: `dotnet ef database update`.
3. Correr: `dotnet run` o F5 en Visual Studio.
4. App en `https://localhost:7192`, Swagger en `https://localhost:7192/swagger`.

Cuenta de prueba: `fer@gmail.com`.

## Estructura del proyecto

```
GymTracker/
├── Controllers/          # MVC: HomeController, EjerciciosController, RutinasController
│   └── Api/              # API REST: EjerciciosApiController, RutinasApiController
├── Models/              # Entidades de dominio: Ejercicio, Rutina, RutinaEjercicio
│   ├── Enums/           # GrupoMuscular
│   └── ViewModels/      # CrearRutinaViewModel, EditarRutinaViewModel, etc.
├── DTOs/                # Objetos de respuesta de la API (JSON limpio)
├── Services/
│   └── Volumen/         # Patrones GOF: Strategy (ICalculoVolumen + 3 estrategias)
│                        # y Factory Method (CalculoVolumenFactory)
├── Data/                # ApplicationDbContext (EF Core)
├── Migrations/          # Migraciones de EF Core
├── Views/               # Vistas Razor (Ejercicios/, Rutinas/, Home/, Shared/)
├── Areas/Identity/      # Páginas de login/registro (scaffolded)
└── wwwroot/             # Archivos estáticos: css/, js/, lib/ (Bootstrap 5)
```

## Arquitectura y decisiones (ADR)

El proyecto documenta sus decisiones en `docs/ADR/`. Resumen:

- **ADR-01** — Patrón MVC con ASP.NET Core como arquitectura base.
- **ADR-02** — Vistas arquitectónicas y trade-offs.
- **ADR-03** — Decisión (propuesta, aún no implementada) de migrar a una
  arquitectura en capas con proyectos separados. **El código actual sigue siendo
  un monolito de un solo proyecto**; los controllers acceden al
  `ApplicationDbContext` directamente.
- **ADR-04** — Incorporación de la API REST.
- **ADR-05** — Patrones de diseño GOF: Strategy + Factory Method para el cálculo
  de volumen de entrenamiento.

## Convenciones importantes

- El proyecto usa **constructores primarios** de C# para inyección de
  dependencias (ej. `public class RutinasController(ApplicationDbContext context)`).
- Los nombres de tablas en PostgreSQL conservan mayúscula inicial
  (`"Ejercicios"`, `"Rutinas"`), por lo que requieren comillas dobles en SQL.
- La API devuelve **DTOs**, nunca las entidades de EF Core directamente, para
  evitar ciclos de serialización (las entidades tienen navegaciones cruzadas).
- Control de versiones con **ramas acumulativas** (una por avance, con su ADR):
  `main` → `04-api` → `05-patrones` → etc. Cada cambio va en commits separados y
  descriptivos (no un solo commit gigante).

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
  - Las directivas `@model`, `@foreach`, `@if`.
  - Los atributos `asp-for`, `asp-action`, `asp-controller`, `asp-route-*`,
    `asp-items`, `asp-page`.
  Son el cableado que conecta la vista con los controllers y los datos. Se puede
  reestilizar el elemento que los contiene, pero los atributos deben permanecer.
- El bloque `<script>` de `Views/Rutinas/Agregar.cshtml`: contiene la lógica de
  asignación dinámica de ejercicios con `fetch`. Debe seguir funcionando igual.
- La configuración de la API y Swagger en `Program.cs`.

### Al terminar cualquier cambio
- Confirmar que el proyecto **compila** (`dotnet build`).
- Verificar que ningún comportamiento cambió: login, crear/editar/eliminar
  ejercicios y rutinas, la asignación dinámica de ejercicios, y que la API +
  Swagger sigan respondiendo.
- Trabajar en una rama dedicada y hacer commits pequeños y descriptivos.

## Nota sobre el rediseño visual

Cualquier mejora de diseño es **puramente visual** y no cambia la arquitectura,
por lo que **no requiere un ADR nuevo**. Mantener Bootstrap 5 como base (no
cambiar de framework de CSS). Preferir cambios por pantalla (layout → ejercicios
→ rutinas) en lugar de todo de golpe.
