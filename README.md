# GymTracker

Sistema personal de seguimiento de entrenamientos. Proyecto de la materia
de Arquitectura de Software (TSU Desarrollo de Software, cuatrimestre
mayo–agosto 2026).

## Stack

- ASP.NET Core 10 MVC
- Entity Framework Core 10
- PostgreSQL 16 (vía Docker)
- ASP.NET Core Identity
- Bootstrap 5

## Setup local

1. Clonar el repositorio.
2. Levantar PostgreSQL: `docker compose up -d`
3. Aplicar migraciones: `dotnet ef database update`
4. Correr el proyecto: `dotnet run` (o F5 en Visual Studio).

## Declaración de IA

## Declaración de uso de IA

En el desarrollo de este proyecto utilicé **Claude** como apoyo. Su uso se limitó a:

- Resolver dudas conceptuales sobre arquitectura de software (estilos arquitectónicos, vistas, trade-offs) y sobre el funcionamiento de una API REST.
- Apoyo en la redacción y estructura de los ADR (Architecture Decision Records).
- Ayuda para diagnosticar errores durante el desarrollo.

Todas las decisiones de diseño, la revisión del código y la comprensión de lo implementado son propias. La IA se utilizó como herramienta de aprendizaje y apoyo técnico, no como sustituto del trabajo ni del criterio personal.

## Autor

Fernando Castro Hernández — TSU Desarrollo de Software
Materia: Arquitectura de Software · Mayo–Agosto 2026