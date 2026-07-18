using GymTracker.Application;
using GymTracker.Application.Abstractions;
using GymTracker.Data;
using GymTracker.Infrastructure;
using GymTracker.Web.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== Composición por capas (ADR-03) =====
// Program.cs es el composition root: arma la inyección de dependencias llamando a
// las extensiones de cada capa, sin conocer los detalles de persistencia ni de los
// servicios de negocio.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Infraestructura: ApplicationDbContext (PostgreSQL) + IApplicationDbContext.
builder.Services.AddInfrastructure(connectionString);

// Aplicación: servicios de negocio (dominio, progreso, volumen, IA, catálogo).
builder.Services.AddApplication(builder.Configuration);

// Localizador de archivos de seed (implementa la abstracción de Application con
// el content root del host).
builder.Services.AddScoped<ISeedFileProvider, WebSeedFileProvider>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// API: registra el explorador de endpoints y el generador de Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== Rate limiting del chatbot (ADR-07, guardarriel determinista) =====
// Política "chat": ventana fija de 10 peticiones por minuto, particionada por
// usuario. Acota bucles/abuso y el costo de las llamadas a la IA sin afectar al
// resto de la app (solo la aplica [EnableRateLimiting("chat")] en ChatApiController).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("chat", httpContext =>
    {
        var clave = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anon";

        return RateLimitPartition.GetFixedWindowLimiter(clave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // Respuesta amable cuando se supera el límite (en vez de un 429 vacío).
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Vas muy rápido. Espera un momento antes de enviar otro mensaje.\"}",
            token);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    // API: expone el JSON de OpenAPI y la interfaz de Swagger (solo en desarrollo)
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Rate limiter después de la autorización: la partición "chat" usa la identidad
// del usuario ya resuelta.
app.UseRateLimiter();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// API: mapea los controllers con rutas por atributo (los [Route("api/...")])
app.MapControllers();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
