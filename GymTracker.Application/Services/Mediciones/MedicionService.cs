using GymTracker.Application.Abstractions;
using GymTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Application.Services.Mediciones
{
    // Implementación del servicio de Mediciones. Depende de IApplicationDbContext
    // (no del DbContext concreto), respetando la dirección de capas del ADR-03.
    public class MedicionService(IApplicationDbContext context) : IMedicionService
    {
        public async Task<List<Medicion>> ListarAsync(string usuarioId) =>
            await context.Mediciones
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

        public async Task<Medicion?> ObtenerAsync(int id, string usuarioId) =>
            await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

        public async Task CrearAsync(Medicion medicion)
        {
            context.Mediciones.Add(medicion);
            await context.SaveChangesAsync();
        }

        public async Task<bool> ActualizarAsync(int id, string usuarioId, Medicion datos)
        {
            // Cargar la medición original (rastreada) validando ownership.
            var original = await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

            if (original == null) return false;

            // Copiar solo los campos editables sobre la entidad rastreada.
            // No se toca UsuarioId ni Id: el ownership queda blindado.
            // La fecha llega ya normalizada a UTC desde el controller.
            original.Fecha = datos.Fecha == default ? original.Fecha : datos.Fecha;
            original.Peso = datos.Peso;
            original.PorcentajeGrasa = datos.PorcentajeGrasa;
            original.GrasaVisceral = datos.GrasaVisceral;
            original.MasaMuscular = datos.MasaMuscular;
            original.PorcentajeAgua = datos.PorcentajeAgua;
            original.Cintura = datos.Cintura;
            original.Cadera = datos.Cadera;
            original.Pecho = datos.Pecho;
            original.Brazo = datos.Brazo;
            original.Muslo = datos.Muslo;
            original.Notas = datos.Notas;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAsync(int id, string usuarioId)
        {
            var medicion = await context.Mediciones
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);

            if (medicion == null) return false;

            context.Mediciones.Remove(medicion);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
