using GymTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Ejercicio> Ejercicios { get; set; }
        public DbSet<Rutina> Rutinas { get; set; }
        public DbSet<RutinaEjercicio> RutinasEjercicios { get; set; }
        public DbSet<Sesion> Sesiones { get; set; }
        public DbSet<SerieRealizada> SeriesRealizadas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== Relación Rutina (1) ←→ (N) RutinaEjercicio =====
            // Al borrar una Rutina, se borran sus RutinaEjercicio en cascada.
            builder.Entity<RutinaEjercicio>()
                .HasOne(re => re.Rutina)
                .WithMany(r => r.Ejercicios)
                .HasForeignKey(re => re.RutinaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Relación Ejercicio (1) ←→ (N) RutinaEjercicio =====
            // Al intentar borrar un Ejercicio que está en uso, la BD lo impide.
            // El usuario debe quitarlo de las rutinas primero.
            builder.Entity<RutinaEjercicio>()
                .HasOne(re => re.Ejercicio)
                .WithMany()
                .HasForeignKey(re => re.EjercicioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Precisión del campo PesoObjetivo =====
            // PostgreSQL usará numeric(6,2): hasta 9999.99 kg, suficiente.
            builder.Entity<RutinaEjercicio>()
                .Property(re => re.PesoObjetivo)
                .HasPrecision(6, 2);

            // ===== Relación Sesion (1) ←→ (N) SerieRealizada =====
            // Al borrar una Sesion, se borran sus series en cascada:
            // una serie no tiene sentido fuera de su sesión.
            builder.Entity<SerieRealizada>()
                .HasOne(s => s.Sesion)
                .WithMany(se => se.Series)
                .HasForeignKey(s => s.SesionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Precisión de los campos decimales de SerieRealizada =====
            builder.Entity<SerieRealizada>()
                .Property(s => s.PesoObjetivo)
                .HasPrecision(6, 2);

            builder.Entity<SerieRealizada>()
                .Property(s => s.PesoReal)
                .HasPrecision(6, 2);

            // NOTA sobre la relación Rutina → Sesion:
            // NO se define una relación con clave foránea real. La Sesion guarda
            // RutinaId como un simple int? (nullable) sin navegación obligatoria,
            // porque es un snapshot: debe sobrevivir aunque la Rutina se borre.
            // Por eso tampoco hay EjercicioId como FK en SerieRealizada: solo se
            // guarda como dato de trazabilidad, no como relación con integridad
            // referencial que impida borrar el ejercicio.
        }
    }
}
