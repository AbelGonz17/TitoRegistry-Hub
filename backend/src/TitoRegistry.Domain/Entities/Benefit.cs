using TitoRegistry.Domain.Common;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un beneficio, incentivo o concepto de ajuste de la empresa.
/// Permite parametrizar dinámicamente beneficios recurrentes (ej: Incentivo de Teléfono, Combustible, Viáticos)
/// o conceptos de deducción sin alterar el esquema de base de datos.
/// </summary>
public class Benefit : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = "General";
    public string Description { get; private set; } = string.Empty;
    public BenefitType Type { get; private set; } = BenefitType.Incentive;
    public decimal DefaultAmount { get; private set; }
    public bool IsRecurring { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    // Relación de choferes que tienen este beneficio asignado
    private readonly List<DriverBenefit> _driverBenefits = new();
    public virtual IReadOnlyCollection<DriverBenefit> DriverBenefits => _driverBenefits.AsReadOnly();

    // Constructor privado para EF Core
    private Benefit() { }

    public Benefit(
        string code,
        string name,
        string category,
        string description,
        BenefitType type,
        decimal defaultAmount,
        bool isRecurring = true,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del beneficio o incentivo es obligatorio.", nameof(name));

        if (defaultAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultAmount), "El monto predeterminado no puede ser negativo.");

        Code = string.IsNullOrWhiteSpace(code) ? "BEN-001" : code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        DefaultAmount = defaultAmount;
        IsRecurring = isRecurring;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la configuración del beneficio.
    /// </summary>
    public void Update(
        string name,
        string category,
        string description,
        BenefitType type,
        decimal defaultAmount,
        bool isRecurring,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del beneficio es obligatorio.", nameof(name));

        if (defaultAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultAmount), "El monto no puede ser negativo.");

        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        DefaultAmount = defaultAmount;
        IsRecurring = isRecurring;
        IsActive = isActive;
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
