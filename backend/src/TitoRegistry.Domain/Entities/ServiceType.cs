using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de catálogo que define un tipo de servicio ofrecido por la empresa.
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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del servicio no puede estar vacío.", nameof(name));

        if (defaultPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultPrice), "El precio predeterminado no puede ser negativo.");

        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        DefaultPrice = defaultPrice;
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza el precio sugerido / base del servicio en el catálogo.
    /// </summary>
    public void UpdateDefaultPrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "El precio no puede ser negativo.");

        DefaultPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la información descriptiva del servicio.
    /// </summary>
    public void UpdateDetails(string name, string category, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del servicio es obligatorio.", nameof(name));

        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Description = description?.Trim() ?? string.Empty;
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
}
