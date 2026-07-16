using GymTracker.Application.Abstractions;
using GymTracker.DTOs;
using GymTracker.Models;
using GymTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Application.Services.Ejercicios
{
    // Implementación del servicio de Ejercicios. Depende de IApplicationDbContext.
    public class EjercicioService(IApplicationDbContext context) : IEjercicioService
    {
        public async Task<List<Ejercicio>> ListarAsync(string usuarioId, GrupoMuscular? grupo)
        {
            var consulta = context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId);

            if (grupo.HasValue)
                consulta = consulta.Where(e => e.GrupoMuscular == grupo.Value);

            return await consulta.OrderBy(e => e.Nombre).ToListAsync();
        }

        public async Task<Ejercicio?> ObtenerAsync(int id, string usuarioId) =>
            await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

        public async Task CrearAsync(Ejercicio ejercicio)
        {
            context.Ejercicios.Add(ejercicio);
            await context.SaveChangesAsync();
        }

        public async Task<bool> ActualizarAsync(int id, string usuarioId, Ejercicio datos)
        {
            var original = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if (original == null) return false;

            // Actualizar solo los campos editables (no se toca UsuarioId ni ExerciseDbId).
            original.Nombre = datos.Nombre;
            original.GrupoMuscular = datos.GrupoMuscular;
            original.Descripcion = datos.Descripcion;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAsync(int id, string usuarioId)
        {
            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if (ejercicio == null) return false;

            context.Ejercicios.Remove(ejercicio);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Ejercicio>> ListarParaSelectorAsync(string usuarioId) =>
            await context.Ejercicios
                .Where(e => e.UsuarioId == usuarioId)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

        public async Task<Ejercicio?> FijarExerciseDbIdAsync(
            int ejercicioId, string usuarioId, string? exerciseDbId)
        {
            var ejercicio = await context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == ejercicioId && e.UsuarioId == usuarioId);

            if (ejercicio == null) return null;

            ejercicio.ExerciseDbId = exerciseDbId;
            await context.SaveChangesAsync();
            return ejercicio;
        }

        // ===== Consultas de la API REST pública (sin filtro de usuario) =====

        public async Task<List<EjercicioDto>> ListarDtoAsync(GrupoMuscular? grupo)
        {
            var consulta = context.Ejercicios.AsQueryable();

            if (grupo.HasValue)
                consulta = consulta.Where(e => e.GrupoMuscular == grupo.Value);

            return await consulta
                .OrderBy(e => e.Nombre)
                .Select(e => new EjercicioDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    GrupoMuscular = e.GrupoMuscular.ToString(),
                    Descripcion = e.Descripcion
                })
                .ToListAsync();
        }

        public async Task<EjercicioDto?> ObtenerDtoAsync(int id) =>
            await context.Ejercicios
                .Where(e => e.Id == id)
                .Select(e => new EjercicioDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    GrupoMuscular = e.GrupoMuscular.ToString(),
                    Descripcion = e.Descripcion
                })
                .FirstOrDefaultAsync();
    }
}
