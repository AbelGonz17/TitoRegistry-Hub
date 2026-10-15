# TitoRegistry-Hub

Plataforma empresarial para el registro y control de servicios prestados por empleados, permitiendo el seguimiento de actividades y acumulados de comisiones/ingresos.

---

## 🏛️ Arquitectura del Backend (.NET 10 - Clean Architecture)

El backend está organizado siguiendo los principios de **Clean Architecture**:

```
backend/
├── TitoRegistry.slnx
├── Dockerfile
└── src/
    ├── TitoRegistry.Domain/             # Núcleo del dominio (Employee, ServiceType, ServiceRecord, ServiceStatus)
    ├── TitoRegistry.Application/        # Casos de uso, interfaces, DTOs y lógica de registro/acumulados
    ├── TitoRegistry.Infrastructure/     # EF Core con PostgreSQL (Npgsql), mapeos y migraciones
    └── TitoRegistry.Api/                # ASP.NET Core Web API, Controllers y Swagger UI
```

---

## 🐳 Inicio Rápido con Docker Compose (Backend + PostgreSQL)

Para levantar la base de datos PostgreSQL y la API del Backend en contenedores:

```bash
docker compose up --build -d
```

### Servicios disponibles:
- **API Swagger UI**: [http://localhost:5107/swagger](http://localhost:5107/swagger)
- **Health Check**: [http://localhost:5107/api/status](http://localhost:5107/api/status)
- **PostgreSQL**: Puerto `5432` (`Database: titoregistry_db`, `User: postgres`, `Pass: postgres`)

Para ver los logs:
```bash
docker compose logs -f
```

Para detener los servicios:
```bash
docker compose down
```

---

## 💻 Ejecución Local (sin Docker para el Backend)

Si tienes PostgreSQL corriendo (o usando solo el servicio postgres de docker: `docker compose up -d postgres`):

Desde la carpeta `backend/`:

```bash
dotnet restore
dotnet build
dotnet run --project src/TitoRegistry.Api
```
