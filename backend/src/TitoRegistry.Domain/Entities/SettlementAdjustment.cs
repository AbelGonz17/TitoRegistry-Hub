using TitoRegistry.Domain.Common;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Representa una partida individual de ajuste (beneficio, bono, incentivo o deducción)
/// dentro de la liquidación mensual de un chofer.
/// Almacena una copia histórica del concepto y monto para garantizar la inmutabilidad contable del reporte.
/// </summary>
public class SettlementAdjustment : BaseEntity
{
    public int DriverSettlementId { get; private set; }
    public virtual DriverSettlement? DriverSettlement { get; private set; }

    public int? BenefitId { get; private set; }
    public virtual Benefit? Benefit { get; private set; }

    // Concepto visible en el reporte (ej: "Incentivo Teléfono", "Bono de Asistencia", "Deducción Préstamo")
    public string Concept { get; private set; } = string.Empty;

    public string Category { get; private set; } = "General";

    // Tipo de ajuste: Incentive (suma) o Deduction (resta)
    public BenefitType Type { get; private set; } = BenefitType.Incentive;

    // Monto del ajuste en pesos (RD$)
    public decimal Amount { get; private set; }

    public string Notes { get; private set; } = string.Empty;

    // Constructor privado para EF Core
    private SettlementAdjustment() { }

    public SettlementAdjustment(
        int driverSettlementId,
        string concept,
        decimal amount,
        BenefitType type,
        int? benefitId = null,
        string category = "General",
        string notes = "")
    {
        if (driverSettlementId <= 0)
            throw new ArgumentException("La liquidación asociada no es válida.", nameof(driverSettlementId));

        if (string.IsNullOrWhiteSpace(concept))
            throw new ArgumentException("El concepto del ajuste es obligatorio.", nameof(concept));

        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto del ajuste no puede ser negativo.");

        DriverSettlementId = driverSettlementId;
        Concept = concept.Trim();
        Amount = amount;
        Type = type;
        BenefitId = benefitId;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string concept, decimal amount, BenefitType type, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(concept))
            throw new ArgumentException("El concepto no puede estar vacío.", nameof(concept));

        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto no puede ser negativo.");

        Concept = concept.Trim();
        Amount = amount;
        Type = type;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
