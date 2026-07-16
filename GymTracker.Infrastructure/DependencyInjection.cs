using GymTracker.Application.Abstractions;
using GymTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker.Infrastructure
{
    // Composición de la capa de Infraestructura (ADR-03).
    //
    // Program.cs (Web) llama a AddInfrastructure(...) para registrar la
    // persistencia sin conocer EF Core ni Npgsql directamente: esos detalles
    // quedan encapsulados aquí. Registra el ApplicationDbContext con PostgreSQL y
    // expone IApplicationDbContext apuntando a la misma instancia del DbContext.
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // La abstracción de Application se resuelve al mismo DbContext ya
            // registrado (scoped), para que los servicios reciban IApplicationDbContext.
            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            return services;
        }
    }
}
