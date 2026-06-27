# Plan de integraciones de IA y APIs de terceros — GymTracker

> **Estado: PROPUESTA / FUTURO.** Este documento es una hoja de ruta, no trabajo
> en curso. Las integraciones aquí descritas se implementarán **después** de
> cerrar el plan original (módulos pendientes: Sesiones y Series, Mediciones
> Corporales, Progreso con gráficas) y, según el ADR-03, después o junto con el
> refactor a arquitectura en capas. Nada de esto modifica el modelo de datos ni
> los controllers existentes: todo entra de forma **aditiva**.

Autor: Fernando Castro Hernández — Arquitectura de Software (TSU Desarrollo de Software).

---

## 1. Objetivo

Cumplir con la actividad de "integrar APIs de terceros" añadiendo a GymTracker
funciones que lo distingan de un tracker de gimnasio común, **sin romper** la
arquitectura ni lo que falta por construir. La idea central que da la ventaja
competitiva: usar IA **sobre los propios datos de entrenamiento** (rutinas con
metas, grupos musculares, y el cálculo de volumen del ADR-05), no un chatbot
genérico.

## 1.1 Público objetivo y justificación (validación de negocio)

**Público objetivo: app personal mono-usuario (el atleta).** Coherente con el
ADR-01 y el ADR-03, que ya definen GymTracker como una herramienta personal sin
roles ni multi-tenancy. Las cuatro integraciones se conservan, pero con una
distinción honesta entre su **valor de usuario** y su **valor académico**:

| Integración | Valor de usuario (para el atleta) | Valor académico (lo que demuestra) |
|---|---|---|
| Catálogo con GIFs | **Alto** — resuelve "¿cómo se hace este ejercicio?" desde el día 1 | Consumo de API REST pública de terceros |
| Coach IA (análisis) | **Medio-alto** — evalúa el balance de mis rutinas reales | Integrar un LLM + reutilizar el cálculo de volumen (ADR-05) |
| Generador de rutinas | **Bajo** para un atleta con experiencia; alto para un principiante | **Salida estructurada (JSON) de un LLM** conectada al flujo existente |
| Chatbot con contexto | **Bajo** — muchas preguntas se resuelven con SQL + gráficas | **Manejo de estado conversacional** sobre una API stateless |

**Postura declarada:** las dos primeras se justifican por valor de usuario; las
dos últimas se incluyen principalmente como **demostración técnica** de conceptos
de ingeniería (salida estructurada de LLM y manejo de estado conversacional). Esta
distinción es deliberada y se asume conscientemente; reconocer el valor limitado de
una feature para el caso de uso propio es parte del análisis arquitectónico, no una
debilidad.

## 1.2 Dónde se guardan los datos y escalabilidad

- **Datos generados que vale la pena persistir** (ej. análisis del coach): van a
  **PostgreSQL** como entidades nuevas, cada una con `UsuarioId`, igual que el
  resto del sistema. Patrón ya existente y escalable.
- **Historial del chatbot:** la API de Claude es **stateless** — no recuerda nada
  entre llamadas. El contexto se guarda en una tabla nueva (por `UsuarioId`) y se
  **reenvía completo en cada mensaje**. Cada usuario tendría su propio contexto;
  al ser mono-usuario, en la práctica es el contexto del atleta.
- **Escalabilidad — límite conocido y aceptado.** El sistema no está pensado para
  muchos usuarios concurrentes; los ADR-02 y ADR-03 ya documentan ese trade-off
  (estado de sesión en cookies, despliegue local, escalado horizontal pospuesto).
  Los cuellos de botella de un futuro multi-usuario serían: (1) costo de la API de
  IA, que crece por usuario activo; (2) rate limits del proveedor; (3) crecimiento
  sin límite del historial de chat en BD; (4) estado de sesión en cookies. Ninguno
  es problema hoy; se registran como omisión **deliberada**, no como descuido.

## 2. Idea clave de diseño

Las cuatro funciones propuestas son, en realidad, **dos piezas de código**:

- **Pieza A — Servicio de IA** (`Services/IA/`): cubre Coach IA, Generador de
  rutinas y Chatbot, porque las tres siguen el mismo flujo:
  *armar contexto con mis datos + prompt → llamar al modelo → procesar la
  respuesta*. Solo cambia qué se le pide y cómo se muestra el resultado.
- **Pieza B — Catálogo enriquecido** (`Services/Ejercicios/` o similar): consume
  una API pública de ejercicios (animaciones, músculos, instrucciones). No usa
  IA. Es independiente.

Ambas son hermanas de `Services/Volumen/` y encajan en el patrón que el ADR-03
ya prevé (añadir servicios) y que el ADR-04 ya usa (endpoints `/api/...`).

## 3. Proveedor de IA elegido

**Claude Haiku 4.5** (`claude-haiku-4-5`).

- Precio: **$1.00 / 1M tokens de entrada**, **$5.00 / 1M de salida**.
- Un análisis de rutina ≈ 1–2K tokens de entrada + ~500 de salida → **fracciones
  de centavo** por llamada. Decenas de análisis cuestan centavos.
- Razones de la elección: salida **JSON estructurada** garantizada y *tool use*
  nativos (clave para el Generador de rutinas), buena documentación, y coherencia
  con el proyecto, que ya declara el uso de Claude.
- SDK para .NET: paquete NuGet `Anthropic` (cliente oficial de C#).
- **La API key NUNCA va en el código ni en `appsettings.json` versionado.** Se
  guarda con *User Secrets* en desarrollo (`dotnet user-secrets`) y como variable
  de entorno en producción.

> Alternativas evaluadas y descartadas para esta primera versión: Gemini (tier
> gratuito, buena opción si se quisiera costo cero) y DeepSeek (chino,
> OpenAI-compatible, muy barato). Ambos funcionarían con la misma arquitectura;
> se elige Claude por la salida estructurada y la coherencia del proyecto.

---

## 4. Las cuatro integraciones

### Integración 1 — Coach IA (analizador de rutinas)  ← empezar por aquí

**Qué hace.** Un botón "Analizar con IA" en el Detalle de una rutina. Toma los
ejercicios (series, reps, peso, grupo muscular) **más el volumen ya calculado por
las estrategias del ADR-05** y se lo manda al modelo, que devuelve:
balance muscular (¿descuido espalda?, ¿demasiado pecho?), si el volumen es
adecuado para hipertrofia, y sugerencias concretas.

**Por qué destaca.** Usa datos reales + el cálculo de volumen como contexto. No
es un chat genérico: es un análisis específico de *esa* rutina.

**Encaje arquitectónico.** Servicio nuevo en `Services/IA/`. Puede reutilizar el
patrón **Strategy + Factory** del ADR-05 para tener varias "personalidades" de
coach (enfoque fuerza vs. hipertrofia), reforzando lo ya enseñado. Se consume
desde un endpoint nuevo (ej. `GET /api/rutinas/{id}/analisis`) o una acción MVC.

**Esfuerzo estimado.** Bajo–medio. Es la mejor primera fase: autocontenida y muy
demostrable.

---

### Integración 2 — Catálogo de ejercicios con GIFs e info (API pública)

**Qué hace.** Al crear o ver un ejercicio, consulta una API pública para mostrar
una **animación (GIF)**, los **músculos trabajados** e **instrucciones** de
técnica. Puede autocompletar el grupo muscular.

**API candidata.** **wger** (open-source, gratuita, sin tarjeta) o **ExerciseDB**
(vía RapidAPI). Preferir wger por ser gratis y sin registro complejo.

**Por qué destaca.** Impacto visual inmediato; el cambio se nota a simple vista.

**Encaje arquitectónico.** Servicio que hace una petición HTTP a la API externa
(usando `IHttpClientFactory`), consumido desde el controller de Ejercicios. Sin
IA, sin costo. Independiente del resto.

**Esfuerzo estimado.** Bajo. Buena segunda fase (o primera si se quiere algo
visual sin tocar IA).

---

### Integración 3 — Generador de rutinas con IA

**Qué hace.** El usuario describe su objetivo ("hipertrofia tren superior, 4
días") y el modelo propone una rutina **usando solo los ejercicios del catálogo
del usuario**. El usuario la revisa y la guarda.

**Detalle técnico elegante.** Se le pasa al modelo la lista de ejercicios del
usuario y se pide **salida estructurada (JSON)**: ids + series/reps/peso. Ese
JSON conecta directo con el `CrearRutinaViewModel` que ya existe. Demuestra
dominio de "salida estructurada de LLM".

**Encaje arquitectónico.** Mismo `Services/IA/` que la Integración 1; cambia el
prompt y el esquema de salida. La rutina propuesta entra por el flujo de creación
de rutinas ya existente.

**Esfuerzo estimado.** Medio. Depende de tener el catálogo y el flujo de rutinas
estables (parte del plan original).

---

### Integración 4 — Chatbot con contexto de entrenamiento

**Qué hace.** Un chat donde el usuario pregunta "¿qué entrené más esta semana?",
"¿cómo mejoro mi rutina de pierna?", y el modelo responde **con acceso a sus
rutinas, ejercicios y (cuando exista) sus sesiones**.

**Por qué destaca.** Es el formato del proyecto del compañero (Gemini chatbot),
pero diferenciado por responder sobre datos reales del usuario.

**Encaje arquitectónico.** Reutiliza `Services/IA/`. Conviene implementarlo
**al final**, cuando ya existan Sesiones y Progreso, porque ahí el chatbot tiene
datos ricos de los que hablar. Antes de eso aportaría poco.

**Esfuerzo estimado.** Medio–alto (manejo de conversación e historial).

---

## 5. Orden de implementación sugerido (por fases)

1. **Fase 0 (prerequisito):** terminar el plan original — Sesiones/Series,
   Mediciones, Progreso. (Y, según ADR-03, el refactor a capas.)
2. **Fase 1 — Coach IA** (Integración 1): la más diferenciadora y autocontenida.
3. **Fase 2 — Catálogo con GIFs** (Integración 2): impacto visual, sin costo.
4. **Fase 3 — Generador de rutinas** (Integración 3): salida estructurada.
5. **Fase 4 — Chatbot** (Integración 4): cuando ya haya datos de sesiones.

Cada fase es un avance independiente, con su rama y (si cambia una decisión
arquitectónica) su ADR.

## 6. Consideraciones transversales

- **Seguridad de la API key:** User Secrets en dev, variable de entorno en prod.
  Nunca en el repositorio.
- **Costo controlado:** Haiku 4.5 es barato; aun así, limitar tamaño de prompts y
  considerar cachear análisis para no re-llamar al modelo en cada vista.
- **La IA no decide sola:** las rutinas/análisis generados son **sugerencias** que
  el usuario revisa y acepta. Coherente con la declaración de uso responsable de
  IA del proyecto.
- **Nuevo ADR cuando toque:** incorporar un proveedor de IA es una decisión
  arquitectónica → cuando se implemente la Fase 1, redactar un **ADR-06**
  (integración de IA / API de terceros) siguiendo el formato de los anteriores.
- **Documentar el uso de IA del producto** (que la app *usa* un modelo) es
  distinto de la **declaración de uso de IA en el desarrollo**; ambas deben quedar
  claras.
