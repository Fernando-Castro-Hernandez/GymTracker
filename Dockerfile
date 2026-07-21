# Empaquetado de GymTracker para producción (ADR-09).
#
# Build multi-etapa: se compila con el SDK completo pero la imagen final sólo
# lleva el runtime de ASP.NET y los .dll publicados. Medido en este proyecto:
# imagen final de 447 MB, de los que 78 MB son la aplicación publicada. No
# contiene código fuente ni compilador, con lo que se descarga más rápido en
# cada despliegue y se reduce la superficie de ataque.
#
# Construir en local (desde la raíz del repo):
#   docker build -t gymtracker .

# ==========================================================================
# Etapa 1 — build: compila y publica la aplicación
# ==========================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copian PRIMERO sólo los archivos de proyecto y la solución, y se restauran
# los paquetes antes de traer el código. Docker cachea cada instrucción: mientras
# no cambien los .csproj, la capa de restore se reutiliza y `docker build` no
# vuelve a descargar NuGet en cada despliegue. Copiar todo de golpe invalidaría
# ese caché con cualquier cambio de una sola línea de código.
COPY GymTracker.slnx ./
COPY GymTracker.Domain/GymTracker.Domain.csproj                 GymTracker.Domain/
COPY GymTracker.Application/GymTracker.Application.csproj       GymTracker.Application/
COPY GymTracker.Infrastructure/GymTracker.Infrastructure.csproj GymTracker.Infrastructure/
COPY GymTracker.Web/GymTracker.Web.csproj                       GymTracker.Web/

# Sólo se restaura Web: arrastra Application e Infrastructure por referencias.
# GymTracker.Tests se excluye a propósito — las pruebas ya corrieron en el
# pipeline de CI (ADR-08) y no deben viajar al servidor.
RUN dotnet restore GymTracker.Web/GymTracker.Web.csproj

# Ahora sí, el código fuente.
COPY GymTracker.Domain/         GymTracker.Domain/
COPY GymTracker.Application/    GymTracker.Application/
COPY GymTracker.Infrastructure/ GymTracker.Infrastructure/
COPY GymTracker.Web/            GymTracker.Web/

# publish deja en /app/publish los .dll, las vistas Razor compiladas, wwwroot y
# SeedData/exercises.json (el .csproj lo marca con CopyToPublishDirectory, que
# WebSeedFileProvider necesita para resolver ContentRootPath/SeedData).
RUN dotnet publish GymTracker.Web/GymTracker.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ==========================================================================
# Etapa 2 — final: sólo el runtime y el resultado de la publicación
# ==========================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# La app corre como usuario sin privilegios, no como root. Si alguien lograra
# ejecutar código dentro del contenedor, no tendría permisos administrativos.
# La imagen de Microsoft ya trae creado el usuario 'app'.
USER app

# Kestrel escucha en el 8080 dentro del contenedor. Caddy (el proxy inverso que
# termina el TLS) es quien recibe el tráfico de internet en 80/443 y lo reenvía
# aquí. Se usa 8080 y no 80 porque un usuario sin privilegios no puede abrir
# puertos por debajo del 1024.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Producción: ASP.NET Core NO carga User Secrets y no muestra páginas de error
# detalladas. La configuración sensible llega por variables de entorno con doble
# guion bajo (ConnectionStrings__DefaultConnection, Anthropic__ApiKey,
# Gemini__ApiKey), que un script en la EC2 lee de SSM Parameter Store.
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GymTracker.Web.dll"]
