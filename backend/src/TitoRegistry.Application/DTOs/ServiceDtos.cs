using System.ComponentModel.DataAnnotations;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Application.DTOs;

/// <summary>
/// Datos requeridos para registrar un servicio realizado por un chofer.
/// </summary>
public class CreateServiceRecordDto
{
    [Required(ErrorMessage = "El identificador del chofer es obligatorio.")]
    public int DriverId { get; set; }

    [Required(ErrorMessage = "El tipo de servicio es obligatorio.")]
    public int ServiceTypeId { get; set; }

    [Required(ErrorMessage = "La referencia o cliente es obligatoria.")]
    [MaxLength(200, ErrorMessage = "La referencia no puede exceder 200 caracteres.")]
    public string ClientReference { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción del servicio es obligatoria.")]
    [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

    [Range(0.01, 10000000, ErrorMessage = "El precio cobrado debe ser mayor a cero.")]
    public decimal PriceCharged { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Datos para actualizar un servicio de transporte existente.
/// </summary>
public class UpdateServiceRecordDto
{
    [Required(ErrorMessage = "El tipo de servicio es obligatorio.")]
    public int ServiceTypeId { get; set; }

    [Required(ErrorMessage = "La referencia o cliente es obligatoria.")]
    [MaxLength(200, ErrorMessage = "La referencia no puede exceder 200 caracteres.")]
    public string ClientReference { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción del servicio es obligatoria.")]
    [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    public DateTime ServiceDate { get; set; }

    [Range(0.01, 10000000, ErrorMessage = "El precio cobrado debe ser mayor a cero.")]
    public decimal PriceCharged { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Representación de un registro de servicio para consulta y listados.
/// </summary>
public class ServiceRecordDto
{
    public int Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;

    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DriverCode { get; set; } = string.Empty;

    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;

    public string ClientReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal PriceCharged { get; set; }

    public ServiceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Reporte de Servicios y Liquidación Mensual del Chofer (Planilla idéntica a la oficial).
/// </summary>
public class DriverSettlementReportDto
{
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DriverCode { get; set; } = string.Empty;

    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }

    // Listado de servicios del periodo
    public List<ServiceRecordDto> Services { get; set; } = new();

    // 1. Total Bruto de los servicios
    public decimal TotalGrossAmount { get; set; }

    // 2. Retención Empresa (30%)
    public decimal CompanyRetentionRate { get; set; } = 30.0m;
    public decimal CompanyRetentionAmount { get; set; }

    // 3. Sub Total Operativo = Bruto - Retención Empresa
    public decimal OperationalSubTotal { get; set; }

    // 4. Comisión del Chofer (ej: 25%)
    public decimal DriverCommissionRate { get; set; } = 25.0m;
    public decimal CommissionAmount { get; set; }

    // 5. Deducciones de Ley
    public decimal IsrDeduction { get; set; } = 873.0m;
    public decimal TssDeduction { get; set; } = 1088.69m;

    // 6. Incentivos
    public decimal PhoneIncentive { get; set; } = 1000.0m;
    public decimal OtherIncentives { get; set; }
    public decimal OtherDeductions { get; set; }

    // 7. Total Neto a Pagar
    public decimal TotalNetPayable { get; set; }

    public string Status { get; set; } = "Borrador"; // Borrador, Aprobado, Pagado
    public DateTime? PaidAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// DTO para guardar / aprobar una liquidación mensual.
/// </summary>
public class SaveSettlementDto
{
    [Required]
    public int DriverId { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(2020, 2100)]
    public int Year { get; set; }

    public decimal? CompanyRetentionRate { get; set; }
    public decimal? DriverCommissionRate { get; set; }
    public decimal? IsrDeduction { get; set; }
    public decimal? TssDeduction { get; set; }
    public decimal? PhoneIncentive { get; set; }
    public decimal OtherIncentives { get; set; } = 0m;
    public decimal OtherDeductions { get; set; } = 0m;
    public string Status { get; set; } = "Borrador";
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// DTO para crear un tipo de servicio en el catálogo.
/// </summary>
public class CreateServiceTypeDto
{
    [Required(ErrorMessage = "El nombre del tipo de servicio es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    [Range(0, 10000000, ErrorMessage = "El precio sugerido no puede ser negativo.")]
    public decimal DefaultPrice { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO para actualizar un tipo de servicio existente.
/// </summary>
public class UpdateServiceTypeDto
{
    [Required(ErrorMessage = "El nombre del tipo de servicio es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = "General";

    [Range(0, 10000000, ErrorMessage = "El precio sugerido no puede ser negativo.")]
    public decimal DefaultPrice { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

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
