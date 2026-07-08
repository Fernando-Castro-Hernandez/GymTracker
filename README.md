# 🏋️ GymTracker
 
> Bitácora personal de entrenamiento de gimnasio. Registra ejercicios, diseña
> rutinas con metas, guarda tus sesiones reales, mide tu progreso corporal y
> visualiza tu evolución de fuerza con el tiempo.
 
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?style=for-the-badge&logo=chartdotjs&logoColor=white)
 
---
 
## 📖 Sobre el proyecto
 
**GymTracker** es una aplicación web construida como proyecto académico para la
materia de **Arquitectura de Software**. Su propósito nace de un principio del
entrenamiento de fuerza: la **sobrecarga progresiva**. Para progresar de forma
sostenida hace falta un registro objetivo de lo que se levanta, sesión tras
sesión. GymTracker es esa bitácora.
 
La aplicación cubre el ciclo completo del seguimiento de entrenamiento:
 
- **Catálogo de ejercicios** — biblioteca personal organizada por grupo muscular.
- **Rutinas con metas** — combinaciones de ejercicios con objetivos de series,
  repeticiones y peso.
- **Sesiones de entrenamiento** — registro de lo que *realmente* se hizo cada día,
  con una arquitectura de *snapshot* que congela la rutina del momento para que el
  historial sea inmutable aunque la rutina cambie después.
- **Mediciones corporales** — peso y composición corporal (grasa, masa muscular,
  medidas) a lo largo del tiempo.
- **Progreso** — gráficas de evolución de peso corporal, volumen de entrenamiento
  y progresión de carga por ejercicio.
---
 
## 🛠️ Tecnologías
 
### Backend
![.NET](https://img.shields.io/badge/ASP.NET_Core_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![EF Core](https://img.shields.io/badge/Entity_Framework_Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
 
### Base de datos
![PostgreSQL](https://img.shields.io/badge/PostgreSQL_16-316192?style=flat-square&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2CA5E0?style=flat-square&logo=docker&logoColor=white)
 
### Frontend
![HTML5](https://img.shields.io/badge/HTML5-E34F26?style=flat-square&logo=html5&logoColor=white)
![CSS3](https://img.shields.io/badge/CSS3-1572B6?style=flat-square&logo=css3&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-F7DF1E?style=flat-square&logo=javascript&logoColor=black)
![Bootstrap](https://img.shields.io/badge/Bootstrap_5-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?style=flat-square&logo=chartdotjs&logoColor=white)
 
### Autenticación y API
![Identity](https://img.shields.io/badge/ASP.NET_Identity-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=flat-square&logo=swagger&logoColor=black)
 
| Capa | Tecnología | Rol |
|------|-----------|-----|
| Web / MVC | ASP.NET Core 10 MVC + Razor | Vistas, formularios y lógica de aplicación |
| API REST | ASP.NET Core Web API + Swagger | Endpoints JSON (catálogo y datos de gráficas) |
| ORM | Entity Framework Core | Acceso a datos y migraciones |
| Base de datos | PostgreSQL 16 (en Docker) | Persistencia |
| Autenticación | ASP.NET Core Identity | Usuarios y sesiones (cookies) |
| Frontend | Bootstrap 5 + Chart.js | Estilos y visualización de datos |
 
---
 
## 🏛️ Arquitectura y decisiones
 
El proyecto sigue el patrón **MVC** de ASP.NET Core, con **servicios** dedicados
para la lógica que no pertenece a los controllers. Todas las decisiones
arquitectónicas relevantes están documentadas como **ADR** (Architecture Decision
Records) en la carpeta [`/docs`](./docs).
 
Aspectos destacados:
 
- **Patrones de diseño GOF (ADR-05):** el cálculo de volumen de entrenamiento usa
  **Strategy + Factory Method**, permitiendo intercambiar fórmulas (tonelaje,
  series efectivas, volumen relativo).
- **Arquitectura de snapshot** en las sesiones: los datos de la rutina se congelan
  al iniciar el entrenamiento, garantizando un historial inmutable.
- **Seguridad contextual:** los endpoints de catálogo son públicos, pero los de
  progreso requieren autenticación por exponer datos personales.
- **DTOs** en la API para evitar ciclos de serialización de EF Core.
---
 
## 🚀 Cómo ejecutar el proyecto
 
**Requisitos:** .NET 10 SDK, Docker.
 
```bash
# 1. Levantar la base de datos PostgreSQL en Docker
docker compose up -d
 
# 2. Aplicar las migraciones
dotnet ef database update
 
# 3. Ejecutar la aplicación
dotnet run
```
 
La aplicación queda disponible en `https://localhost:44353` y la documentación
de la API (Swagger) en `https://localhost:44353/swagger`.
 
---
 
## 🔮 Implementaciones futuras
 
El proyecto tiene una hoja de ruta documentada en
[`docs/PLAN-integraciones-IA.md`](./docs/PLAN-integraciones-IA.md):
 
- **Coach IA** — análisis del balance muscular y volumen de las rutinas, con
  recomendaciones personalizadas basadas en el historial y las mediciones.
- **Generador de rutinas con IA** — creación de rutinas mediante salida
  estructurada de un modelo de lenguaje.
- **Catálogo enriquecido** — integración con una API pública (wger) para añadir
  GIFs, músculos e instrucciones a cada ejercicio.
![Claude](https://img.shields.io/badge/Anthropic_Claude-D97757?style=flat-square&logo=anthropic&logoColor=white)
 
---
 
## ☁️ Despliegue futuro
 
Plan de migración a la nube sobre **AWS**, alineado con una ruta de certificación
(CLF-C02 → SAA-C03):
 
![AWS](https://img.shields.io/badge/AWS-232F3E?style=flat-square&logo=amazon-web-services&logoColor=white)
![Terraform](https://img.shields.io/badge/Terraform-7B42BC?style=flat-square&logo=terraform&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-2088FF?style=flat-square&logo=github-actions&logoColor=white)
 
- **Base de datos:** Amazon RDS (PostgreSQL gestionado).
- **Cómputo:** evolución de EC2 → ECS Fargate + ECR + ALB.
- **Infraestructura como código:** Terraform.
- **CI/CD:** GitHub Actions.
---
 
## 📐 Documentación de arquitectura (esta rama)
 
Esta rama (`UML`) añade la documentación visual del sistema mediante el **modelo
C4** de Simon Brown, en la carpeta [`/docs`](./docs). El modelo C4 documenta la
arquitectura en niveles, donde cada uno es un *zoom-in* del anterior y está
pensado para una audiencia distinta.
 
- **[Diagrama C4 — Nivel 1: Contexto](./docs/DiagramaC1.md)** — ¿Qué es el sistema
  y quién lo usa? Vista general sin tecnología.
- **[Diagrama C4 — Nivel 2: Contenedores](./docs/DiagramaC2.md)** — ¿De qué piezas
  grandes se compone? Aplicaciones, base de datos y cómo se comunican.
- **[Diagrama C4 — Nivel 3: Componentes](./docs/DiagramaC3.md)** — ¿Qué hay dentro
  de la aplicación web? Controllers, servicios y acceso a datos.
Más detalle en el [README de documentación](./docs/C4-README.md).
 
---
 
## 👤 Autor
 
**Jesús Fernando Castro Hernández**
Proyecto académico — TSU en Desarrollo de Software · Arquitectura de Software.
 
