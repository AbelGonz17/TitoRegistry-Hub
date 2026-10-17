using Microsoft.EntityFrameworkCore;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Domain.Entities;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Application.Services;

public interface IServiceRegistryService
{
    // --- SERVICIOS REALIZADOS ---
    Task<ServiceRecordDto> RegisterServiceAsync(CreateServiceRecordDto dto, CancellationToken cancellationToken = default);
    Task<ServiceRecordDto?> GetServiceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ServiceRecordDto>> GetServicesAsync(int? driverId = null, DateTime? from = null, DateTime? to = null, int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<ServiceRecordDto> UpdateServiceAsync(int id, UpdateServiceRecordDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteServiceAsync(int id, CancellationToken cancellationToken = default);

    // --- LIQUIDACIÓN Y REPORTES DE CHOFER (PLANILLA) ---
    Task<DriverSettlementReportDto> CalculateDriverSettlementAsync(
        int driverId,
        int month,
        int year,
        decimal? customCompanyRetentionRate = null,
        decimal? customDriverCommissionRate = null,
        decimal? customIsr = null,
        decimal? customTss = null,
        decimal? customPhoneIncentive = null,
        decimal otherIncentives = 0m,
        decimal otherDeductions = 0m,
        CancellationToken cancellationToken = default);

    Task<DriverSettlementReportDto> SaveSettlementAsync(SaveSettlementDto dto, CancellationToken cancellationToken = default);
    Task<List<DriverSettlementReportDto>> GetSettlementHistoryAsync(int? driverId = null, int? year = null, CancellationToken cancellationToken = default);

    // --- CATÁLOGO DE TIPOS DE SERVICIO (CON SOFT DELETE) ---
    Task<List<ServiceTypeDto>> GetActiveServiceTypesAsync(CancellationToken cancellationToken = default);
    Task<ServiceTypeDto?> GetServiceTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceTypeDto> CreateServiceTypeAsync(CreateServiceTypeDto dto, CancellationToken cancellationToken = default);
    Task<ServiceTypeDto> UpdateServiceTypeAsync(int id, UpdateServiceTypeDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteServiceTypeAsync(int id, CancellationToken cancellationToken = default);
}

public class ServiceRegistryService : IServiceRegistryService
{
    private readonly IApplicationDbContext _context;

    public ServiceRegistryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceRecordDto> RegisterServiceAsync(CreateServiceRecordDto dto, CancellationToken cancellationToken = default)
    {
        var driver = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == dto.DriverId && !u.IsDeleted, cancellationToken);

        if (driver == null)
            throw new KeyNotFoundException($"No se encontró ningún usuario/chofer con Id {dto.DriverId}.");

        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceTypeId && !s.IsDeleted, cancellationToken);

        if (serviceType == null)
            throw new KeyNotFoundException($"No se encontró el tipo de servicio con Id {dto.ServiceTypeId}.");

        int totalCount = await _context.ServiceRecords.CountAsync(cancellationToken);
        string serviceCode = $"SRV-{(totalCount + 1):D5}";

        var record = ServiceRecord.Create(
            serviceCode: serviceCode,
            driver: driver,
            serviceType: serviceType,
            clientReference: dto.ClientReference,
            description: dto.Description,
            serviceDate: dto.ServiceDate,
            priceCharged: dto.PriceCharged,
            notes: dto.Notes
        );

        _context.ServiceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        return MapRecordToDto(record, driver, serviceType);
    }

    public async Task<ServiceRecordDto?> GetServiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _context.ServiceRecords
            .AsNoTracking()
            .Include(s => s.Driver)
            .Include(s => s.ServiceType)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        return record != null ? MapRecordToDto(record, record.Driver, record.ServiceType) : null;
    }

    public async Task<List<ServiceRecordDto>> GetServicesAsync(
        int? driverId = null,
        DateTime? from = null,
        DateTime? to = null,
        int? month = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceRecords
            .AsNoTracking()
            .Include(s => s.Driver)
            .Include(s => s.ServiceType)
            .Where(s => !s.IsDeleted);

        if (driverId.HasValue && driverId.Value > 0)
            query = query.Where(s => s.DriverId == driverId.Value);

        if (month.HasValue && month.Value > 0)
            query = query.Where(s => s.ServiceDate.Month == month.Value);

        if (year.HasValue && year.Value > 0)
            query = query.Where(s => s.ServiceDate.Year == year.Value);

        if (from.HasValue)
            query = query.Where(s => s.ServiceDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.ServiceDate <= to.Value);

        var records = await query
            .OrderByDescending(s => s.ServiceDate)
            .ToListAsync(cancellationToken);

        return records.Select(s => MapRecordToDto(s, s.Driver, s.ServiceType)).ToList();
    }

    public async Task<ServiceRecordDto> UpdateServiceAsync(int id, UpdateServiceRecordDto dto, CancellationToken cancellationToken = default)
    {
        var record = await _context.ServiceRecords
            .Include(s => s.Driver)
            .Include(s => s.ServiceType)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (record == null)
            throw new KeyNotFoundException($"No se encontró ningún servicio con Id {id}.");

        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceTypeId && !s.IsDeleted, cancellationToken);

        if (serviceType == null)
            throw new KeyNotFoundException($"No se encontró el tipo de servicio con Id {dto.ServiceTypeId}.");

        record.Update(dto.ServiceTypeId, dto.ClientReference, dto.Description, dto.ServiceDate, dto.PriceCharged, dto.Notes);
        await _context.SaveChangesAsync(cancellationToken);

        return MapRecordToDto(record, record.Driver, serviceType);
    }

    public async Task<bool> SoftDeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _context.ServiceRecords
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (record == null)
            return false;

        record.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- LIQUIDACIÓN Y REPORTES FINANCIEROS (PLANILLA) ---

    public async Task<DriverSettlementReportDto> CalculateDriverSettlementAsync(
        int driverId,
        int month,
        int year,
        decimal? customCompanyRetentionRate = null,
        decimal? customDriverCommissionRate = null,
        decimal? customIsr = null,
        decimal? customTss = null,
        decimal? customPhoneIncentive = null,
        decimal otherIncentives = 0m,
        decimal otherDeductions = 0m,
        CancellationToken cancellationToken = default)
    {
        var driver = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == driverId && !u.IsDeleted, cancellationToken);

        if (driver == null)
            throw new KeyNotFoundException($"No se encontró ningún chofer con Id {driverId}.");

        // Servicios completados en el mes y año solicitados
        var services = await _context.ServiceRecords
            .AsNoTracking()
            .Include(s => s.ServiceType)
            .Where(s => s.DriverId == driverId && !s.IsDeleted && s.Status == ServiceStatus.Completado &&
                        s.ServiceDate.Month == month && s.ServiceDate.Year == year)
            .OrderBy(s => s.ServiceDate)
            .ToListAsync(cancellationToken);

        decimal grossTotal = services.Sum(s => s.PriceCharged);

        // Cargar beneficios asignados al chofer (ej: Incentivo Teléfono)
        var assignedBenefits = await _context.DriverBenefits
            .AsNoTracking()
            .Include(db => db.Benefit)
            .Where(db => db.DriverId == driverId && db.IsActive && !db.IsDeleted)
            .ToListAsync(cancellationToken);

        decimal phoneBenefitAmount = assignedBenefits
            .FirstOrDefault(b => b.Benefit != null && (b.Benefit.Code == "BEN-TEL" || b.Benefit.Name.Contains("Teléfono", StringComparison.OrdinalIgnoreCase)))?.CustomAmount ?? 1000.0m;

        // Cargar configuración de nómina y retenciones de ley del chofer (TSS e ISR - Opción A)
        var payrollConfig = await _context.DriverPayrollConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.DriverId == driverId && !p.IsDeleted, cancellationToken);

        decimal defaultIsr = payrollConfig != null ? payrollConfig.GetEffectiveIsr() : 873.0m;
        decimal defaultTss = payrollConfig != null ? payrollConfig.GetEffectiveTss() : 1088.69m;

        decimal companyRetentionRate = customCompanyRetentionRate ?? 30.0m;
        decimal driverCommissionRate = customDriverCommissionRate ?? driver.CommissionRate;
        decimal isr = customIsr ?? defaultIsr;
        decimal tss = customTss ?? defaultTss;
        decimal phoneIncentive = customPhoneIncentive ?? phoneBenefitAmount;

        // FÓRMULA OFICIAL DE TRANSPORTE TITO:
        // 1. Retención 30%
        decimal retentionAmount = Math.Round(grossTotal * (companyRetentionRate / 100m), 2, MidpointRounding.AwayFromZero);
        // 2. Subtotal Operativo = Bruto - 30%
        decimal subTotal = Math.Round(grossTotal - retentionAmount, 2, MidpointRounding.AwayFromZero);
        // 3. Comisión Chofer (ej: 25% sobre Subtotal)
        decimal commissionAmount = Math.Round(subTotal * (driverCommissionRate / 100m), 2, MidpointRounding.AwayFromZero);
        // 4. Total a Pagar = Comisión - ISR - TSS + Incentivo Teléfono + Otros Incentivos - Otras Deducciones
        decimal netPayable = Math.Round(commissionAmount - isr - tss + phoneIncentive + otherIncentives - otherDeductions, 2, MidpointRounding.AwayFromZero);

        // Verificar si ya existe una liquidación guardada
        var existingSettlement = await _context.DriverSettlements
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DriverId == driverId && d.Month == month && d.Year == year && !d.IsDeleted, cancellationToken);

        string monthName = new DateTime(year, month, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
        monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);

        return new DriverSettlementReportDto
        {
            DriverId = driver.Id,
            DriverName = driver.FullName,
            DriverCode = driver.Code,
            Month = month,
            MonthName = monthName,
            Year = year,
            Services = services.Select(s => MapRecordToDto(s, driver, s.ServiceType)).ToList(),
            TotalGrossAmount = grossTotal,
            CompanyRetentionRate = companyRetentionRate,
            CompanyRetentionAmount = retentionAmount,
            OperationalSubTotal = subTotal,
            DriverCommissionRate = driverCommissionRate,
            CommissionAmount = commissionAmount,
            IsrDeduction = isr,
            TssDeduction = tss,
            PhoneIncentive = phoneIncentive,
            OtherIncentives = otherIncentives,
            OtherDeductions = otherDeductions,
            TotalNetPayable = netPayable,
            Status = existingSettlement?.Status ?? "Borrador",
            PaidAt = existingSettlement?.PaidAt,
            Notes = existingSettlement?.Notes ?? string.Empty
        };
    }

    public async Task<DriverSettlementReportDto> SaveSettlementAsync(SaveSettlementDto dto, CancellationToken cancellationToken = default)
    {
        var driver = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == dto.DriverId && !u.IsDeleted, cancellationToken);

        if (driver == null)
            throw new KeyNotFoundException($"No se encontró el chofer con Id {dto.DriverId}.");

        var report = await CalculateDriverSettlementAsync(
            driverId: dto.DriverId,
            month: dto.Month,
            year: dto.Year,
            customCompanyRetentionRate: dto.CompanyRetentionRate,
            customDriverCommissionRate: dto.DriverCommissionRate,
            customIsr: dto.IsrDeduction,
            customTss: dto.TssDeduction,
            customPhoneIncentive: dto.PhoneIncentive,
            otherIncentives: dto.OtherIncentives,
            otherDeductions: dto.OtherDeductions,
            cancellationToken: cancellationToken
        );

        var settlement = await _context.DriverSettlements
            .FirstOrDefaultAsync(d => d.DriverId == dto.DriverId && d.Month == dto.Month && d.Year == dto.Year && !d.IsDeleted, cancellationToken);

        if (settlement == null)
        {
            settlement = new DriverSettlement(
                driverId: dto.DriverId,
                month: dto.Month,
                year: dto.Year,
                totalGrossAmount: report.TotalGrossAmount,
                companyRetentionRate: report.CompanyRetentionRate,
                driverCommissionRate: report.DriverCommissionRate,
                isrDeduction: report.IsrDeduction,
                tssDeduction: report.TssDeduction,
                phoneIncentive: report.PhoneIncentive,
                otherIncentives: report.OtherIncentives,
                otherDeductions: report.OtherDeductions,
                status: dto.Status,
                notes: dto.Notes
            );
            _context.DriverSettlements.Add(settlement);
        }
        else
        {
            settlement.Recalculate(report.TotalGrossAmount);
            if (dto.Status == "Aprobado") settlement.Approve();
            if (dto.Status == "Pagado") settlement.MarkAsPaid();
        }

        await _context.SaveChangesAsync(cancellationToken);

        report.Status = settlement.Status;
        report.PaidAt = settlement.PaidAt;
        return report;
    }

    public async Task<List<DriverSettlementReportDto>> GetSettlementHistoryAsync(int? driverId = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DriverSettlements
            .AsNoTracking()
            .Include(d => d.Driver)
            .Where(d => !d.IsDeleted);

        if (driverId.HasValue && driverId.Value > 0)
            query = query.Where(d => d.DriverId == driverId.Value);

        if (year.HasValue && year.Value > 0)
            query = query.Where(d => d.Year == year.Value);

        var settlements = await query
            .OrderByDescending(d => d.Year)
            .ThenByDescending(d => d.Month)
            .ToListAsync(cancellationToken);

        return settlements.Select(d =>
        {
            string monthName = new DateTime(d.Year, d.Month, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
            monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);

            return new DriverSettlementReportDto
            {
                DriverId = d.DriverId,
                DriverName = d.Driver?.FullName ?? string.Empty,
                DriverCode = d.Driver?.Code ?? string.Empty,
                Month = d.Month,
                MonthName = monthName,
                Year = d.Year,
                TotalGrossAmount = d.TotalGrossAmount,
                CompanyRetentionRate = d.CompanyRetentionRate,
                CompanyRetentionAmount = d.CompanyRetentionAmount,
                OperationalSubTotal = d.OperationalSubTotal,
                DriverCommissionRate = d.DriverCommissionRate,
                CommissionAmount = d.CommissionAmount,
                IsrDeduction = d.IsrDeduction,
                TssDeduction = d.TssDeduction,
                PhoneIncentive = d.PhoneIncentive,
                OtherIncentives = d.OtherIncentives,
                OtherDeductions = d.OtherDeductions,
                TotalNetPayable = d.TotalNetPayable,
                Status = d.Status,
                PaidAt = d.PaidAt,
                Notes = d.Notes
            };
        }).ToList();
    }

    // --- CRUD DEL CATÁLOGO DE SERVICIOS (CON SOFT DELETE) ---

    public async Task<List<ServiceTypeDto>> GetActiveServiceTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceTypes
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new ServiceTypeDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                DefaultPrice = s.DefaultPrice,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceTypeDto?> GetServiceTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (serviceType == null) return null;

        return new ServiceTypeDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Category = serviceType.Category,
            DefaultPrice = serviceType.DefaultPrice,
            Description = serviceType.Description,
            IsActive = serviceType.IsActive,
            CreatedAt = serviceType.CreatedAt
        };
    }

    public async Task<ServiceTypeDto> CreateServiceTypeAsync(CreateServiceTypeDto dto, CancellationToken cancellationToken = default)
    {
        var serviceType = new ServiceType(
            dto.Name,
            dto.Category,
            dto.DefaultPrice,
            dto.Description
        );

        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceTypeDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Category = serviceType.Category,
            DefaultPrice = serviceType.DefaultPrice,
            Description = serviceType.Description,
            IsActive = serviceType.IsActive,
            CreatedAt = serviceType.CreatedAt
        };
    }

    public async Task<ServiceTypeDto> UpdateServiceTypeAsync(int id, UpdateServiceTypeDto dto, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (serviceType == null)
            throw new KeyNotFoundException($"No se encontró ningún tipo de servicio con Id {id}.");

        serviceType.Update(dto.Name, dto.Category, dto.DefaultPrice, dto.Description);

        if (dto.IsActive)
            serviceType.Activate();
        else
            serviceType.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceTypeDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Category = serviceType.Category,
            DefaultPrice = serviceType.DefaultPrice,
            Description = serviceType.Description,
            IsActive = serviceType.IsActive,
            CreatedAt = serviceType.CreatedAt
        };
    }

    public async Task<bool> SoftDeleteServiceTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (serviceType == null)
            return false;

        serviceType.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ServiceRecordDto MapRecordToDto(ServiceRecord record, User? driver, ServiceType? serviceType) => new()
    {
        Id = record.Id,
        ServiceCode = record.ServiceCode,
        DriverId = record.DriverId,
        DriverName = driver?.FullName ?? string.Empty,
        DriverCode = driver?.Code ?? string.Empty,
        ServiceTypeId = record.ServiceTypeId,
        ServiceTypeName = serviceType?.Name ?? string.Empty,
        ServiceCategory = serviceType?.Category ?? string.Empty,
        ClientReference = record.ClientReference,
        Description = record.Description,
        ServiceDate = record.ServiceDate,
        PriceCharged = record.PriceCharged,
        Status = record.Status,
        Notes = record.Notes,
        CreatedAt = record.CreatedAt
    };
}
