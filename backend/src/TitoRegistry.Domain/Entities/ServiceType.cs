using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de catálogo dinámico que define un tipo de servicio de transporte.
/// Permite creación, actualización y borrado lógico (Soft Delete).
/// </summary>
public class ServiceType : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal DefaultPrice { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Relación con los servicios registrados
    private readonly List<ServiceRecord> _serviceRecords = new();
    public IReadOnlyCollection<ServiceRecord> ServiceRecords => _serviceRecords.AsReadOnly();

    // Constructor privado para Entity Framework Core
    private ServiceType() { }

    public ServiceType(string name, string category, decimal defaultPrice, string description = "")
    {
        ValidateAndAssign(name, category, defaultPrice, description);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza todos los datos del tipo de servicio.
    /// </summary>
    public void Update(string name, string category, decimal defaultPrice, string description)
    {
        ValidateAndAssign(name, category, defaultPrice, description);
        UpdatedAt = DateTime.UtcNow;
    }

    private void ValidateAndAssign(string name, string category, decimal defaultPrice, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del servicio es obligatorio.", nameof(name));

        if (defaultPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultPrice), "El precio predeterminado no puede ser negativo.");

        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        DefaultPrice = defaultPrice;
        Description = description?.Trim() ?? string.Empty;
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

    /// <summary>
    /// Realiza el borrado lógico del tipo de servicio y lo marca como inactivo.
    /// </summary>
    public override void SoftDelete()
    {
        base.SoftDelete();
        IsActive = false;
    }
}
