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

    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();
    public DbSet<DriverSettlement> DriverSettlements => Set<DriverSettlement>();
    public DbSet<Benefit> Benefits => Set<Benefit>();
    public DbSet<DriverBenefit> DriverBenefits => Set<DriverBenefit>();
    public DbSet<SettlementAdjustment> SettlementAdjustments => Set<SettlementAdjustment>();
    public DbSet<DriverPayrollConfig> DriverPayrollConfigs => Set<DriverPayrollConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Filtros globales para Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<ServiceType>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<ServiceRecord>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<DriverSettlement>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<Benefit>().HasQueryFilter(b => !b.IsDeleted);
        modelBuilder.Entity<DriverBenefit>().HasQueryFilter(db => !db.IsDeleted);
        modelBuilder.Entity<SettlementAdjustment>().HasQueryFilter(sa => !sa.IsDeleted);
        modelBuilder.Entity<DriverPayrollConfig>().HasQueryFilter(p => !p.IsDeleted);

        // Configuración de Usuarios (SuperAdmin, Administradores y Choferes)
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
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);
            entity.Property(u => u.Department).HasMaxLength(100);
            entity.Property(u => u.CommissionRate).HasPrecision(5, 2);
            entity.Property(u => u.MustChangePassword).HasDefaultValue(true);

            entity.HasMany(u => u.ServiceRecords)
                  .WithOne(s => s.Driver)
                  .HasForeignKey(s => s.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.Settlements)
                  .WithOne(s => s.Driver)
                  .HasForeignKey(s => s.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Tipo de Servicio (Catálogo Oficial de Transporte Tito)
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

        // Configuración de Registro de Servicio Realizado (Viajes)
        modelBuilder.Entity<ServiceRecord>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ServiceCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(s => s.ServiceCode).IsUnique();

            entity.Property(s => s.ClientReference).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Description).IsRequired().HasMaxLength(500);
            entity.Property(s => s.PriceCharged).HasPrecision(18, 2);

            entity.Property(s => s.Status).HasConversion<int>();
            entity.Property(s => s.Notes).HasMaxLength(1000);

            entity.HasIndex(s => new { s.DriverId, s.ServiceDate });
        });

        // Configuración de Liquidación Mensual del Chofer (Planilla)
        modelBuilder.Entity<DriverSettlement>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.TotalGrossAmount).HasPrecision(18, 2);
            entity.Property(d => d.CompanyRetentionRate).HasPrecision(5, 2);
            entity.Property(d => d.CompanyRetentionAmount).HasPrecision(18, 2);
            entity.Property(d => d.OperationalSubTotal).HasPrecision(18, 2);
            entity.Property(d => d.DriverCommissionRate).HasPrecision(5, 2);
            entity.Property(d => d.CommissionAmount).HasPrecision(18, 2);
            entity.Property(d => d.IsrDeduction).HasPrecision(18, 2);
            entity.Property(d => d.TssDeduction).HasPrecision(18, 2);
            entity.Property(d => d.PhoneIncentive).HasPrecision(18, 2);
            entity.Property(d => d.OtherIncentives).HasPrecision(18, 2);
            entity.Property(d => d.OtherDeductions).HasPrecision(18, 2);
            entity.Property(d => d.TotalNetPayable).HasPrecision(18, 2);

            entity.Property(d => d.Status).IsRequired().HasMaxLength(50);
            entity.Property(d => d.Notes).HasMaxLength(1000);

            entity.HasIndex(d => new { d.DriverId, d.Year, d.Month }).IsUnique();
        });

        // Configuración de Beneficios y Ajustes (Catálogo Dinámico de la Empresa)
        modelBuilder.Entity<Benefit>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(b => b.Code).IsUnique();

            entity.Property(b => b.Name).IsRequired().HasMaxLength(150);
            entity.Property(b => b.Category).HasMaxLength(100);
            entity.Property(b => b.Description).HasMaxLength(500);
            entity.Property(b => b.Type).HasConversion<int>();
            entity.Property(b => b.DefaultAmount).HasPrecision(18, 2);

            entity.HasMany(b => b.DriverBenefits)
                  .WithOne(db => db.Benefit)
                  .HasForeignKey(db => db.BenefitId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Beneficios Asignados al Chofer
        modelBuilder.Entity<DriverBenefit>(entity =>
        {
            entity.HasKey(db => db.Id);
            entity.Property(db => db.CustomAmount).HasPrecision(18, 2);
            entity.Property(db => db.Notes).HasMaxLength(500);

            entity.HasOne(db => db.Driver)
                  .WithMany(u => u.DriverBenefits)
                  .HasForeignKey(db => db.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(db => db.Benefit)
                  .WithMany(b => b.DriverBenefits)
                  .HasForeignKey(db => db.BenefitId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(db => new { db.DriverId, db.BenefitId }).IsUnique();
        });

        // Configuración de Partidas de Ajuste en la Liquidación
        modelBuilder.Entity<SettlementAdjustment>(entity =>
        {
            entity.HasKey(sa => sa.Id);
            entity.Property(sa => sa.Concept).IsRequired().HasMaxLength(150);
            entity.Property(sa => sa.Category).HasMaxLength(100);
            entity.Property(sa => sa.Amount).HasPrecision(18, 2);
            entity.Property(sa => sa.Type).HasConversion<int>();
            entity.Property(sa => sa.Notes).HasMaxLength(500);

            entity.HasOne(sa => sa.DriverSettlement)
                  .WithMany(ds => ds.Adjustments)
                  .HasForeignKey(sa => sa.DriverSettlementId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sa => sa.Benefit)
                  .WithMany()
                  .HasForeignKey(sa => sa.BenefitId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración de Perfil Fiscal y Nómina de Choferes (TSS e ISR - Opción A)
        modelBuilder.Entity<DriverPayrollConfig>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.ReportedSalary).HasPrecision(18, 2);
            entity.Property(p => p.FixedTssAmount).HasPrecision(18, 2);
            entity.Property(p => p.FixedIsrAmount).HasPrecision(18, 2);
            entity.Property(p => p.Notes).HasMaxLength(500);

            entity.HasOne(p => p.Driver)
                  .WithOne(u => u.PayrollConfig)
                  .HasForeignKey<DriverPayrollConfig>(p => p.DriverId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.DriverId).IsUnique();
        });

        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // 1. Usuario SuperAdmin Inicial
        var superAdmin = new
        {
            Id = 1,
            Code = "ADM-001",
            FullName = "Abel González",
            Email = "admin@titotransporte.com",
            PasswordHash = "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a", // Password: Admin123!
            Phone = "+1 (809) 555-0100",
            Role = UserRole.SuperAdmin,
            Department = "Dirección General & Propietario",
            CommissionRate = 0m,
            IsActive = true,
            MustChangePassword = false,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        // 2. Choferes iniciales reales (Browel y Denny, como figuran en la planilla)
        var driverBrowel = new
        {
            Id = 2,
            Code = "CHO-001",
            FullName = "Browel",
            Email = "browel@titotransporte.com",
            PasswordHash = "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a",
            Phone = "+1 (809) 555-0101",
            Role = UserRole.Chofer,
            Department = "Transporte Urbano e Interurbano",
            CommissionRate = 25.0m,
            IsActive = true,
            MustChangePassword = false,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var driverDenny = new
        {
            Id = 3,
            Code = "CHO-002",
            FullName = "Denny",
            Email = "denny@titotransporte.com",
            PasswordHash = "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a",
            Phone = "+1 (809) 555-0102",
            Role = UserRole.Chofer,
            Department = "Rutas Fijas Corporativas",
            CommissionRate = 25.0m,
            IsActive = true,
            MustChangePassword = false,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<User>().HasData(superAdmin, driverBrowel, driverDenny);

        // 3. Catálogo oficial de tipos de servicios reales
        var type1 = new
        {
            Id = 1,
            Name = "Servicio en la Ciudad",
            Category = "Urbano",
            DefaultPrice = 1500m,
            Description = "Servicio de transporte local en la ciudad (ej: traslados, personal)",
            IsActive = true,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var type2 = new
        {
            Id = 2,
            Name = "Servicio Al Interior",
            Category = "Interurbano",
            DefaultPrice = 9000m,
            Description = "Viajes al interior del país (Samaná, Punta Cana, Santiago, Las Terrenas, etc.)",
            IsActive = true,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var type3 = new
        {
            Id = 3,
            Name = "Aereopuerto",
            Category = "Aeropuerto",
            DefaultPrice = 2500m,
            Description = "Traslados hacia y desde aeropuertos nacionales e internacionales",
            IsActive = true,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var type4 = new
        {
            Id = 4,
            Name = "Rutas Fijas",
            Category = "Corporativo",
            DefaultPrice = 42500m,
            Description = "Servicio de transporte de ruta fija mensual matutina y vespertina",
            IsActive = true,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<ServiceType>().HasData(type1, type2, type3, type4);

        // 4. Beneficio predeterminado: Incentivo de Teléfono (Flota)
        var phoneBenefit = new
        {
            Id = 1,
            Code = "BEN-TEL",
            Name = "Incentivo Teléfono",
            Category = "Flota",
            Description = "Incentivo mensual por disponibilidad y uso de teléfono móvil corporativo",
            Type = BenefitType.Incentive,
            DefaultAmount = 1000m,
            IsRecurring = true,
            IsActive = true,
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<Benefit>().HasData(phoneBenefit);

        // 5. Asignación del incentivo a los choferes iniciales Browel y Denny
        var driverBenefitBrowel = new
        {
            Id = 1,
            DriverId = 2,
            BenefitId = 1,
            CustomAmount = 1000m,
            IsActive = true,
            Notes = "Incentivo mensual estándar de flota",
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var driverBenefitDenny = new
        {
            Id = 2,
            DriverId = 3,
            BenefitId = 1,
            CustomAmount = 1000m,
            IsActive = true,
            Notes = "Incentivo mensual estándar de flota",
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<DriverBenefit>().HasData(driverBenefitBrowel, driverBenefitDenny);

        // 6. Configuración de retenciones de ley (TSS e ISR) para choferes Browel y Denny (Opción A)
        var payrollConfigBrowel = new
        {
            Id = 1,
            DriverId = 2,
            ReportedSalary = 18421.15m,
            FixedTssAmount = 1088.69m,
            FixedIsrAmount = 873.00m,
            IsExemptTss = false,
            IsExemptIsr = false,
            Notes = "Salario cotizable base SUIR TSS RD$ 18,421.15 y retención ISR",
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        var payrollConfigDenny = new
        {
            Id = 2,
            DriverId = 3,
            ReportedSalary = 18421.15m,
            FixedTssAmount = 1088.69m,
            FixedIsrAmount = 873.00m,
            IsExemptTss = false,
            IsExemptIsr = false,
            Notes = "Salario cotizable base SUIR TSS RD$ 18,421.15 y retención ISR",
            IsDeleted = false,
            DeletedAt = (DateTime?)null,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null
        };

        modelBuilder.Entity<DriverPayrollConfig>().HasData(payrollConfigBrowel, payrollConfigDenny);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
