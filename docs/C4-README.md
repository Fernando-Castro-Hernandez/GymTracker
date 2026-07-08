# Documentación de Arquitectura — GymTracker

Esta carpeta contiene la documentación arquitectónica del proyecto, incluyendo
los diagramas del **modelo C4** y los registros de decisiones de arquitectura
(ADR).

## ¿Qué es el modelo C4?

El **modelo C4** es una forma de documentar la arquitectura de un sistema de
software mediante diagramas organizados en niveles, propuesta por Simon Brown.
La idea central es que cada nivel es un **zoom-in** del anterior: se empieza con
una vista muy general y se va profundizando solo donde hace falta. Cada nivel
está pensado para una **audiencia distinta**.

Las "4 C" son cuatro niveles de detalle:

| Nivel | Nombre | Responde a la pregunta | Audiencia |
|-------|--------|------------------------|-----------|
| 1 | **Contexto** | ¿Qué es el sistema y quién lo usa? | Cualquiera |
| 2 | **Contenedores** | ¿De qué piezas grandes se compone? | Equipo técnico |
| 3 | **Componentes** | ¿Qué hay dentro de cada pieza? | Quien la modifica |
| 4 | **Código** | ¿Cómo está hecha esa clase exactamente? | Rara vez necesario |

La ventaja de este enfoque es que **no obliga a mostrarlo todo de golpe**. En el
Nivel 1 nadie necesita saber que el sistema usa ASP.NET o PostgreSQL; ese detalle
aparece cuando es relevante, en niveles más profundos. Así cada lector encuentra
justo el nivel de detalle que necesita.

## ¿Por qué se documenta GymTracker con C4?

GymTracker creció hasta tener varias piezas (aplicación web, API REST, base de
datos, servicios con patrones de diseño). Documentar esa arquitectura con C4
aporta:

- **Comunicación clara:** explicar el sistema a distintas audiencias sin abrumar.
- **Trazabilidad de decisiones:** los diagramas reflejan decisiones registradas
  en los ADR (por ejemplo, los patrones Strategy + Factory del ADR-05).
- **Onboarding:** cualquiera que se acerque al proyecto entiende primero el
  panorama general y luego el detalle.

En este proyecto se documentan los **tres primeros niveles** (Contexto,
Contenedores y Componentes). El Nivel 4 (Código) se omite deliberadamente:
raramente aporta valor y el propio código, junto con los ADR, ya cumple ese rol.

## Diagramas

Los diagramas están escritos en **Mermaid** y se renderizan automáticamente al
abrir cada archivo en GitHub.

- [Diagrama C4 — Nivel 1: Contexto](./DiagramaC1.md)
- [Diagrama C4 — Nivel 2: Contenedores](./DiagramaC2.md)
- [Diagrama C4 — Nivel 3: Componentes](./DiagramaC3.md)

## Otros documentos

- [Registros de Decisiones de Arquitectura (ADR)](./ADR/) — decisiones clave del
  proyecto y su justificación.
- [Plan de integraciones de IA](./PLAN-integraciones-IA.md) — propuesta de trabajo
  futuro.
