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
        }
    }
}