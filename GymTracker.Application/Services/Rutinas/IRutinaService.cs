using GymTracker.DTOs;
using GymTracker.Models;
using GymTracker.Services.Volumen;

namespace GymTracker.Application.Services.Rutinas
{
    // Servicio de acceso a datos de Rutinas (ADR-03 / deuda técnica #2). Encapsula
    // las consultas, el guardado, el filtro de ownership por UsuarioId y la
    // validación de que los ejercicios asignados pertenezcan al usuario. El
    // controller conserva lo HTTP (BadRequest/NotFound/Ok, mapeo a ViewModels).
    public interface IRutinaService
    {
        Task<List<Rutina>> ListarAsync(string usuarioId);

        // Rutina con sus ejercicios (y el Ejercicio relacionado) para el detalle/editar.
        Task<Rutina?> ObtenerConEjerciciosAsync(int id, string usuarioId);

        // Ejercicios disponibles del usuario, para el dropdown de asignación.
        Task<List<Ejercicio>> ListarEjerciciosDisponiblesAsync(string usuarioId);

        // ¿Todos estos ids de ejercicio pertenecen al usuario? (validación de negocio)
        Task<bool> EjerciciosPertenecenAlUsuarioAsync(string usuarioId, IEnumerable<int> ejercicioIds);

        // Crea la rutina con sus ejercicios asignados. Devuelve el Id nuevo.
        Task<int> CrearAsync(Rutina rutina);

        // Reemplaza nombre/descripcion y los ejercicios de la rutina del usuario
        // (estrategia delete-and-replace). Devuelve false si no existe/no es suya.
        Task<bool> ActualizarAsync(
            int id, string usuarioId, string nombre, string? descripcion,
            IReadOnlyList<RutinaEjercicio> ejercicios);

        // Rutina con ejercicios, para la pantalla de confirmar borrado.
        Task<Rutina?> ObtenerParaEliminarAsync(int id, string usuarioId);

        Task<bool> EliminarAsync(int id, string usuarioId);

        // ===== Consultas de la API REST pública (sin filtro de usuario) =====
        // La API de rutinas es pública y de solo lectura (ADR-04): no filtra por
        // UsuarioId. Devuelve DTOs (no entidades) para la serialización JSON.

        Task<List<RutinaDto>> ListarDtoAsync();

        Task<RutinaDto?> ObtenerDtoAsync(int id);

        // Calcula el volumen de una rutina reutilizando Strategy + Factory (ADR-05).
        // Devuelve null si la rutina no existe.
        Task<VolumenDto?> CalcularVolumenAsync(int id, TipoVolumen tipo);
    }
}
