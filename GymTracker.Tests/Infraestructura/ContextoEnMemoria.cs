using GymTracker.Application.Abstractions;
using GymTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Tests.Infraestructura
{
    // DbContext de PRUEBA que implementa IApplicationDbContext con el proveedor
    // InMemory de EF Core.
    //
    // Existe gracias al ADR-03: los servicios dependen de la abstracción
    // IApplicationDbContext y no del ApplicationDbContext concreto, que vive en
    // Infrastructure. Eso permite probar la capa Application sin referenciar
    // Infrastructure, sin PostgreSQL y sin Docker — la dirección de dependencias
    // (Tests -> Application -> Domain) se mantiene intacta.
    //
    // Limitación asumida: InMemory NO valida SQL, claves foráneas ni
    // restricciones de la base. No sirve para probar el esquema. Sí ejecuta las
    // consultas LINQ de verdad, que es exactamente lo que se quiere verificar
    // aquí: que el .Where(r => r.UsuarioId == usuarioId) esté presente.
    public class ContextoEnMemoria : DbContext, IApplicationDbContext
    {
        public ContextoEnMemoria(DbContextOptions<ContextoEnMemoria> options)
            : base(options) { }

        public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
        public DbSet<Rutina> Rutinas => Set<Rutina>();
        public DbSet<RutinaEjercicio> RutinasEjercicios => Set<RutinaEjercicio>();
        public DbSet<Sesion> Sesiones => Set<Sesion>();
        public DbSet<SerieRealizada> SeriesRealizadas => Set<SerieRealizada>();
        public DbSet<Medicion> Mediciones => Set<Medicion>();
        public DbSet<ChatMensaje> ChatMensajes => Set<ChatMensaje>();

        // Cada prueba recibe una base con nombre único (un Guid), de modo que las
        // pruebas quedan aisladas entre sí y pueden correr en cualquier orden.
        public static ContextoEnMemoria Crear()
        {
            var options = new DbContextOptionsBuilder<ContextoEnMemoria>()
                .UseInMemoryDatabase($"gymtracker-test-{Guid.NewGuid()}")
                .Options;

            return new ContextoEnMemoria(options);
        }
    }
}
