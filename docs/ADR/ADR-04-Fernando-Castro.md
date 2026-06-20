# ADR-04: Incorporación de una API REST a GymTracker
 
| Campo  | Valor |
|--------|-------|
| Autor  | Fernando Castro Hernández |
| Fecha  | 19/06/2026 |
| Estado | Propuesto |
 
---
 
## Contexto
 
GymTracker es hoy una aplicación web MVC con ASP.NET Core (ADR-01) que renderiza
HTML del lado del servidor con Razor y persiste sus datos en PostgreSQL a través
de Entity Framework Core. Toda la información del sistema —ejercicios y rutinas—
solo es accesible navegando la interfaz web: no existe ninguna forma
programática de consultar esos datos.
 
Esto presenta una limitación: la información del sistema está "atrapada" dentro de
las vistas. Si en el futuro quisiera que otro cliente consumiera los datos de
GymTracker —por ejemplo, una app móvil, un panel de consulta, o incluso una
herramienta de análisis personal—, hoy no habría manera de obtenerlos salvo
raspando el HTML, lo cual no es viable.
 
Las características de mi sistema relevantes para esta decisión:
 
- Las entidades **Ejercicio** y **Rutina** ya están modeladas, persistidas en
  PostgreSQL y funcionando en la aplicación web.
- El acceso actual a esos datos es exclusivamente vía HTML renderizado en el
  servidor; no hay un canal que devuelva datos crudos.
---
 
## Decisión
 
Incorporo una **API REST** a GymTracker, implementada con **ASP.NET Core Web
API**, que expone las entidades existentes como endpoints HTTP que devuelven
**JSON**. La API convive dentro del mismo proyecto que la aplicación MVC: los
controllers web siguen devolviendo HTML, y los nuevos controllers de API
responden datos bajo rutas con el prefijo `/api/...`.
 
Los endpoints de esta primera iteración son de solo lectura:
 
| Recurso | Endpoint | Devuelve |
|---------|----------|----------|
| Ejercicios | `GET /api/ejercicios` | Lista de ejercicios |
| Ejercicios | `GET /api/ejercicios/{id}` | Un ejercicio por id (404 si no existe) |
| Ejercicios | `GET /api/ejercicios?grupo={grupo}` | Lista filtrada por grupo muscular |
| Rutinas | `GET /api/rutinas` | Lista de rutinas |
| Rutinas | `GET /api/rutinas/{id}` | Una rutina por id (404 si no existe) |
 
Los endpoints **no devuelven las entidades de EF Core directamente**, sino
**DTOs** (objetos de transferencia con solo los campos que se desean exponer).
Esto evita los ciclos de serialización que producen las propiedades de navegación
cruzadas (Rutina ↔ RutinaEjercicio ↔ Ejercicio) y garantiza un JSON limpio y con
estructura estable, independiente del modelo interno.
 
La API se documenta con **Swagger (OpenAPI)**, el estándar de la industria, que
genera una interfaz interactiva donde cada endpoint queda descrito y se puede
probar.
 
### ¿Por qué REST?
 
REST es el estilo estándar para exponer recursos sobre HTTP: usa los verbos y
códigos de estado propios del protocolo (`GET`, `200`, `404`), es sencillo de
consumir desde cualquier cliente (navegador, app móvil, otra aplicación) y se
integra de forma nativa con ASP.NET Core Web API, que es el stack que ya uso. No
requiere aprender un protocolo nuevo ni añadir dependencias ajenas a mi entorno.
 
### Alternativas consideradas
 
| Alternativa | Por qué la descarté (por ahora) |
|-------------|--------------------------------|
| **No exponer API (seguir solo con MVC/HTML)** | Es lo que tengo hoy, pero deja los datos accesibles únicamente a través de la interfaz web y cierra la puerta a cualquier cliente programático. |
| **GraphQL** | Permite al cliente pedir exactamente los campos que necesita y es potente cuando hay muchas relaciones y consultas variables. Para una API de lectura con dos entidades simples es sobre-ingeniería: añade complejidad de esquema y resolvers sin un beneficio real a esta escala. |
| **gRPC** | Eficiente para comunicación entre servicios (binario, contratos fuertes), pero está pensado para servicio-a-servicio, no para ser consumido fácilmente desde un navegador o documentado con Swagger. No encaja con el objetivo de la actividad. |
 
---
 
## Persistencia en producción — ¿cómo accederá la API a sus datos?
 
> *Pregunta requerida: ¿cómo va a acceder tu API a sus datos cuando esté en
> producción? ¿Seguirás usando archivos JSON, o migras a una base de datos?*
 
GymTracker **no usa archivos JSON como almacén de datos en ningún momento**. Desde
su origen (ADR-01) persiste en una base de datos relacional **PostgreSQL** a
través de Entity Framework Core, tanto en desarrollo como será en producción. La
API REST consume esos mismos datos reutilizando el `ApplicationDbContext`
existente; no introduce un mecanismo de persistencia distinto.
 
En producción, la decisión ya documentada en el ADR-02 y el ADR-03 se mantiene:
la base de datos migrará a **Amazon RDS for PostgreSQL**, gestionada por AWS,
mientras la aplicación (web + API en el mismo despliegue) corre en un contenedor.
Gracias a EF Core, ese cambio de PostgreSQL local (Docker) a PostgreSQL en RDS no
requiere modificar el código de la API: solo cambia la cadena de conexión. La API,
por tanto, accederá a sus datos exactamente igual que la web, contra la misma base
relacional.
 
---
 
## Consecuencias
 
**✅ Lo que gano:**
 
- **Datos accesibles de forma programática.** Cualquier cliente —no solo el
  navegador— puede consultar ejercicios y rutinas como JSON.
- **Documentación profesional automática.** Swagger expone y describe los
  endpoints, lo que facilita probarlos y entenderlos sin leer el código.
- **JSON estable e independiente del modelo interno.** Al exponer DTOs y no las
  entidades de EF Core, puedo cambiar el modelo interno sin romper el contrato de
  la API, y evito fugas de campos que no quiero exponer.
- **Base para clientes futuros.** Deja preparado el terreno para, si algún día se
  quisiera, una app móvil u otra herramienta que consuma estos datos.
**⚠️ Lo que sacrifico o la complejidad que agrego:**
 
- **Más superficie que mantener.** Ahora hay dos formas de acceder a los datos
  (HTML vía MVC y JSON vía API), y ambas deben mantenerse coherentes con el
  modelo.
- **Mapeo a DTOs.** Exponer DTOs obliga a escribir y mantener el mapeo entre la
  entidad y el DTO; es más código que devolver la entidad directa, a cambio de un
  contrato limpio.
- **Consideración de seguridad pendiente.** Los endpoints de esta iteración son de
  solo lectura y, para efectos de la actividad, abiertos. En un escenario real
  habría que protegerlos (autenticación/autorización) para que cada quien acceda
  solo a sus propios datos, igual que ya hace la parte web con Identity.
---
 
## Relación con decisiones anteriores
 
Esta decisión es coherente con los ADR previos: la API REST es una nueva interfaz
sobre el mismo sistema (ADR-01), encaja como un cliente más de la lógica que el
ADR-03 busca centralizar en servicios, y consume los datos de la misma base
PostgreSQL que migrará a RDS según los ADR-02 y ADR-03.
 