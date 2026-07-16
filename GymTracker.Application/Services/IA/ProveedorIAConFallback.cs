using Microsoft.Extensions.Logging;

namespace GymTracker.Services.IA
{
    // Orquestador de proveedores con fallback (patrón Chain of Responsibility).
    // Implementa IProveedorIA, así que el CoachService lo usa igual que a
    // cualquier proveedor. Internamente prueba una lista de proveedores en
    // orden: si el primero falla, pasa al siguiente. Solo falla si TODOS fallan.
    public class ProveedorIAConFallback : IProveedorIA
    {
        private readonly IReadOnlyList<IProveedorIA> _proveedores;
        private readonly ILogger<ProveedorIAConFallback> _logger;

        public string Nombre => "Fallback (" + string.Join(" → ", _proveedores.Select(p => p.Nombre)) + ")";

        public ProveedorIAConFallback(
            IReadOnlyList<IProveedorIA> proveedores,
            ILogger<ProveedorIAConFallback> logger)
        {
            if (proveedores == null || proveedores.Count == 0)
                throw new ArgumentException("Se requiere al menos un proveedor de IA.");
            _proveedores = proveedores;
            _logger = logger;
        }

        public async Task<AnalisisRutinaDto> AnalizarRutinaAsync(string systemPrompt, string datosRutina)
        {
            Exception? ultimaExcepcion = null;

            foreach (var proveedor in _proveedores)
            {
                try
                {
                    var resultado = await proveedor.AnalizarRutinaAsync(systemPrompt, datosRutina);
                    _logger.LogInformation("Análisis generado por el proveedor {Proveedor}.", proveedor.Nombre);
                    return resultado;
                }
                catch (Exception ex)
                {
                    // Este proveedor falló: registramos y probamos el siguiente.
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex,
                        "El proveedor {Proveedor} falló. Intentando el siguiente.", proveedor.Nombre);
                }
            }

            // Si llegamos aquí, todos los proveedores fallaron.
            throw new InvalidOperationException(
                "Todos los proveedores de IA fallaron.", ultimaExcepcion);
        }
    }
}