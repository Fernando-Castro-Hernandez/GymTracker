<a name="readme-top"></a>

<div align="center">

# GymTracker

**Bitácora personal de entrenamiento de gimnasio.**
Registra ejercicios, diseña rutinas con metas, guarda tus sesiones reales,
mide tu progreso corporal y visualiza tu evolución de fuerza con el tiempo.

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL_16-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap_5-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?style=for-the-badge&logo=chartdotjs&logoColor=white)

![Estado](https://img.shields.io/badge/estado-MVP_funcional-success?style=flat-square)
![Arquitectura](https://img.shields.io/badge/arquitectura-4_capas-informational?style=flat-square)
![Último commit](https://img.shields.io/github/last-commit/Fernando-Castro-Hernandez/GymTracker?style=flat-square)
![Lenguaje top](https://img.shields.io/github/languages/top/Fernando-Castro-Hernandez/GymTracker?style=flat-square)

</div>

---

## Tabla de contenidos

1. [Sobre el proyecto](#-sobre-el-proyecto)
2. [Funcionalidades](#-funcionalidades)
3. [Cómo usar GymTracker](#-cómo-usar-gymtracker-flujo)
4. [Capturas](#-capturas)
5. [Tecnologías](#-tecnologías)
6. [Arquitectura y decisiones](#-arquitectura-y-decisiones)
7. [Cómo ejecutar el proyecto](#-cómo-ejecutar-el-proyecto)
8. [Hoja de ruta](#-hoja-de-ruta)
9. [Uso de IA en el desarrollo](#-uso-de-ia-en-el-desarrollo)
10. [Agradecimientos](#-agradecimientos)
11. [Licencia](#-licencia)
12. [Autor](#-autor)

---

## 📖 Sobre el proyecto

**GymTracker** es una aplicación web construida como proyecto académico para la
materia de **Arquitectura de Software** (TSU en Desarrollo de Software). Su
propósito nace de un principio del entrenamiento de fuerza: la **sobrecarga
progresiva**. Para progresar de forma sostenida hace falta un registro objetivo
de lo que se levanta, sesión tras sesión. GymTracker es esa bitácora.

Cubre el **ciclo completo** del seguimiento de entrenamiento: desde la biblioteca
de ejercicios y el diseño de rutinas con metas, hasta el registro de lo que
*realmente* se hizo en el gimnasio, las mediciones corporales y las gráficas de
progreso. Incluye además un **Coach IA** que analiza tus rutinas y un **catálogo
de +1300 ejercicios con animaciones (GIFs)**.

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## 🌟 Funcionalidades

| Módulo | Qué hace |
|--------|----------|
| **Catálogo de ejercicios** | Biblioteca personal (CRUD) organizada por grupo muscular. |
| **Rutinas con metas** | Combinaciones de ejercicios con objetivos de series, repeticiones y peso. Asignación dinámica con una tabla interactiva. |
| **Sesiones de entrenamiento** | Al iniciar una sesión desde una rutina se **congela un *snapshot*** de la rutina del momento, para que el historial sea inmutable aunque la rutina cambie después. Se registran los valores reales de cada serie. |
| **Mediciones corporales** | Peso (obligatorio) + composición corporal (% grasa, grasa visceral, masa muscular, % agua) y medidas con cinta (opcionales), a lo largo del tiempo. |
| **Progreso** | Tres gráficas Chart.js: evolución de peso corporal, volumen por sesión y progresión de carga por ejercicio. |
| **Catálogo con GIFs** | +1300 ejercicios con animaciones, servidos desde un *seed* local (sin llamar a APIs externas en runtime). Se vinculan a tus ejercicios propios para ver la técnica al entrenar. |
| **Coach IA** | Analiza una rutina (balance muscular y volumen) con un LLM y devuelve recomendaciones. Usa Claude Haiku con *fallback* a Gemini. |
| **API REST + Swagger** | Endpoints JSON para el catálogo y los datos de las gráficas, documentados con Swagger/OpenAPI. |
| **Autenticación** | Registro e inicio de sesión con ASP.NET Core Identity; cada usuario solo ve y edita sus propios datos. |

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## 🔄 Cómo usar GymTracker (flujo)

El uso sigue el ciclo natural de la sobrecarga progresiva: **construyes tu
biblioteca → diseñas la rutina → entrenas y registras → mides tu avance.**

```mermaid
flowchart LR
    A([Inicia sesión]) --> B[Crea tus ejercicios]
    B --> C[Arma una rutina<br/>con metas de series/reps/peso]
    C --> D[Inicia el entrenamiento<br/>desde la rutina]
    D --> E[Registra reps y peso<br/>reales de cada serie]
    E --> F[Registra tus mediciones<br/>corporales]
    F --> G[Revisa tu progreso<br/>en las gráficas]
    G -. ajusta metas .-> C
    C -. Coach IA analiza .-> H[Recomendaciones]
```

| Paso | Qué haces | Qué pasa por dentro |
|:----:|-----------|---------------------|
| 1 | Inicias sesión o te registras | ASP.NET Core Identity crea tu cuenta; a partir de aquí solo ves tus datos. |
| 2 | Creas ejercicios en tu catálogo | Se guardan por grupo muscular; puedes vincularlos a un GIF del catálogo. |
| 3 | Armas una rutina con metas | Asignas ejercicios con series/reps/peso objetivo en una tabla interactiva. |
| 4 | Inicias el entrenamiento | Se **congela un snapshot** de la rutina: aunque la edites después, la sesión conserva las metas del momento. |
| 5 | Registras la sesión real | Capturas reps y peso realmente ejecutados de cada serie. |
| 6 | Añades mediciones | Registras peso corporal y composición para seguir tu evolución física. |
| 7 | Revisas tu progreso | Tres gráficas resumen peso corporal, volumen por sesión y progresión de carga. |
| + | Consultas al Coach IA | Sobre una rutina, la IA evalúa el balance muscular y sugiere mejoras. |

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## Vistas

<div align="center">

| Landing | Mis ejercicios | Rutinas |
|:---:|:---:|:---:|
| <img src="./docs/Images/LandingGymTracker.png" width="280" alt="Landing de GymTracker"> | <img src="./docs/Images/MisEjercicios.png" width="280" alt="Catálogo de ejercicios"> | <img src="./docs/Images/Rutinas.png" width="280" alt="Rutinas con metas"> |

</div>

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## 🧰 Tecnologías

<div align="center">

![.NET](https://img.shields.io/badge/ASP.NET_Core_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![EF Core](https://img.shields.io/badge/Entity_Framework_Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL_16-316192?style=flat-square&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2CA5E0?style=flat-square&logo=docker&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap_5-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?style=flat-square&logo=chartdotjs&logoColor=white)
![Identity](https://img.shields.io/badge/ASP.NET_Identity-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=flat-square&logo=swagger&logoColor=black)
![Claude](https://img.shields.io/badge/Anthropic_Claude-D97757?style=flat-square&logo=anthropic&logoColor=white)
![Gemini](https://img.shields.io/badge/Google_Gemini-8E75B2?style=flat-square&logo=googlegemini&logoColor=white)

</div>

| Capa | Tecnología | Rol |
|------|-----------|-----|
| Web / MVC | ASP.NET Core 10 MVC + Razor | Vistas, formularios y lógica de presentación |
| API REST | ASP.NET Core Web API + Swagger | Endpoints JSON (catálogo y datos de gráficas) |
| ORM | Entity Framework Core 10 | Acceso a datos y migraciones |
| Base de datos | PostgreSQL 16 (en Docker) | Persistencia |
| Autenticación | ASP.NET Core Identity | Usuarios y sesiones (cookies) |
| IA | Claude Haiku + Gemini (fallback) | Coach: análisis de rutinas |
| Frontend | Bootstrap 5 + Chart.js | Estilos y visualización de datos |

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## 🧱 Arquitectura y decisiones

GymTracker está organizado en una **arquitectura en capas con 4 proyectos
separados** (ADR-03), donde las referencias entre proyectos hacen que el
compilador imponga la separación de responsabilidades:

```
GymTracker.slnx
├── GymTracker.Domain          → Entidades y enums (núcleo, sin dependencias)
├── GymTracker.Application      → Servicios de negocio, DTOs e interfaces        → Domain
├── GymTracker.Infrastructure   → ApplicationDbContext, migraciones, Identity     → Domain, Application
└── GymTracker.Web              → MVC, API REST, Identity UI (composition root)   → Application, Infrastructure
```

Dirección de dependencia: **`Web → Application → Domain`**, con `Infrastructure`
proveyendo la persistencia. Todas las decisiones relevantes están documentadas
como **ADR** (Architecture Decision Records) en [`docs/ADR/`](./docs/ADR).

<details>
<summary><b>Aspectos destacados de la arquitectura</b> (clic para desplegar)</summary>

<br>

- **Arquitectura en capas (ADR-03):** los controllers no acceden al `DbContext`;
  la lógica de datos vive en servicios de la capa Application, inyectados por DI y
  dependientes de la abstracción `IApplicationDbContext`.
- **Patrones de diseño GOF (ADR-05):** el cálculo de volumen usa **Strategy +
  Factory Method**, permitiendo intercambiar fórmulas (tonelaje, series efectivas,
  volumen relativo) sin tocar el código que las consume.
- **Arquitectura de *snapshot*** en las sesiones: los datos de la rutina se
  congelan al iniciar el entrenamiento, garantizando un historial inmutable.
- **Coach IA con *fallback*:** interfaz común `IProveedorIA` con Claude como
  proveedor principal y Gemini como respaldo, orquestados por `ProveedorIAConFallback`.
- **Catálogo con *seed* local (ADR-06):** el catálogo de +1300 ejercicios se lee
  de un JSON local cacheado en memoria; **no** se llama a la API externa en runtime
  (patrón cache-aside, para evitar rate limits y desacoplar el nº de usuarios).
- **Seguridad contextual:** los endpoints de catálogo son públicos, pero los de
  progreso requieren autenticación por exponer datos personales.
- **Gestión de secretos:** contraseñas y API keys nunca se versionan; viven en
  **User Secrets** (desarrollo) y variables de entorno (producción).
- **DTOs** en la API para evitar ciclos de serialización de EF Core.

</details>

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## 🚀 Cómo ejecutar el proyecto

**Requisitos:** .NET 10 SDK, Docker.

> Los secretos (contraseña de la base de datos y API keys de los LLMs) **no se
> versionan**: viven en **User Secrets** en desarrollo y en variables de entorno
> en producción. Por eso, al clonar el repo hay que configurarlos una vez.

```bash
# 1. Configurar la contraseña de la base de datos para Docker
#    Copia la plantilla y edita el valor de POSTGRES_PASSWORD.
cp .env.example .env          # en PowerShell: Copy-Item .env.example .env

# 2. Levantar la base de datos PostgreSQL en Docker
#    El contenedor se publica en el puerto 5433 del host (para no chocar con un
#    PostgreSQL nativo que use el 5432).
docker compose up -d

# 3. Dar a la app la connection string COMPLETA (con la misma contraseña del .env)
#    vía User Secrets. Sobrescribe la de appsettings.json (que va sin contraseña).
#    Los User Secrets viven en el proyecto Web (--project GymTracker.Web).
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5433;Database=gymtracker;Username=gymtracker_user;Password=TU_CONTRASENA" \
  --project GymTracker.Web

# 4. (Opcional, solo si usas el Coach IA) Configurar las API keys de los LLMs
dotnet user-secrets set "Anthropic:ApiKey" "TU_API_KEY_DE_ANTHROPIC" --project GymTracker.Web
dotnet user-secrets set "Gemini:ApiKey" "TU_API_KEY_DE_GEMINI" --project GymTracker.Web

# 5. Aplicar las migraciones (el DbContext vive en Infrastructure; el arranque en Web)
dotnet ef database update --project GymTracker.Infrastructure --startup-project GymTracker.Web

# 6. Ejecutar la aplicación
dotnet run --project GymTracker.Web
```

La aplicación queda disponible en `https://localhost:44353` y la documentación de
la API (Swagger) en `https://localhost:44353/swagger`.

> **Nota sobre el puerto 5432:** si tienes un PostgreSQL instalado de forma nativa
> en Windows, suele ocupar el 5432 y le robaría las conexiones al contenedor. Por
> eso el contenedor de GymTracker se publica en **5433**. Si prefieres el 5432,
> cambia el mapeo en `docker-compose.yml` y el `Port=` de la connection string.

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## Hoja de ruta

**Ya implementado**

- Catálogo, rutinas, sesiones con snapshot, mediciones y progreso
- API REST + Swagger
- Arquitectura en capas (4 proyectos)
- Catálogo de +1300 ejercicios con GIFs (seed local)
- Coach IA (análisis de rutinas con Claude + fallback a Gemini)

**Pendiente**

- **🧠 Generador de rutinas con IA** — crear rutinas a partir de un objetivo, con
  salida estructurada de un LLM (ver [`docs/PLAN-integraciones-IA.md`](./docs/PLAN-integraciones-IA.md)).
- **☁️ Despliegue en AWS** — Amazon RDS (PostgreSQL), ECS Fargate + ECR + ALB,
  Terraform (IaC) y GitHub Actions (CI/CD).

![AWS](https://img.shields.io/badge/AWS-232F3E?style=flat-square&logo=amazon-web-services&logoColor=white)
![Terraform](https://img.shields.io/badge/Terraform-7B42BC?style=flat-square&logo=terraform&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-2088FF?style=flat-square&logo=github-actions&logoColor=white)

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## Uso de IA en el desarrollo

Durante el desarrollo de GymTracker se usaron asistentes de IA (Claude) como
**apoyo educativo**, no como sustituto del aprendizaje. El criterio fue el
siguiente:

- **El diseño y las decisiones son propios.** Las decisiones arquitectónicas están
  razonadas y documentadas por el autor en los ADR.
- **Entender antes que copiar.** El código generado o sugerido se revisó, se
  comprendió y se adaptó manualmente antes de integrarse.
- **Aprendizaje, no atajo.** La IA se usó para explicar conceptos (patrones de
  diseño, arquitectura en capas, EF Core, manejo de secretos), acelerar tareas
  repetitivas y revisar código, reforzando lo visto en clase.


<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## Agradecimientos

- Al **Dr. Jorge Javier Pedrozo Romero**, por la guía, la retroalimentación y los
  contenidos impartidos en la materia de **Arquitectura de Software**. Muchos de
  los conceptos aplicados en este proyecto —patrones de diseño, arquitectura en
  capas, ADR, refactorización y deuda técnica— provienen directamente de sus
  clases y fueron la base para las decisiones documentadas aquí.

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>

---

## 📄 Licencia

Proyecto **académico** con fines educativos (TSU en Desarrollo de Software ·
Arquitectura de Software). No se distribuye bajo una licencia open-source formal.

---

## 👤 Autor

**Jesús Fernando Castro Hernández**
Proyecto académico — TSU en Desarrollo de Software · Arquitectura de Software.

<p align="right">(<a href="#readme-top">volver arriba</a>)</p>
