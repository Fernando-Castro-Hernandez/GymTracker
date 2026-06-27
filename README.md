# GymTracker — Rama `04-api`

Esta rama incorpora una **API REST** al proyecto, partiendo del estado de `main`
(MVC + ADR-01). La decisión completa está documentada en
`docs/ADR/ADR-04-Fernando-Castro.md`.

## ¿Qué se hizo?

- Se agregó una **API REST** con ASP.NET Core Web API que expone las entidades
  **Ejercicio** y **Rutina** como endpoints HTTP que devuelven JSON.
- Se configuró **Swagger (OpenAPI)** para documentar y probar los endpoints de
  forma interactiva.
- Las respuestas usan **DTOs** (no las entidades de EF Core directamente), para
  evitar ciclos de serialización y mantener un JSON limpio.

## Endpoints

```
GET /api/ejercicios            → lista de ejercicios
GET /api/ejercicios/{id}       → un ejercicio por id 
GET /api/ejercicios?grupo=...  → lista filtrada por grupo muscular
GET /api/rutinas               → lista de rutinas
GET /api/rutinas/{id}          → una rutina por id 
```



## Uso responsable de IA

En el desarrollo de esta rama se utilizó **Claude** como apoyo para
entender conceptos, redactar el ADR y guiar la implementación paso a paso. Todo el
código fue revisado, comprendido y probado por el autor, y las decisiones de
diseño son propias. La IA se usó como asistente de aprendizaje, manteniendo la
responsabilidad y la autoría del trabajo.
