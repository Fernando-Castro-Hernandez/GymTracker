using System.Text.Json;
using GymTracker.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace GymTracker.Services.Catalogo
{
    // Servicio del catálogo de ejercicios (con GIFs).
    //
    // Arquitectura (ADR-06): patrón cache-aside / seed local. El catálogo completo
    // (~1,500 ejercicios) se descargó UNA vez desde ExerciseDB OSS a
    // SeedData/exercises.json (ver tools/generate-seed.ps1). En runtime NO se llama
    // a la API externa: se lee el JSON local una sola vez, se cachea en memoria y
    // se filtra/pagina con LINQ. Así el catálogo carga en milisegundos, no hay
    // rate limit (HTTP 429), y el número de usuarios queda desacoplado del
    // consumo de la API. Los GIFs individuales sí se sirven del CDN al vuelo
    // (URLs estables en gifUrl), pero eso no toca la API de lista.
    public class CatalogoService(ISeedFileProvider seedFiles, IMemoryCache cache)
    {
        private const string ClaveCacheCatalogo = "catalogo:todos";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Catálogo completo, cargado una sola vez desde el archivo y cacheado
        // permanentemente (es un archivo estático que no cambia en runtime).
        private List<EjercicioCatalogoDto> Catalogo =>
            cache.GetOrCreate(ClaveCacheCatalogo, entrada =>
            {
                entrada.Priority = CacheItemPriority.NeverRemove;
                return CargarDesdeArchivo();
            })!;

        // Lista ejercicios, opcionalmente filtrados por parte del cuerpo, con
        // paginación por offset. El "cursor" es el índice de inicio de la
        // siguiente página (se mantiene el contrato opaco de la vista: la vista
        // solo reenvía el NextCursor que le damos, sin interpretarlo).
        public Task<RespuestaCatalogoDto> ListarAsync(
            string? bodyPart = null, string? cursor = null, int limit = 12)
        {
            IEnumerable<EjercicioCatalogoDto> filtrados = Catalogo;

            if (!string.IsNullOrWhiteSpace(bodyPart))
                filtrados = filtrados.Where(e =>
                    e.BodyParts.Any(bp => bp.Equals(bodyPart, StringComparison.OrdinalIgnoreCase)));

            var lista = filtrados.ToList();

            // El cursor es el offset; si no es un entero válido, empieza en 0.
            var offset = int.TryParse(cursor, out var o) && o > 0 ? o : 0;

            var pagina = lista.Skip(offset).Take(limit).ToList();
            var siguienteOffset = offset + pagina.Count;
            var hayMas = siguienteOffset < lista.Count;

            var respuesta = new RespuestaCatalogoDto
            {
                Success = true,
                Data = pagina,
                Meta = new MetaCatalogo
                {
                    Total = lista.Count,
                    HasNextPage = hayMas,
                    NextCursor = hayMas ? siguienteOffset.ToString() : null
                }
            };

            return Task.FromResult(respuesta);
        }

        // Devuelve todo el catálogo (para el filtrado en cliente de "Explorar":
        // búsqueda instantánea, filtros combinados y contadores dinámicos en JS).
        public IReadOnlyList<EjercicioCatalogoDto> ObtenerTodos() => Catalogo;

        // Obtiene el detalle de un ejercicio por su id (búsqueda en memoria).
        public Task<EjercicioCatalogoDto?> ObtenerDetalleAsync(string exerciseId)
        {
            var ejercicio = Catalogo.FirstOrDefault(e =>
                e.ExerciseId.Equals(exerciseId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(ejercicio);
        }

        // Dado un conjunto de (ejercicioId, exerciseDbId), devuelve un mapa
        // ejercicioId -> gifUrl para los ejercicios que tienen un vínculo
        // resolvible en el seed. Se usa para mostrar los GIFs durante el
        // entrenamiento y en el detalle de rutina. Es síncrono: opera en memoria.
        public Dictionary<int, string> ResolverGifs(
            IEnumerable<(int EjercicioId, string? ExerciseDbId)> ejercicios)
        {
            var mapa = new Dictionary<int, string>();

            foreach (var (ejercicioId, exerciseDbId) in ejercicios)
            {
                if (string.IsNullOrEmpty(exerciseDbId)) continue;

                var detalle = Catalogo.FirstOrDefault(e =>
                    e.ExerciseId.Equals(exerciseDbId, StringComparison.OrdinalIgnoreCase));

                if (detalle != null && !string.IsNullOrEmpty(detalle.GifUrl))
                    mapa[ejercicioId] = detalle.GifUrl;
            }

            return mapa;
        }

        // Lista las partes del cuerpo disponibles (para los filtros), ordenadas.
        public Task<List<string>> ListarBodyPartsAsync()
        {
            var partes = Catalogo
                .SelectMany(e => e.BodyParts)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult(partes);
        }

        // Lee y deserializa el seed local. Se ejecuta una sola vez (lo cachea la
        // propiedad Catalogo). Si el archivo falta o está corrupto, lanza para
        // que el fallo sea visible en desarrollo (no un catálogo vacío silencioso).
        private List<EjercicioCatalogoDto> CargarDesdeArchivo()
        {
            var ruta = seedFiles.GetSeedFilePath("exercises.json");
            var json = File.ReadAllText(ruta);
            return JsonSerializer.Deserialize<List<EjercicioCatalogoDto>>(json, JsonOpts)
                ?? new List<EjercicioCatalogoDto>();
        }
    }
}
