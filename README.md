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

## Autor

Fernando Castro Hernández — TSU Desarrollo de Software
Materia: Arquitectura de Software · Mayo–Agosto 2026