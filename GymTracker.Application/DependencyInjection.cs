using GymTracker.Application.Services.Ejercicios;
using GymTracker.Application.Services.Mediciones;
using GymTracker.Application.Services.Rutinas;
using GymTracker.Application.Services.Sesiones;
using GymTracker.Services.Catalogo;
using GymTracker.Services.IA;
using GymTracker.Services.Progreso;
using GymTracker.Services.Volumen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymTracker.Application
{
    // Composición de la capa de Aplicación (ADR-03).
    //
    // Program.cs (Web) llama a AddApplication(config) para registrar todos los
    // servicios de negocio en un solo lugar, en vez de esparcir los AddScoped por
    // la capa web. Recibe IConfiguration para las API keys de los proveedores de IA
    // (User Secrets en dev / variables de entorno en prod).
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services, IConfiguration configuration)
        {
            // ===== Servicios de dominio (deuda técnica #2 del ADR-06) =====
            services.AddScoped<IEjercicioService, EjercicioService>();
            services.AddScoped<IRutinaService, RutinaService>();
            services.AddScoped<ISesionService, SesionService>();
            services.AddScoped<IMedicionService, MedicionService>();

            // ===== Progreso (gráficas) =====
            services.AddScoped<ProgresoService>();

            // ===== Cálculo de volumen (ADR-05: Strategy + Factory) =====
            services.AddScoped<CalculoVolumenFactory>();

            // ===== Coach IA =====
            // Proveedor con fallback Claude -> Gemini. Las API keys vienen de
            // configuración, nunca del código.
            services.AddScoped<IProveedorIA>(sp =>
            {
                var claudeKey = configuration["Anthropic:ApiKey"]
                    ?? throw new InvalidOperationException("Falta la API key de Anthropic (Anthropic:ApiKey).");
                var geminiKey = configuration["Gemini:ApiKey"]
                    ?? throw new InvalidOperationException("Falta la API key de Gemini (Gemini:ApiKey).");

                var proveedores = new List<IProveedorIA>
                {
                    new ClaudeProveedor(claudeKey),
                    new GeminiProveedor(geminiKey)
                };

                var logger = sp.GetRequiredService<ILogger<ProveedorIAConFallback>>();
                return new ProveedorIAConFallback(proveedores, logger);
            });

            services.AddScoped<CoachService>();

            // ===== Chatbot con contexto (Integración 4, ADR-07) =====
            // Reutiliza el mismo IProveedorIA (gateway con fallback) del Coach.
            services.AddScoped<ContextoChatBuilder>();
            services.AddScoped<ChatService>();

            // ===== Catálogo de ejercicios (seed local + caché en memoria) =====
            services.AddMemoryCache();
            services.AddScoped<CatalogoService>();

            return services;
        }
    }
}
