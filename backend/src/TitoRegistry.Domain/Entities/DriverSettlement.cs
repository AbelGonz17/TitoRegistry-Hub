using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa la liquidación o planilla mensual de un chofer.
/// Contiene el desglose exacto de servicios brutos, retención operativa (30%),
/// comisión del chofer (ej: 25%), deducciones fiscales (ISR, TSS) e incentivos de flota.
/// </summary>
public class DriverSettlement : BaseEntity
{
    public int DriverId { get; private set; }
    public virtual User? Driver { get; private set; }

    public int Month { get; private set; }
    public int Year { get; private set; }

    // Monto bruto total de los servicios registrados en el periodo
    public decimal TotalGrossAmount { get; private set; }

    // Retención de la empresa / gastos operativos (ej: 30%)
    public decimal CompanyRetentionRate { get; private set; } = 30.0m;
    public decimal CompanyRetentionAmount { get; private set; }

    // Subtotal operativo base para comisión = Bruto - Retención Empresa
    public decimal OperationalSubTotal { get; private set; }

    // Porcentaje de comisión del chofer (ej: 25%)
    public decimal DriverCommissionRate { get; private set; } = 25.0m;
    public decimal CommissionAmount { get; private set; }

    // Deducciones de ley
    public decimal IsrDeduction { get; private set; } // Impuesto Sobre la Renta (RD)
    public decimal TssDeduction { get; private set; } // Tesorería de la Seguridad Social (RD)

    // Incentivos y bonificaciones
    public decimal PhoneIncentive { get; private set; } = 1000.0m;
    public decimal OtherIncentives { get; private set; }
    public decimal OtherDeductions { get; private set; }

    // Total Neto final a pagar al Chofer
    public decimal TotalNetPayable { get; private set; }

    public string Status { get; private set; } = "Borrador"; // Borrador, Aprobado, Pagado
    public string Notes { get; private set; } = string.Empty;
    public DateTime? PaidAt { get; private set; }

    // Constructor privado para EF Core
    private DriverSettlement() { }

    public DriverSettlement(
        int driverId,
        int month,
        int year,
        decimal totalGrossAmount,
        decimal companyRetentionRate,
        decimal driverCommissionRate,
        decimal isrDeduction,
        decimal tssDeduction,
        decimal phoneIncentive,
        decimal otherIncentives = 0m,
        decimal otherDeductions = 0m,
        string status = "Borrador",
        string notes = "")
    {
        if (driverId <= 0)
            throw new ArgumentException("El chofer asignado no es válido.", nameof(driverId));

        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "El mes debe estar entre 1 y 12.");

        if (year < 2020 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "El año no es válido.");

        DriverId = driverId;
        Month = month;
        Year = year;
        CompanyRetentionRate = companyRetentionRate < 0 ? 0 : companyRetentionRate;
        DriverCommissionRate = driverCommissionRate < 0 ? 0 : driverCommissionRate;
        IsrDeduction = isrDeduction < 0 ? 0 : isrDeduction;
        TssDeduction = tssDeduction < 0 ? 0 : tssDeduction;
        PhoneIncentive = phoneIncentive < 0 ? 0 : phoneIncentive;
        OtherIncentives = otherIncentives < 0 ? 0 : otherIncentives;
        OtherDeductions = otherDeductions < 0 ? 0 : otherDeductions;
        Status = string.IsNullOrWhiteSpace(status) ? "Borrador" : status.Trim();
        Notes = notes?.Trim() ?? string.Empty;

        Recalculate(totalGrossAmount);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ejecuta la fórmula matemática oficial de planilla:
    /// 1. Retención Empresa = Bruto * (30 / 100)
    /// 2. Subtotal Operativo = Bruto - Retención Empresa
    /// 3. Comisión Chofer = Subtotal Operativo * (25 / 100)
    /// 4. Total a Pagar = Comisión - ISR - TSS + Incentivo Teléfono + Otros Incentivos - Otras Deducciones
    /// </summary>
    public void Recalculate(decimal grossAmount)
    {
        TotalGrossAmount = grossAmount < 0 ? 0 : grossAmount;

        CompanyRetentionAmount = Math.Round(TotalGrossAmount * (CompanyRetentionRate / 100m), 2, MidpointRounding.AwayFromZero);
        OperationalSubTotal = Math.Round(TotalGrossAmount - CompanyRetentionAmount, 2, MidpointRounding.AwayFromZero);

        CommissionAmount = Math.Round(OperationalSubTotal * (DriverCommissionRate / 100m), 2, MidpointRounding.AwayFromZero);

        TotalNetPayable = Math.Round(
            CommissionAmount - IsrDeduction - TssDeduction + PhoneIncentive + OtherIncentives - OtherDeductions,
            2,
            MidpointRounding.AwayFromZero
        );

        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        Status = "Aprobado";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        Status = "Pagado";
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
