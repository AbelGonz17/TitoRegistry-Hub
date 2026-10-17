using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Asignación de un beneficio recurrente a un chofer específico.
/// Permite personalizar el monto asignado (ej: Chofer Browel recibe RD$ 1,000 por teléfono,
/// mientras que otro chofer podría tener un monto personalizado u otros beneficios).
/// </summary>
public class DriverBenefit : BaseEntity
{
    public int DriverId { get; private set; }
    public virtual User? Driver { get; private set; }

    public int BenefitId { get; private set; }
    public virtual Benefit? Benefit { get; private set; }

    // Monto efectivo a pagar en cada liquidación mensual para este chofer
    public decimal CustomAmount { get; private set; }

    public bool IsActive { get; private set; } = true;
    public string Notes { get; private set; } = string.Empty;

    // Constructor privado para EF Core
    private DriverBenefit() { }

    public DriverBenefit(
        int driverId,
        int benefitId,
        decimal customAmount,
        bool isActive = true,
        string notes = "")
    {
        if (driverId <= 0)
            throw new ArgumentException("El chofer asignado no es válido.", nameof(driverId));

        if (benefitId <= 0)
            throw new ArgumentException("El beneficio seleccionado no es válido.", nameof(benefitId));

        if (customAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(customAmount), "El monto del beneficio no puede ser negativo.");

        DriverId = driverId;
        BenefitId = benefitId;
        CustomAmount = customAmount;
        IsActive = isActive;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAmount(decimal newAmount, string notes = "")
    {
        if (newAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(newAmount), "El monto no puede ser negativo.");

        CustomAmount = newAmount;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public override void SoftDelete()
    {
        base.SoftDelete();
        IsActive = false;
    }
}
