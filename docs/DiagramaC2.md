# Diagrama C4 — Nivel 2: Contenedores

**GymTracker** — Vista de contenedores.

Este nivel responde: **¿De qué piezas grandes se compone el sistema?**
Hace *zoom-in* sobre la caja de GymTracker del Nivel 1 y muestra las unidades
desplegables (aplicaciones, base de datos) y cómo se comunican. Aquí ya aparece
la tecnología.

```mermaid
flowchart TB
    usuario(["👤 Deportista"])

    subgraph gt["GymTracker"]
        web["<b>Aplicación Web</b><br/><small>ASP.NET Core 10 MVC + Razor</small><br/>Sirve páginas, procesa formularios<br/>y contiene la lógica de la app"]
        api["<b>API REST</b><br/><small>ASP.NET Core Web API</small><br/>Endpoints JSON para el catálogo<br/>y los datos de las gráficas"]
        db[("<b>Base de Datos</b><br/><small>PostgreSQL 16</small><br/>Usuarios, ejercicios, rutinas,<br/>sesiones, series y mediciones")]
    end

    identity["<b>ASP.NET Core Identity</b><br/><small>Autenticación por cookies</small>"]

    usuario -->|"Usa (HTTPS)"| web
    web -->|"Consume para las<br/>gráficas (fetch/JSON)"| api
    web -->|"Lee y escribe<br/>(EF Core / Npgsql)"| db
    api -->|"Lee (EF Core / Npgsql)"| db
    web -->|"Autentica (cookies)"| identity

    classDef persona fill:#08427b,stroke:#052e56,color:#fff
    classDef software fill:#1168bd,stroke:#0b4884,color:#fff
    classDef database fill:#1168bd,stroke:#0b4884,color:#fff
    classDef external fill:#666,stroke:#444,color:#fff
    class usuario persona
    class web,api software
    class db database
    class identity external
```

## Para quién es y qué responde

- **Audiencia:** equipo técnico, quien despliega o mantiene el sistema.
- **Pregunta que responde:** ¿qué aplicaciones/almacenes hay y cómo se comunican?
- **Notas:** la API vive dentro de la misma aplicación ASP.NET Core (no es un
  servicio separado), pero se modela como contenedor lógico distinto porque
  cumple un rol propio: exponer datos JSON para las visualizaciones. La
  autenticación se apoya en Identity, que persiste sus tablas en la misma BD.
