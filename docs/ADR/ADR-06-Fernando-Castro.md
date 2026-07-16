# ADR-06: Registro de deuda técnica (credenciales en el historial y acceso directo a datos desde los controllers)

| Campo  | Valor |
|--------|-------|
| Autor  | Fernando Castro Hernández |
| Fecha  | 15/07/2026 |
| Estado | Aceptado |

---

## Contexto

Este ADR no propone una arquitectura nueva: **documenta deuda técnica ya
existente** en GymTracker, siguiendo la práctica de que el problema de la deuda no
es tenerla, sino no saber que existe. Se registran dos deudas concretas del
proyecto —una de infraestructura/configuración y una de diseño de código—, cada
una con qué es, por qué existe, el costo de dejarla crecer y la propuesta de
solución. Hacerlas explícitas permite decidir de forma consciente cuándo pagarlas.

---

## Deuda 1 — Contraseña de PostgreSQL en el historial de git (infraestructura/configuración)

### Qué es
La contraseña de desarrollo de PostgreSQL (`dev_local_password_2026`) estuvo
escrita en texto plano en `appsettings.json` y `docker-compose.yml`, ambos
versionados. Se sacó de esos archivos y se movió a User Secrets y a un `.env`
ignorado por git, y la contraseña se rotó con `ALTER USER`. Sin embargo, **el
valor viejo sigue presente en commits antiguos del historial** (visible desde el
commit que introdujo `docker-compose.yml` en adelante): reescribir el historial se
descartó deliberadamente por ser un proyecto académico con clones existentes.

### Por qué existe
Decisión consciente en dos momentos. Al inicio, por comodidad de arranque rápido,
la contraseña se puso directa en los archivos de configuración versionados
(típica deuda deliberada "lo aseguramos después"). Al remediarlo, se optó por
**rotar la contraseña en vez de reescribir el historial**, aceptando a sabiendas
que el valor viejo —ya inválido— permanece en los commits pasados.

### Costo de no pagarla
- Mientras el repositorio sea local/privado y la contraseña sea de un contenedor
  de desarrollo efímero, el riesgo real es bajo.
- Pero si el repositorio se hiciera **público** (o se compartiera), cualquiera
  podría leer en el historial el patrón de credenciales del proyecto. Si en el
  futuro se reutilizara una contraseña parecida en un entorno real (por ejemplo
  Amazon RDS en producción), el historial se vuelve una pista de ataque.
- El costo crece con cada credencial nueva que llegara a commitearse por inercia
  si no se corrige el hábito.

### Propuesta de solución
- **Ya aplicado (mitigación):** externalizar el secreto (User Secrets + `.env`
  ignorado + `.env.example` como plantilla) y **rotar** la contraseña, de modo que
  el valor del historial ya no sea válido.
- **Pendiente (pago completo):** si el repositorio se hace público, reescribir el
  historial con `git filter-repo` o BFG Repo-Cleaner para purgar el valor viejo, y
  hacer force-push coordinado. Es una operación destructiva sobre el historial, por
  eso se pospone hasta que sea necesaria.


---

## Deuda 2 — Los controllers acceden directamente a ApplicationDbContext (diseño de código)

### Qué es
Los controllers MVC consultan la base de datos **directamente** a través de
`ApplicationDbContext`, mezclando en la misma clase la lógica de acceso a datos con
la de atender peticiones HTTP. Por ejemplo, `SesionesController.Iniciar` arma la
sesión, recorre los ejercicios de la rutina, crea cada `SerieRealizada` y guarda,
todo dentro del método de acción; y varios controllers repiten el patrón de filtrar
por `UsuarioId` en cada consulta. Es un caso incipiente de God Class / Long Method:
el controller "hace todo" en lugar de orquestar.

### Por qué existe
Decisión deliberada de arranque, coherente con el ADR-01 (MVC directo) para
avanzar rápido en un proyecto académico monolito de un solo proyecto. El propio
ADR-03 ya reconoce esta deuda: propone migrar a una arquitectura en capas con
servicios/repositorios, pero **aún no se ha implementado** para la mayoría de los
controllers (solo `Services/Volumen/`, `Services/Progreso/`, `Services/IA/` y
`Services/Catalogo/` extrajeron su lógica).

### Costo de no pagarla
- Cada regla de negocio que cambie (por ejemplo, cómo se congela una sesión)
  obliga a tocar el controller, aumentando el riesgo de romper el manejo de HTTP.
- La lógica de datos **no es testeable de forma aislada**: para probar la
  construcción de una sesión hay que pasar por el controller y la base de datos.
- El filtro de ownership por `UsuarioId` está repetido en cada consulta; si se
  olvidara en una nueva, se abriría una fuga de datos entre usuarios.
- A medida que crecen los controllers (RutinasController ya ronda las 266 líneas),
  se acercan al God Class que "nadie quiere tocar".

### Propuesta de solución
- **Extract Class:** mover la lógica de acceso a datos a servicios por dominio
  (p. ej. `SesionService`, `RutinaService`), dejando a los controllers solo
  orquestar (validar entrada, llamar al servicio, devolver la vista/resultado).
  Es la dirección que el ADR-03 ya definió; se aplicaría de forma incremental,
  un controller a la vez.
- **Extract Method:** dentro de un controller grande, extraer bloques con nombre
  (p. ej. `ConstruirSeriesDesdeRutina(...)`) mientras se hace la migración.
- **Dependency Injection:** los servicios se inyectan por el constructor primario
  (patrón que el proyecto ya usa con `CatalogoService` en `SesionesController`),
  de modo que el controller dependa de una abstracción y no del `DbContext`.
- El comportamiento observable debe permanecer idéntico: es refactorización, no
  reescritura, y se hace en pasos pequeños con un commit antes y otro después.

### Estado: PAGADA (15/07/2026)

Esta deuda se **pagó** implementando la arquitectura en capas del **ADR-03** (rama
`arquitectura-capas`). La lógica de acceso a datos se extrajo de los controllers a
servicios de la capa `GymTracker.Application` (`EjercicioService`, `RutinaService`,
`SesionService`, `MedicionService`, más los ya existentes), inyectados por DI. Los
servicios dependen de la abstracción `IApplicationDbContext`, no del `DbContext`
concreto. Se verificó que **ningún controller** accede ya a `ApplicationDbContext`
y que el comportamiento observable es idéntico (la base de datos no se vio
afectada; EF no detecta cambios de modelo). Ver la sección "Implementación" del
ADR-03.

---

## Consecuencias

- **Visibilidad:** ambas deudas quedan registradas y dejan de ser un problema
  silencioso; se pueden priorizar y pagar de forma consciente.
- **La Deuda 1** está mitigada (secreto externalizado y rotado) y su pago completo
  queda condicionado a que el repositorio se haga público.
- **La Deuda 2** se **pagó** con la migración a la arquitectura en capas del
  ADR-03 (rama `arquitectura-capas`): los controllers ya no acceden al `DbContext`.

## Relación con decisiones anteriores


- La Deuda 2 es la misma que anticipa el **ADR-03** (migración a capas); este ADR
  la formaliza como deuda técnica con su costo y su plan de pago, en lugar de
  dejarla solo mencionada.