using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace GymTracker.Services.Catalogo
{
    // Servicio que consume el catálogo externo de ejercicios (ExerciseDB OSS).
    //
    // ARQUITECTURA (ADR-06): en lugar de paginar contra la API bajo demanda,
    // este servicio carga el catálogo COMPLETO una sola vez y lo mantiene en
    // caché en memoria durante 7 días ("despensa"). Todos los filtros, búsquedas
    // y conteos se resuelven con LINQ sobre esa copia local. Esto:
    //   - Desacopla el número de usuarios del consumo de la API externa.
    //   - Habilita contadores dinámicos y búsqueda instantánea (imposibles con
    //     paginación bajo demanda, que solo ve una página a la vez).
    //   - Da resiliencia: si la API se cae, seguimos sirviendo desde caché.
    //   - Resuelve el problema de idioma: la búsqueda por texto filtra localmente
    //     con Contains, sin depender de que la API entienda español.
    public class CatalogoService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        // El catálogo es casi estático (un press de banca no cambia), así que
        // 7 días es un buen equilibrio: se refresca solo por si agregan ejercicios,
        // manteniendo el consumo de la API en ~8-20 llamadas por semana.
        private static readonly TimeSpan DuracionCache = TimeSpan.FromDays(7);

        // Tamaño de página al paginar internamente contra la API durante la carga
        // completa. 25 es el máximo que acepta el endpoint gratuito.
        private const int TamanoPaginaInterna = 25;

        // Tope de seguridad para no entrar en un bucle infinito si la API devolviera
        // siempre hasNextPage=true. El dataset OSS ronda 1,500; 100 páginas de 25
        // (2,500 ejercicios) es un margen holgado.
        private const int MaxPaginas = 100;

        private const string ClaveCatalogoCompleto = "catalogo:completo";
        private const string ClaveCatalogoParcial = "catalogo:parcial";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Pausa entre páginas durante la carga completa (respeta rate limit).
        private const int DelayEntrePaginasMs = 350;

        // Reintentos ante un 429 y espera base del backoff.
        private const int MaxReintentos = 3;
        private const int DelayBackoffBaseMs = 1500;

        // TTL corto cuando la carga quedó parcial (para reintentar completar pronto).
        private static readonly TimeSpan DuracionCacheParcial = TimeSpan.FromMinutes(5);

        // ===================================================================
        //  LA DESPENSA: carga el catálogo completo (privado)
        // ===================================================================

        // Trae TODO el catálogo una sola vez, paginando internamente hasta agotar
        // el cursor de la API, y lo cachea 7 días. Las siguientes llamadas devuelven
        // la copia cacheada al instante sin tocar la red.
        //
        // RESILIENCIA (ADR-06): el OSS aplica rate limiting (429). Para ser buenos
        // ciudadanos y no ser bloqueados:
        //   - Metemos una pausa entre páginas (no disparamos ráfagas).
        //   - Ante un 429, reintentamos esa página con espera creciente (backoff).
        //   - Si aun así no completamos, cacheamos lo parcial con TTL corto para
        //     no volver a martillar la API en cada recarga (rompe el bucle de 429).


        // ===================================================================
        //  LA DESPENSA: carga el catálogo completo (privado)
        // ===================================================================

        // Trae TODO el catálogo una sola vez, paginando internamente hasta agotar
        // el cursor de la API, y lo cachea 7 días. Las siguientes llamadas devuelven
        // la copia cacheada al instante sin tocar la red.
        //
        // RESILIENCIA + SALVAGUARDA (ADR-06): el OSS aplica rate limiting (429).
        //   - Pausa entre páginas (no disparamos ráfagas) + retry con backoff.
        //   - Solo cacheamos como CONFIABLE (7 días) si la API confirmó el final
        //     con hasNextPage=false. Si la carga se cortó por un fallo (429 tras
        //     reintentos, o tope de páginas), se considera PARCIAL: se cachea con
        //     TTL cortísimo para servir algo, pero la próxima petición reintentará
        //     traer el catálogo completo en vez de conformarse con lo incompleto.
        //   - No confiamos en un número mágico de ejercicios: la señal de "completo"
        //     es la propia API (hasNextPage), no un umbral arbitrario.
        private async Task<List<EjercicioCatalogoDto>> CargarCatalogoCompletoAsync()
        {
            // Si ya hay un catálogo COMPLETO cacheado, lo devolvemos.
            if (cache.TryGetValue(ClaveCatalogoCompleto, out List<EjercicioCatalogoDto>? cacheado)
                && cacheado != null)
                return cacheado;

            var acumulado = new List<EjercicioCatalogoDto>();
            string? cursor = null;
            var completo = false;

            for (var pagina = 0; pagina < MaxPaginas; pagina++)
            {
                var query = new List<string> { $"limit={TamanoPaginaInterna}" };
                if (!string.IsNullOrWhiteSpace(cursor))
                    query.Add($"after={Uri.EscapeDataString(cursor)}");
                var ruta = "exercises?" + string.Join("&", query);

                var respuesta = await ObtenerConReintentosAsync<RespuestaCatalogoDto>(ruta);

                // Página fallida tras reintentos: cortamos con lo que llevamos (parcial).
                if (respuesta?.Data == null || respuesta.Data.Count == 0)
                    break;

                acumulado.AddRange(respuesta.Data);

                // La API confirma que no hay más páginas: catálogo COMPLETO.
                if (respuesta.Meta?.HasNextPage != true
                    || string.IsNullOrWhiteSpace(respuesta.Meta.NextCursor))
                {
                    completo = true;
                    break;
                }

                cursor = respuesta.Meta.NextCursor;
                await Task.Delay(DelayEntrePaginasMs);
            }

            if (completo)
            {
                // Confiable: caché larga bajo la clave definitiva.
                cache.Set(ClaveCatalogoCompleto, acumulado, DuracionCache);
            }
            else if (acumulado.Count > 0)
            {
                // Parcial: NO lo guardamos como el catálogo completo (así la próxima
                // petición reintenta la carga entera). Lo guardamos aparte con TTL
                // corto solo para no dejar la pantalla vacía si algo lo consulta ya.
                cache.Set(ClaveCatalogoParcial, acumulado, DuracionCacheParcial);
            }
            // Si acumulado está vacío, no cacheamos nada: reintento inmediato.

            return acumulado;
        }

        // Indica si el catálogo COMPLETO y confiable ya está en caché. Útil para
        // que la vista avise "catálogo cargándose, algunos ejercicios pueden faltar".
        public bool CatalogoCompletoEnCache()
            => cache.TryGetValue(ClaveCatalogoCompleto, out List<EjercicioCatalogoDto>? c) && c != null;

        // Helper con reintentos ante 429 (rate limit). Espera creciente entre
        // intentos (backoff). Devuelve null si agota los reintentos.
        private async Task<T?> ObtenerConReintentosAsync<T>(string ruta) where T : class
        {
            for (var intento = 0; intento < MaxReintentos; intento++)
            {
                try
                {
                    return await ObtenerAsync<T>(ruta);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(DelayBackoffBaseMs * (intento + 1));
                }
            }
            return null;
        }



        // ===================================================================
        //  MÉTODOS NUEVOS: filtrado, conteo y búsqueda sobre la despensa
        //  (se usarán en B2, la UI de filtros mejorada)
        // ===================================================================

        // Devuelve el catálogo completo ya filtrado por los criterios dados.
        // Cualquier criterio nulo/vacío se ignora. Todos los filtros son en memoria.
        public async Task<List<EjercicioCatalogoDto>> FiltrarAsync(
            string? bodyPart = null,
            string? equipment = null,
            string? targetMuscle = null,
            string? texto = null)
        {
            var catalogo = await CargarCatalogoCompletoAsync();
            IEnumerable<EjercicioCatalogoDto> query = catalogo;

            if (!string.IsNullOrWhiteSpace(bodyPart))
                query = query.Where(e => e.BodyParts
                    .Any(b => b.Equals(bodyPart, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(equipment))
                query = query.Where(e => e.Equipments
                    .Any(eq => eq.Equals(equipment, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(targetMuscle))
                query = query.Where(e => e.TargetMuscles
                    .Any(m => m.Equals(targetMuscle, StringComparison.OrdinalIgnoreCase)));

            // Búsqueda por texto: coincidencia parcial e insensible a mayúsculas
            // sobre el nombre. Resuelve el flujo "ya sé qué ejercicio quiero".
            if (!string.IsNullOrWhiteSpace(texto))
                query = query.Where(e => e.Name
                    .Contains(texto, StringComparison.OrdinalIgnoreCase));

            return query.ToList();
        }

        // Cuenta cuántos ejercicios hay por cada parte del cuerpo, aplicando los
        // filtros transversales activos (equipamiento y texto) PERO NO el filtro
        // de bodyPart en sí. Así los contadores son dinámicos ("chest (8)" cuando
        // hay equipo filtrado) sin poner en cero las demás zonas, permitiendo al
        // usuario cambiarse a ellas. Devuelve un diccionario bodyPart -> cantidad.
        public async Task<Dictionary<string, int>> ContarPorBodyPartAsync(
            string? equipment = null, string? texto = null)
        {
            var catalogo = await CargarCatalogoCompletoAsync();
            IEnumerable<EjercicioCatalogoDto> query = catalogo;

            if (!string.IsNullOrWhiteSpace(equipment))
                query = query.Where(e => e.Equipments
                    .Any(eq => eq.Equals(equipment, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(texto))
                query = query.Where(e => e.Name
                    .Contains(texto, StringComparison.OrdinalIgnoreCase));

            return query
                .SelectMany(e => e.BodyParts)
                .GroupBy(b => b, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        }

        // Lista los valores únicos de equipamiento presentes en el catálogo
        // (para poblar el filtro de equipamiento).
        public async Task<List<string>> ListarEquipmentsAsync()
        {
            var catalogo = await CargarCatalogoCompletoAsync();
            return catalogo
                .SelectMany(e => e.Equipments)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        // Lista los músculos objetivo únicos, opcionalmente acotados a una parte
        // del cuerpo (para el sub-filtro dinámico de músculo específico).
        public async Task<List<string>> ListarTargetMusclesAsync(string? bodyPart = null)
        {
            var catalogo = await CargarCatalogoCompletoAsync();
            IEnumerable<EjercicioCatalogoDto> fuente = catalogo;

            if (!string.IsNullOrWhiteSpace(bodyPart))
                fuente = fuente.Where(e => e.BodyParts
                    .Any(b => b.Equals(bodyPart, StringComparison.OrdinalIgnoreCase)));

            return fuente
                .SelectMany(e => e.TargetMuscles)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        // ===================================================================
        //  MÉTODOS EXISTENTES (compatibilidad con el controller actual)
        //  ListarAsync conserva su firma pero ahora filtra sobre la despensa.
        // ===================================================================

        // Lista ejercicios, opcionalmente filtrados por parte del cuerpo.
        // NOTA: la firma conserva 'cursor' y 'limit' por compatibilidad con el
        // controller actual, pero tras el refactor la paginación por cursor de la
        // API ya no aplica (ahora tenemos el catálogo completo en memoria). En B2
        // se migrará el controller/vista al nuevo modelo y esta firma se limpiará.
        public async Task<RespuestaCatalogoDto> ListarAsync(
            string? bodyPart = null, string? cursor = null, int limit = 12)
        {
            var filtrados = await FiltrarAsync(bodyPart: bodyPart);

            // Empaquetamos en el DTO de siempre para no romper la vista actual.
            // Sin paginación por cursor: devolvemos todo lo filtrado de una vez.
            return new RespuestaCatalogoDto
            {
                Success = true,
                Data = filtrados,
                Meta = new MetaCatalogo
                {
                    Total = filtrados.Count,
                    HasNextPage = false,
                    NextCursor = null
                }
            };
        }

        // Obtiene el detalle de un ejercicio por su id. Primero busca en la
        // despensa (evita una llamada de red); si no está, va a la API. Cacheado por id.
        public async Task<EjercicioCatalogoDto?> ObtenerDetalleAsync(string exerciseId)
        {
            // Atajo: si el catálogo completo ya está en caché, el detalle probablemente
            // esté ahí y trae todos los campos (incluidas instructions).
            if (cache.TryGetValue(ClaveCatalogoCompleto, out List<EjercicioCatalogoDto>? completo)
                && completo != null)
            {
                var enDespensa = completo.FirstOrDefault(e =>
                    e.ExerciseId.Equals(exerciseId, StringComparison.OrdinalIgnoreCase));
                if (enDespensa != null && enDespensa.Instructions.Count > 0)
                    return enDespensa;
            }

            var claveCache = $"catalogo:detalle:{exerciseId}";
            if (cache.TryGetValue(claveCache, out EjercicioCatalogoDto? cacheado) && cacheado != null)
                return cacheado;

            var envoltorio = await ObtenerConReintentosAsync<RespuestaDetalleDto>(
                $"exercises/{Uri.EscapeDataString(exerciseId)}");
            var detalle = envoltorio?.Data;

            if (detalle != null)
                cache.Set(claveCache, detalle, DuracionCache);

            return detalle;
        }

        // Lista las partes del cuerpo disponibles (para los filtros), derivadas
        // de la despensa (catálogo completo en memoria). No hace llamada de red:
        // reutiliza la copia cacheada, lo que reduce el riesgo de rate limit (429)
        // y garantiza que solo aparezcan zonas que realmente tienen ejercicios.
        public async Task<List<string>> ListarBodyPartsAsync()
        {
            var catalogo = await CargarCatalogoCompletoAsync();
            return catalogo
                .SelectMany(e => e.BodyParts)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
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