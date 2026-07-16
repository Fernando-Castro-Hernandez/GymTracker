using GymTracker.DTOs;
using GymTracker.Models;
using GymTracker.Models.Enums;

namespace GymTracker.Application.Services.Ejercicios
{
    // Servicio de acceso a datos de Ejercicios (ADR-03 / deuda técnica #2).
    // Encapsula consultas, guardado y filtro de ownership por UsuarioId. El
    // controller conserva lo HTTP (vistas, Json, ViewBag) y la vinculación con el
    // catálogo (CatalogoService).
    public interface IEjercicioService
    {
        // Listado del usuario, opcionalmente filtrado por grupo muscular.
        Task<List<Ejercicio>> ListarAsync(string usuarioId, GrupoMuscular? grupo);

        Task<Ejercicio?> ObtenerAsync(int id, string usuarioId);

        Task CrearAsync(Ejercicio ejercicio);

        // Actualiza los campos editables (nombre, grupo, descripción). Devuelve
        // false si no existe o no le pertenece.
        Task<bool> ActualizarAsync(int id, string usuarioId, Ejercicio datos);

        Task<bool> EliminarAsync(int id, string usuarioId);

        // Proyección ligera para el selector JSON del modal "Vincular".
        Task<List<Ejercicio>> ListarParaSelectorAsync(string usuarioId);

        // Guarda (o quita) el vínculo con el catálogo. Devuelve el ejercicio
        // actualizado (para leer su nombre) o null si no existe/no es del usuario.
        Task<Ejercicio?> FijarExerciseDbIdAsync(int ejercicioId, string usuarioId, string? exerciseDbId);

        // ===== Consultas de la API REST pública (sin filtro de usuario) =====
        // La API de catálogo es pública y de solo lectura (ADR-04): no filtra por
        // UsuarioId. Devuelve DTOs (no entidades) para la serialización JSON.

        // Lista los ejercicios como DTO, opcionalmente filtrados por grupo.
        Task<List<EjercicioDto>> ListarDtoAsync(GrupoMuscular? grupo);

        // Un ejercicio por id como DTO, o null si no existe.
        Task<EjercicioDto?> ObtenerDtoAsync(int id);
    }
}
