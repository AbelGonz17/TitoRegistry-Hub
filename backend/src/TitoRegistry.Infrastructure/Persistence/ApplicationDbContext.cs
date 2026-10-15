using Microsoft.EntityFrameworkCore;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Domain.Entities;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Empleado (Conductores / Choferes)
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.CommissionRate).HasPrecision(5, 2);

            entity.HasMany(e => e.ServiceRecords)
                  .WithOne(s => s.Employee)
                  .HasForeignKey(s => s.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Tipo de Servicio de Transporte
        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.Category).IsRequired().HasMaxLength(100);
            entity.Property(s => s.DefaultPrice).HasPrecision(18, 2);
            entity.Property(s => s.Description).HasMaxLength(500);

            entity.HasMany(t => t.ServiceRecords)
                  .WithOne(s => s.ServiceType)
                  .HasForeignKey(s => s.ServiceTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Registro de Servicio Realizado
        modelBuilder.Entity<ServiceRecord>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ServiceCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(s => s.ServiceCode).IsUnique();

            entity.Property(s => s.ClientName).IsRequired().HasMaxLength(150);
            entity.Property(s => s.ClientAddress).HasMaxLength(250);
            entity.Property(s => s.ClientPhone).HasMaxLength(50);

            entity.Property(s => s.PriceCharged).HasPrecision(18, 2);
            entity.Property(s => s.EmployeeEarnings).HasPrecision(18, 2);

            entity.Property(s => s.Status).HasConversion<int>();
            entity.Property(s => s.Notes).HasMaxLength(1000);

            entity.HasIndex(s => new { s.EmployeeId, s.ServiceDate });
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Conductores / Choferes de la empresa de transporte
        var emp1 = new
        {
            Id = 1,
            Code = "CHO-001",
            FullName = "Tito González",
            Email = "tito.gonzalez@titotransporte.com",
            Phone = "+1 (809) 555-0101",
            Role = "Conductor Ejecutivo de Rutas",
            Department = "Ruta Norte (Santiago)",
            CommissionRate = 25.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var emp2 = new
        {
            Id = 2,
            Code = "CHO-002",
            FullName = "Sofía Ramírez",
            Email = "sofia.ramirez@titotransporte.com",
            Phone = "+1 (809) 555-0102",
            Role = "Conductora de Transporte Interurbano",
            Department = "Ruta Cibao Central",
            CommissionRate = 22.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var emp3 = new
        {
            Id = 3,
            Code = "CHO-003",
            FullName = "Carlos Mendoza",
            Email = "carlos.mendoza@titotransporte.com",
            Phone = "+1 (809) 555-0103",
            Role = "Operador de Carga y Fletes",
            Department = "Transporte de Encomiendas",
            CommissionRate = 20.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<Employee>().HasData(emp1, emp2, emp3);

        // Catálogo de Servicios de Transporte
        var st1 = new
        {
            Id = 1,
            Name = "Traslado Privado / Ejecutivo a Santiago",
            Category = "Viajes Interurbanos",
            DefaultPrice = 180.0m,
            Description = "Servicio de transporte privado puerta a puerta hacia Santiago de los Caballeros en vehículo de confort.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st2 = new
        {
            Id = 2,
            Name = "Transporte Expreso de Pasajeros (Ruta Santiago)",
            Category = "Viajes Interurbanos",
            DefaultPrice = 95.0m,
            Description = "Servicio directo en van/minibús con paradas programadas hacia Santiago.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st3 = new
        {
            Id = 3,
            Name = "Envío de Paquetería y Encomienda Expresa a Santiago",
            Category = "Carga y Encomiendas",
            DefaultPrice = 45.0m,
            Description = "Transporte y entrega de paquetes, valijas y documentos el mismo día en Santiago.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st4 = new
        {
            Id = 4,
            Name = "Flete y Carga Liviana Interprovincial",
            Category = "Carga y Encomiendas",
            DefaultPrice = 280.0m,
            Description = "Transporte de mercancía comercial o carga ligera en vehículo utilitario cerrado.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st5 = new
        {
            Id = 5,
            Name = "Traslado Turístico / Aeropuerto (Santiago - Cibao)",
            Category = "Turismo & Aeropuerto",
            DefaultPrice = 130.0m,
            Description = "Servicio especializado de recogida y traslado desde o hacia el Aeropuerto del Cibao (STI).",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st6 = new
        {
            Id = 6,
            Name = "Alquiler de Vehículo con Conductor por Día (Santiago y Región)",
            Category = "Servicios Especiales",
            DefaultPrice = 350.0m,
            Description = "Disponibilidad completa de chofer y vehículo para gestiones corporativas o familiares en Santiago.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<ServiceType>().HasData(st1, st2, st3, st4, st5, st6);

        // Servicios iniciales de transporte registrados para Tito González (CHO-001)
        var srv1 = new
        {
            Id = 1,
            ServiceCode = "SRV-2026-001",
            EmployeeId = 1,
            ServiceTypeId = 1, // Traslado Ejecutivo a Santiago
            ClientName = "Ing. Manuel De Los Santos",
            ClientAddress = "Santo Domingo -> Los Jardines Metropolitanos, Santiago",
            ClientPhone = "+1 (809) 555-1020",
            ServiceDate = new DateTime(2026, 9, 18, 8, 30, 0, DateTimeKind.Utc),
            PriceCharged = 180.0m,
            EmployeeEarnings = 45.0m, // 25% de 180
            Status = ServiceStatus.Completado,
            Notes = "Viaje ejecutivo de ida a Santiago sin contratiempos.",
            CreatedAt = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var srv2 = new
        {
            Id = 2,
            ServiceCode = "SRV-2026-002",
            EmployeeId = 1,
            ServiceTypeId = 6, // Conductor por día en Santiago
            ClientName = "Grupo Farmacéutico Cibao",
            ClientAddress = "Av. Juan Pablo Duarte #50, Santiago",
            ClientPhone = "+1 (809) 555-2040",
            ServiceDate = new DateTime(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc),
            PriceCharged = 350.0m,
            EmployeeEarnings = 87.5m, // 25% de 350
            Status = ServiceStatus.Completado,
            Notes = "Recorrido de sucursales en Santiago durante toda la jornada.",
            CreatedAt = new DateTime(2026, 10, 6, 18, 30, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var srv3 = new
        {
            Id = 3,
            ServiceCode = "SRV-2026-003",
            EmployeeId = 1,
            ServiceTypeId = 3, // Encomienda Expresa a Santiago
            ClientName = "Distribuidora Nacional del Norte",
            ClientAddress = "Zona Franca Industrial Víctor Espaillat Mera, Santiago",
            ClientPhone = "+1 (809) 555-3050",
            ServiceDate = new DateTime(2026, 10, 14, 11, 0, 0, DateTimeKind.Utc),
            PriceCharged = 50.0m,
            EmployeeEarnings = 12.5m, // 25% de 50
            Status = ServiceStatus.Completado,
            Notes = "Entrega de documentos prioritarios con firma de recepción.",
            CreatedAt = new DateTime(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<ServiceRecord>().HasData(srv1, srv2, srv3);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
