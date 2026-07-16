using GymTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Application.Abstractions
{
    // Abstracción de acceso a datos (ADR-03).
    //
    // Vive en la capa de Aplicación y la implementa ApplicationDbContext en
    // Infrastructure. Los servicios dependen de esta interfaz —no del DbContext
    // concreto—, de modo que Application NO referencia a Infrastructure y la
    // dirección de dependencia (Web -> Application -> Domain) se mantiene.
    //
    // Expone los DbSet<> que consumen los servicios más SaveChangesAsync. Las
    // operaciones LINQ async (Include, Where, FirstOrDefaultAsync, ToListAsync)
    // funcionan sobre estos DbSet porque Application referencia EF Core.
    public interface IApplicationDbContext
    {
        DbSet<Ejercicio> Ejercicios { get; }
        DbSet<Rutina> Rutinas { get; }
        DbSet<RutinaEjercicio> RutinasEjercicios { get; }
        DbSet<Sesion> Sesiones { get; }
        DbSet<SerieRealizada> SeriesRealizadas { get; }
        DbSet<Medicion> Mediciones { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
