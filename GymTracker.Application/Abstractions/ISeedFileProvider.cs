namespace GymTracker.Application.Abstractions
{
    // Abstracción para localizar archivos de datos semilla (seed) en disco.
    //
    // El catálogo de ejercicios se lee de SeedData/exercises.json, que en runtime
    // vive en el content root del proyecto Web. Resolver esa ruta requiere conocer
    // el entorno de hosting (IWebHostEnvironment), detalle que pertenece a la capa
    // de presentación. Esta interfaz permite que CatalogoService (Application)
    // pida la ruta sin depender de ASP.NET Core; Web la implementa.
    public interface ISeedFileProvider
    {
        // Devuelve la ruta absoluta de un archivo dentro de la carpeta SeedData.
        string GetSeedFilePath(string fileName);
    }
}
