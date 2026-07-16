using GymTracker.Models;

namespace GymTracker.Application.Services.Sesiones
{
    // Servicio de acceso a datos de Sesiones (ADR-03 / deuda técnica #2).
    // Encapsula el historial, la creación con snapshot congelado de la rutina, el
    // guardado de valores reales y el filtro de ownership. El controller conserva
    // lo HTTP y el mapeo a ViewModels (incluida la resolución de GIFs vía catálogo).
    public interface ISesionService
    {
        // Historial de sesiones del usuario (con sus series), más reciente primero.
        Task<List<Sesion>> ListarAsync(string usuarioId);

        // Inicia una sesión CONGELANDO un snapshot de la rutina del momento
        // (nombre, ejercicios, metas). Devuelve el Id de la sesión creada, o null
        // si la rutina no existe/no es del usuario.
        Task<int?> IniciarDesdeRutinaAsync(int rutinaId, string usuarioId);

        // Sesión con sus series (para la pantalla de registrar/detalle), validando
        // ownership.
        Task<Sesion?> ObtenerConSeriesAsync(int id, string usuarioId);

        // Devuelve los vínculos (EjercicioId -> ExerciseDbId ACTUAL) de los
        // ejercicios del usuario, para resolver GIFs en vivo en la pantalla de
        // registro (el snapshot guarda el EjercicioId; el GIF es ayuda visual).
        Task<List<(int EjercicioId, string? ExerciseDbId)>> ObtenerVinculosGifAsync(
            string usuarioId, IEnumerable<int> ejercicioIds);

        // Guarda las notas y los valores reales (reps/peso) capturados de cada
        // serie. Devuelve false si la sesión no existe/no es del usuario.
        Task<bool> GuardarRealesAsync(
            int sesionId, string usuarioId, string? notas,
            IReadOnlyDictionary<int, (int RepsReales, decimal PesoReal)> valoresPorSerieId);

        Task<bool> EliminarAsync(int id, string usuarioId);
    }
}
