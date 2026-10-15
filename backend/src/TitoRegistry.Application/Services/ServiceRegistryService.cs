using Microsoft.EntityFrameworkCore;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Domain.Entities;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Application.Services;

public interface IServiceRegistryService
{
    // Registro y consulta de servicios realizados
    Task<ServiceRecordDto> RegisterServiceAsync(CreateServiceRecordDto dto, CancellationToken cancellationToken = default);
    Task<List<ServiceRecordDto>> GetServicesByEmployeeAsync(int employeeId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<EmployeeSummaryDto?> GetEmployeeSummaryAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<List<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);

    // CRUD del catálogo de tipos de servicio (con Soft Delete)
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
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            throw new KeyNotFoundException($"No se encontró ningún empleado con Id {dto.EmployeeId}.");

        if (!employee.IsActive)
            throw new InvalidOperationException($"El empleado {employee.FullName} se encuentra inactivo y no puede registrar servicios.");

        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceTypeId && !s.IsDeleted, cancellationToken);

        if (serviceType == null)
            throw new KeyNotFoundException($"No se encontró ningún tipo de servicio con Id {dto.ServiceTypeId}.");

        if (!serviceType.IsActive)
            throw new InvalidOperationException($"El tipo de servicio '{serviceType.Name}' está inactivo en el catálogo.");

        // Generar código correlativo de servicio: SRV-YYYY-XXX
        int nextId = await _context.ServiceRecords.CountAsync(cancellationToken) + 1;
        string serviceCode = $"SRV-{DateTime.UtcNow.Year}-{nextId:D3}";

        // Crear registro a través de la lógica de dominio (calcula y congela la ganancia)
        var record = ServiceRecord.Create(
            serviceCode,
            employee,
            serviceType,
            dto.ClientName,
            dto.ClientAddress,
            dto.ClientPhone,
            dto.ServiceDate,
            dto.PriceCharged,
            dto.Notes
        );

        _context.ServiceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceRecordDto
        {
            Id = record.Id,
            ServiceCode = record.ServiceCode,
            EmployeeId = record.EmployeeId,
            EmployeeName = employee.FullName,
            ServiceTypeId = record.ServiceTypeId,
            ServiceTypeName = serviceType.Name,
            ServiceCategory = serviceType.Category,
            ClientName = record.ClientName,
            ClientAddress = record.ClientAddress,
            ClientPhone = record.ClientPhone,
            ServiceDate = record.ServiceDate,
            PriceCharged = record.PriceCharged,
            EmployeeEarnings = record.EmployeeEarnings,
            Status = record.Status,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<List<ServiceRecordDto>> GetServicesByEmployeeAsync(int employeeId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceRecords
            .AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.ServiceType)
            .Where(s => s.EmployeeId == employeeId && !s.IsDeleted);

        if (from.HasValue)
            query = query.Where(s => s.ServiceDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.ServiceDate <= to.Value);

        var records = await query
            .OrderByDescending(s => s.ServiceDate)
            .ToListAsync(cancellationToken);

        return records.Select(s => new ServiceRecordDto
        {
            Id = s.Id,
            ServiceCode = s.ServiceCode,
            EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee?.FullName ?? string.Empty,
            ServiceTypeId = s.ServiceTypeId,
            ServiceTypeName = s.ServiceType?.Name ?? string.Empty,
            ServiceCategory = s.ServiceType?.Category ?? string.Empty,
            ClientName = s.ClientName,
            ClientAddress = s.ClientAddress,
            ClientPhone = s.ClientPhone,
            ServiceDate = s.ServiceDate,
            PriceCharged = s.PriceCharged,
            EmployeeEarnings = s.EmployeeEarnings,
            Status = s.Status,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<EmployeeSummaryDto?> GetEmployeeSummaryAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);

        if (employee == null) return null;

        var completedServices = await _context.ServiceRecords
            .AsNoTracking()
            .Include(s => s.ServiceType)
            .Where(s => s.EmployeeId == employeeId && !s.IsDeleted && s.Status == ServiceStatus.Completado)
            .OrderByDescending(s => s.ServiceDate)
            .ToListAsync(cancellationToken);

        decimal totalEarnings = completedServices.Sum(s => s.EmployeeEarnings);
        decimal totalBilled = completedServices.Sum(s => s.PriceCharged);

        var recentDtos = completedServices.Take(10).Select(s => new ServiceRecordDto
        {
            Id = s.Id,
            ServiceCode = s.ServiceCode,
            EmployeeId = s.EmployeeId,
            EmployeeName = employee.FullName,
            ServiceTypeId = s.ServiceTypeId,
            ServiceTypeName = s.ServiceType?.Name ?? string.Empty,
            ServiceCategory = s.ServiceType?.Category ?? string.Empty,
            ClientName = s.ClientName,
            ClientAddress = s.ClientAddress,
            ClientPhone = s.ClientPhone,
            ServiceDate = s.ServiceDate,
            PriceCharged = s.PriceCharged,
            EmployeeEarnings = s.EmployeeEarnings,
            Status = s.Status,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt
        }).ToList();

        return new EmployeeSummaryDto
        {
            Id = employee.Id,
            Code = employee.Code,
            FullName = employee.FullName,
            Email = employee.Email,
            Phone = employee.Phone,
            Role = employee.Role,
            Department = employee.Department,
            CommissionRate = employee.CommissionRate,
            IsActive = employee.IsActive,
            TotalServicesCompleted = completedServices.Count,
            TotalEarningsAccumulated = totalEarnings,
            TotalBilled = totalBilled,
            RecentServices = recentDtos
        };
    }

    public async Task<List<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.IsActive)
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                Code = e.Code,
                FullName = e.FullName,
                Email = e.Email,
                Phone = e.Phone,
                Role = e.Role,
                Department = e.Department,
                CommissionRate = e.CommissionRate,
                IsActive = e.IsActive
            })
            .ToListAsync(cancellationToken);
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

        // Borrado lógico (Soft delete)
        serviceType.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
