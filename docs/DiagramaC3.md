# Diagrama C4 — Nivel 3: Componentes

**GymTracker** — Vista de componentes de la Aplicación Web.

Este nivel responde: **¿Qué hay dentro de la pieza principal?**
Hace *zoom-in* sobre el contenedor "Aplicación Web" del Nivel 2 y muestra sus
componentes internos (controllers, servicios, acceso a datos) y cómo colaboran.

```mermaid
flowchart TB
    usuario(["👤 Deportista"])

    subgraph web["Aplicación Web — ASP.NET Core MVC"]
        controllers["<b>Controllers MVC</b><br/><small>Ejercicios, Rutinas, Sesiones,<br/>Mediciones, Progreso</small><br/>Reciben peticiones, validan<br/>ownership y devuelven vistas"]
        apiCtrl["<b>Controllers API</b><br/><small>EjerciciosApi, RutinasApi,<br/>ProgresoApi</small><br/>Exponen datos en JSON"]
        volumen["<b>Servicio de Volumen</b><br/><small>Strategy + Factory (ADR-05)</small><br/>Calcula volumen con<br/>distintas fórmulas"]
        progreso["<b>Servicio de Progreso</b><br/><small>ProgresoService</small><br/>Agrega sesiones y<br/>mediciones para gráficas"]
        efcore["<b>ApplicationDbContext</b><br/><small>EF Core</small><br/>Traduce entidades ↔ BD"]
    end

    db[("<b>PostgreSQL 16</b>")]
    identity["<b>ASP.NET Core Identity</b>"]

    usuario -->|"HTTPS"| controllers
    controllers -->|"Calcula volumen"| volumen
    apiCtrl -->|"Datos de gráficas"| progreso
    controllers -->|"Consulta y guarda"| efcore
    apiCtrl -->|"Consulta"| efcore
    progreso -->|"Consulta"| efcore
    efcore -->|"Lee y escribe (Npgsql)"| db
    controllers -->|"Verifica identidad"| identity

    classDef persona fill:#08427b,stroke:#052e56,color:#fff
    classDef comp fill:#1168bd,stroke:#0b4884,color:#fff
    classDef database fill:#1168bd,stroke:#0b4884,color:#fff
    classDef external fill:#666,stroke:#444,color:#fff
    class usuario persona
    class controllers,apiCtrl,volumen,progreso,efcore comp
    class db database
    class identity external
```

## Para quién es y qué responde

- **Audiencia:** desarrolladores que van a modificar el interior de la aplicación.
- **Pregunta que responde:** ¿qué componentes hay dentro y cómo colaboran?
