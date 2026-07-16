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
