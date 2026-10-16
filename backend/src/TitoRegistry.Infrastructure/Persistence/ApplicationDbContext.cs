using Microsoft.EntityFrameworkCore;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Filtros globales para Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ServiceType>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<ServiceRecord>().HasQueryFilter(s => !s.IsDeleted);

        // Configuración de Usuarios y Autenticación
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Code).IsUnique();

            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Phone).HasMaxLength(50);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Department).HasMaxLength(100);
            entity.Property(u => u.CommissionRate).HasPrecision(5, 2);
            entity.Property(u => u.Avatar).HasMaxLength(100);
        });

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

        // Configuración de Tipo de Servicio (Catálogo Dinámico)
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

        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // Usuario SuperAdmin Inicial
        var superAdmin = new
        {
            Id = 1,
            Code = "ADM-001",
            FullName = "Tito González",
            Email = "admin@titotransporte.com",
            PasswordHash = "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a", // Password: Admin123!
            Phone = "+1 (809) 555-0100",
            Role = "SuperAdmin",
            Department = "Dirección General & Propietario",
            CommissionRate = 0m,
            IsActive = true,
            IsSuperAdmin = true,
            Avatar = "👑",
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<User>().HasData(superAdmin);

        // Empleados conductores iniciales de la empresa
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
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
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
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
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
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<Employee>().HasData(emp1, emp2, emp3);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
