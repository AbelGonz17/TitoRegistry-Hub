using System.ComponentModel.DataAnnotations;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Application.DTOs;

/// <summary>
/// Datos requeridos para registrar un servicio realizado por un empleado.
/// </summary>
public class CreateServiceRecordDto
{
    [Required(ErrorMessage = "El identificador del empleado es obligatorio.")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "El tipo de servicio es obligatorio.")]
    public int ServiceTypeId { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre del cliente no puede exceder 150 caracteres.")]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string ClientAddress { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ClientPhone { get; set; } = string.Empty;

    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

    [Range(0.01, 1000000, ErrorMessage = "El monto cobrado debe ser mayor a cero.")]
    public decimal PriceCharged { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Representación de un registro de servicio para consulta.
/// </summary>
public class ServiceRecordDto
{
    public int Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientAddress { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal PriceCharged { get; set; }
    public decimal EmployeeEarnings { get; set; }
    public ServiceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Resumen financiero y acumulado de un empleado específico.
/// </summary>
public class EmployeeSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }

    public int TotalServicesCompleted { get; set; }
    public decimal TotalEarningsAccumulated { get; set; }
    public decimal TotalBilled { get; set; }

    public decimal AverageEarningsPerService => TotalServicesCompleted > 0 
        ? Math.Round(TotalEarningsAccumulated / TotalServicesCompleted, 2) 
        : 0m;

    public List<ServiceRecordDto> RecentServices { get; set; } = new();
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO para crear un nuevo tipo de servicio en el catálogo.
/// </summary>
public class CreateServiceTypeDto
{
    [Required(ErrorMessage = "El nombre del tipo de servicio es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    [Range(0, 1000000, ErrorMessage = "El precio sugerido no puede ser negativo.")]
    public decimal DefaultPrice { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO para actualizar un tipo de servicio existente en el catálogo.
/// </summary>
public class UpdateServiceTypeDto
{
    [Required(ErrorMessage = "El nombre del tipo de servicio es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    [Range(0, 1000000, ErrorMessage = "El precio sugerido no puede ser negativo.")]
    public decimal DefaultPrice { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Representación de un tipo de servicio para consulta.
/// </summary>
public class ServiceTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
