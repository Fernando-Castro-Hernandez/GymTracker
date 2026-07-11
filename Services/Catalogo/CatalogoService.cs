using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GymTracker.Services.Catalogo
{
    // Servicio que consume el catálogo externo de ejercicios (ExerciseDB).
    // Usa IHttpClientFactory (cliente "ExerciseDB") y cachea las respuestas en
    // memoria para reducir llamadas externas y protegerse de la latencia/caídas
    // del tercero.
    public class CatalogoService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        private static readonly TimeSpan DuracionCache = TimeSpan.FromHours(6);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Lista ejercicios, opcionalmente filtrados por parte del cuerpo, con
        // paginación por cursor. Cachea cada combinación de filtro+cursor.
        public async Task<RespuestaCatalogoDto> ListarAsync(
            string? bodyPart = null, string? cursor = null, int limit = 12)
        {
            // Clave de caché única por combinación de parámetros.
            var claveCache = $"catalogo:list:{bodyPart}:{cursor}:{limit}";

            if (cache.TryGetValue(claveCache, out RespuestaCatalogoDto? cacheada) && cacheada != null)
                return cacheada;

            // Construir la ruta relativa con los parámetros presentes.
            var query = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(bodyPart)) query.Add($"bodyParts={Uri.EscapeDataString(bodyPart)}");
            if (!string.IsNullOrWhiteSpace(cursor)) query.Add($"after={Uri.EscapeDataString(cursor)}");
            var ruta = "exercises?" + string.Join("&", query);

            var respuesta = await ObtenerAsync<RespuestaCatalogoDto>(ruta)
                ?? new RespuestaCatalogoDto();

            cache.Set(claveCache, respuesta, DuracionCache);
            return respuesta;
        }

        // Obtiene el detalle de un ejercicio por su id. Cacheado por id.
        public async Task<EjercicioCatalogoDto?> ObtenerDetalleAsync(string exerciseId)
        {
            var claveCache = $"catalogo:detalle:{exerciseId}";

            if (cache.TryGetValue(claveCache, out EjercicioCatalogoDto? cacheado) && cacheado != null)
                return cacheado;

            // El detalle viene envuelto en { success, data: {...} } (objeto, no lista).
            var envoltorio = await ObtenerAsync<RespuestaDetalleDto>($"exercises/{Uri.EscapeDataString(exerciseId)}");
            var detalle = envoltorio?.Data;

            if (detalle != null)
                cache.Set(claveCache, detalle, DuracionCache);

            return detalle;
        }

        // Lista las partes del cuerpo disponibles (para los filtros). Cacheado.
        public async Task<List<string>> ListarBodyPartsAsync()
        {
            const string claveCache = "catalogo:bodyparts";

            if (cache.TryGetValue(claveCache, out List<string>? cacheadas) && cacheadas != null)
                return cacheadas;

            var envoltorio = await ObtenerAsync<RespuestaNombresDto>("bodyparts");
            var lista = envoltorio?.Data.Select(x => x.Name).ToList() ?? new List<string>();

            cache.Set(claveCache, lista, DuracionCache);
            return lista;
        }

        // Helper genérico: hace GET al cliente "ExerciseDB" y deserializa.
        // Devuelve null si falla (el llamador decide cómo manejarlo).
        private async Task<T?> ObtenerAsync<T>(string ruta) where T : class
        {
            var client = httpClientFactory.CreateClient("ExerciseDB");
            var resp = await client.GetAsync(ruta);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
    }
}