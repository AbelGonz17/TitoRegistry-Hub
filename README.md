# TitoRegistry-Hub

Plataforma empresarial para el registro y control de servicios prestados por empleados, permitiendo el seguimiento de actividades y acumulados de comisiones/ingresos.

---

## 🏛️ Arquitectura del Backend (.NET 10 - Clean Architecture)

El backend está organizado siguiendo los principios de **Clean Architecture**:

```
backend/
├── TitoRegistry.sln
└── src/
    ├── TitoRegistry.Domain/             # Núcleo del dominio (entidades base, reglas de negocio puras)
    │   └── Common/
    │       └── BaseEntity.cs
    ├── TitoRegistry.Application/        # Casos de uso, interfaces, DTOs y lógica de aplicación
    │   ├── Common/Interfaces/
    │   │   └── IApplicationDbContext.cs # Abstracción del contexto de base de datos
    │   └── DependencyInjection.cs
    ├── TitoRegistry.Infrastructure/     # Acceso a datos, persistencia y servicios externos
    │   ├── Persistence/
    │   │   └── ApplicationDbContext.cs  # Implementación EF Core (SQLite / relacional)
    │   └── DependencyInjection.cs
    └── TitoRegistry.Api/                # Capa de presentación (ASP.NET Core Web API, Controllers, Swagger, CORS)
        ├── Controllers/
        │   └── StatusController.cs
        ├── appsettings.json
        └── Program.cs
```

---

## 🌐 Estructura del Frontend (Vue 3 + Vite)

El frontend se encuentra en la carpeta `frontend/` listo para integrarse una vez definidas las entidades y contratos de API.

---

## 🚀 Cómo ejecutar el Backend

Desde la carpeta `backend/`:

```bash
dotnet restore
dotnet build
dotnet run --project src/TitoRegistry.Api
```

El endpoint de verificación estará disponible en:
- `http://localhost:5107/api/status`
