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

        // Configuración de Empleado
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

        // Configuración de Tipo de Servicio
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

        // Configuración de Registro de Servicio
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
        // Empleados iniciales
        var emp1 = new
        {
            Id = 1,
            Code = "EMP-001",
            FullName = "Tito González",
            Email = "tito.gonzalez@empresa.com",
            Phone = "+1 (555) 234-8901",
            Role = "Especialista Técnico Senior",
            Department = "Infraestructura & Redes",
            CommissionRate = 25.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var emp2 = new
        {
            Id = 2,
            Code = "EMP-002",
            FullName = "Sofía Ramírez",
            Email = "sofia.ramirez@empresa.com",
            Phone = "+1 (555) 456-1122",
            Role = "Ingeniera de Soporte",
            Department = "Soporte Técnico",
            CommissionRate = 22.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var emp3 = new
        {
            Id = 3,
            Code = "EMP-003",
            FullName = "Carlos Mendoza",
            Email = "carlos.mendoza@empresa.com",
            Phone = "+1 (555) 789-3344",
            Role = "Técnico de Campo",
            Department = "Mantenimiento",
            CommissionRate = 20.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<Employee>().HasData(emp1, emp2, emp3);

        // Catálogo de tipos de servicios estándar
        var st1 = new
        {
            Id = 1,
            Name = "Instalación de Fibra Óptica y Enlace de Red",
            Category = "Redes",
            DefaultPrice = 450.0m,
            Description = "Montaje de rack, cableado estructurado Cat6A/Fibra y configuración de routers.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st2 = new
        {
            Id = 2,
            Name = "Mantenimiento Preventivo de Servidores",
            Category = "Mantenimiento",
            DefaultPrice = 320.0m,
            Description = "Limpieza física, revisión de redundancia, actualización de firmware y logs.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st3 = new
        {
            Id = 3,
            Name = "Diagnóstico y Reparación de Hardware",
            Category = "Soporte",
            DefaultPrice = 180.0m,
            Description = "Reemplazo de componentes, fuentes de poder y pruebas de banco.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var st4 = new
        {
            Id = 4,
            Name = "Instalación de Sistema CCTV / Cámaras IP",
            Category = "Seguridad",
            DefaultPrice = 520.0m,
            Description = "Configuración de NVR, cámaras PoE y acceso móvil remoto.",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<ServiceType>().HasData(st1, st2, st3, st4);

        // Servicios iniciales para Tito González (EMP-001) para que ya tenga acumulado demostrable
        var srv1 = new
        {
            Id = 1,
            ServiceCode = "SRV-2026-001",
            EmployeeId = 1,
            ServiceTypeId = 1, // Redes ($450)
            ClientName = "Corporación Andina S.A.",
            ClientAddress = "Av. Principal #405",
            ClientPhone = "+1 555-901-2001",
            ServiceDate = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
            PriceCharged = 480.0m,
            EmployeeEarnings = 120.0m, // 25% de 480
            Status = ServiceStatus.Completado,
            Notes = "Instalación de 8 puntos de red y prueba de ancho de banda.",
            CreatedAt = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var srv2 = new
        {
            Id = 2,
            ServiceCode = "SRV-2026-002",
            EmployeeId = 1,
            ServiceTypeId = 4, // CCTV ($520)
            ClientName = "Logística Express del Norte",
            ClientAddress = "Zona Industrial Este, Galpón 4",
            ClientPhone = "+1 555-901-2002",
            ServiceDate = new DateTime(2026, 10, 5, 14, 0, 0, DateTimeKind.Utc),
            PriceCharged = 550.0m,
            EmployeeEarnings = 137.5m, // 25% de 550
            Status = ServiceStatus.Completado,
            Notes = "Instalación de 6 cámaras IP con visión nocturna calibrada.",
            CreatedAt = new DateTime(2026, 10, 5, 18, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var srv3 = new
        {
            Id = 3,
            ServiceCode = "SRV-2026-003",
            EmployeeId = 1,
            ServiceTypeId = 2, // Mantenimiento
            ClientName = "Financiera Futuro",
            ClientAddress = "Torre Empresarial, Nivel 14",
            ClientPhone = "+1 555-901-2003",
            ServiceDate = new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc),
            PriceCharged = 350.0m,
            EmployeeEarnings = 87.5m, // 25% de 350
            Status = ServiceStatus.Completado,
            Notes = "Mantenimiento preventivo semestral de servidores en rack.",
            CreatedAt = new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<ServiceRecord>().HasData(srv1, srv2, srv3);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
