using GymTracker.Models;

namespace GymTracker.Application.Services.Mediciones
{
    // Servicio de acceso a datos de Mediciones (ADR-03 / deuda técnica #2 del
    // ADR-06). Encapsula las consultas, el guardado y el filtro de ownership por
    // UsuarioId, sacándolos del controller. El controller conserva la lógica HTTP
    // (ModelState, vistas, redirecciones, conversión de fechas de formulario).
    public interface IMedicionService
    {
        Task<List<Medicion>> ListarAsync(string usuarioId);
        Task<Medicion?> ObtenerAsync(int id, string usuarioId);
        Task CrearAsync(Medicion medicion);

        // Actualiza los campos editables de la medición del usuario. Devuelve
        // false si no existe o no le pertenece (para que el controller responda
        // NotFound). El controller ya validó ModelState antes de llamar.
        Task<bool> ActualizarAsync(int id, string usuarioId, Medicion datos);

        // Elimina la medición del usuario. Devuelve false si no existe/no es suya.
        Task<bool> EliminarAsync(int id, string usuarioId);
    }
}
