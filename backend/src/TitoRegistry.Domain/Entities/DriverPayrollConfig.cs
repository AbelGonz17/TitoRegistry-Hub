using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Configuración de nómina y perfil fiscal/tributario específico para un chofer (Opción A).
/// Permite almacenar los valores de retención legal oficiales de República Dominicana:
/// TSS (Tesorería de la Seguridad Social) e ISR (Impuesto Sobre la Renta),
/// desacoplando la entidad User de datos impositivos.
/// </summary>
public class DriverPayrollConfig : BaseEntity
{
    public int DriverId { get; private set; }
    public virtual User? Driver { get; private set; }

    // Salario cotizable base reportado en el SUIR / TSS (ej: RD$ 18,421.15)
    public decimal ReportedSalary { get; private set; }

    // Monto mensual de retención fija de TSS (ej: RD$ 1,088.69)
    public decimal FixedTssAmount { get; private set; } = 1088.69m;

    // Monto mensual de retención fija de ISR (ej: RD$ 873.00)
    public decimal FixedIsrAmount { get; private set; } = 873.00m;

    // Indica si el chofer está exento de retención TSS (ej: contratista independiente)
    public bool IsExemptTss { get; private set; } = false;

    // Indica si el chofer está exento de retención ISR
    public bool IsExemptIsr { get; private set; } = false;

    public string Notes { get; private set; } = string.Empty;

    // Constructor privado para EF Core
    private DriverPayrollConfig() { }

    public DriverPayrollConfig(
        int driverId,
        decimal fixedTssAmount = 1088.69m,
        decimal fixedIsrAmount = 873.00m,
        decimal reportedSalary = 18421.15m,
        bool isExemptTss = false,
        bool isExemptIsr = false,
        string notes = "")
    {
        if (driverId <= 0)
            throw new ArgumentException("El chofer asignado no es válido.", nameof(driverId));

        if (fixedTssAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedTssAmount), "El monto de TSS no puede ser negativo.");

        if (fixedIsrAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedIsrAmount), "El monto de ISR no puede ser negativo.");

        if (reportedSalary < 0)
            throw new ArgumentOutOfRangeException(nameof(reportedSalary), "El salario cotizable no puede ser negativo.");

        DriverId = driverId;
        FixedTssAmount = fixedTssAmount;
        FixedIsrAmount = fixedIsrAmount;
        ReportedSalary = reportedSalary;
        IsExemptTss = isExemptTss;
        IsExemptIsr = isExemptIsr;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Retorna el monto efectivo de TSS a retener (0 si está exento).
    /// </summary>
    public decimal GetEffectiveTss() => IsExemptTss ? 0m : FixedTssAmount;

    /// <summary>
    /// Retorna el monto efectivo de ISR a retener (0 si está exento).
    /// </summary>
    public decimal GetEffectiveIsr() => IsExemptIsr ? 0m : FixedIsrAmount;

    /// <summary>
    /// Actualiza la configuración impositiva del chofer.
    /// </summary>
    public void Update(
        decimal fixedTssAmount,
        decimal fixedIsrAmount,
        decimal reportedSalary = 0m,
        bool isExemptTss = false,
        bool isExemptIsr = false,
        string notes = "")
    {
        if (fixedTssAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedTssAmount), "El monto de TSS no puede ser negativo.");

        if (fixedIsrAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedIsrAmount), "El monto de ISR no puede ser negativo.");

        if (reportedSalary < 0)
            throw new ArgumentOutOfRangeException(nameof(reportedSalary), "El salario cotizable no puede ser negativo.");

        FixedTssAmount = fixedTssAmount;
        FixedIsrAmount = fixedIsrAmount;
        ReportedSalary = reportedSalary;
        IsExemptTss = isExemptTss;
        IsExemptIsr = isExemptIsr;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
