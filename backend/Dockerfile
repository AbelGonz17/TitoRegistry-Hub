# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar proyectos y restaurar paquetes (capas cacheadas)
COPY TitoRegistry.slnx ./
COPY src/TitoRegistry.Domain/TitoRegistry.Domain.csproj src/TitoRegistry.Domain/
COPY src/TitoRegistry.Application/TitoRegistry.Application.csproj src/TitoRegistry.Application/
COPY src/TitoRegistry.Infrastructure/TitoRegistry.Infrastructure.csproj src/TitoRegistry.Infrastructure/
COPY src/TitoRegistry.Api/TitoRegistry.Api.csproj src/TitoRegistry.Api/

RUN dotnet restore src/TitoRegistry.Api/TitoRegistry.Api.csproj

# Copiar el codigo fuente completo
COPY src/ ./src/

# Compilar y publicar en modo Release
WORKDIR /app/src/TitoRegistry.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime ligero
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5107
EXPOSE 5107

ENTRYPOINT ["dotnet", "TitoRegistry.Api.dll"]
