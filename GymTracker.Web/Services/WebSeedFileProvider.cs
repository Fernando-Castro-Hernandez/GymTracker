using GymTracker.Application.Abstractions;

namespace GymTracker.Web.Services
{
    // Implementación de ISeedFileProvider para la capa web. Resuelve la ruta de
    // los archivos de SeedData respecto al content root del host (donde se copia
    // exercises.json en el build). Vive en Web porque IWebHostEnvironment es un
    // detalle de hosting que no debe filtrarse a la capa de Aplicación.
    public class WebSeedFileProvider(IWebHostEnvironment env) : ISeedFileProvider
    {
        public string GetSeedFilePath(string fileName) =>
            Path.Combine(env.ContentRootPath, "SeedData", fileName);
    }
}
