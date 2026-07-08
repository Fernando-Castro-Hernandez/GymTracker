# Diagrama C4 — Nivel 1: Contexto del Sistema

**GymTracker** — Bitácora personal de entrenamiento de gimnasio.

Este nivel responde: **¿Qué es el sistema y quién lo usa?**
Es la vista más general. No muestra tecnología: solo el sistema como una caja y
los actores que interactúan con él. Cualquier persona, técnica o no, debe poder
entenderlo.

```mermaid
flowchart TB
    usuario(["👤 Deportista<br/><small>Entrena en el gimnasio y quiere<br/>registrar y medir su progreso</small>"])
    sistema["<b>GymTracker</b><br/><small>Aplicación web personal para registrar<br/>ejercicios, rutinas, sesiones, mediciones<br/>y visualizar el progreso</small>"]

    usuario -->|"Registra entrenamientos y<br/>consulta su progreso (HTTPS)"| sistema

    classDef persona fill:#08427b,stroke:#052e56,color:#fff
    classDef software fill:#1168bd,stroke:#0b4884,color:#fff
    class usuario persona
    class sistema software
```

## Para quién es y qué responde

- **Audiencia:** cualquiera (usuario final, profesor, alguien ajeno al proyecto).
- **Pregunta que responde:** ¿qué hace el sistema y quién lo usa?
