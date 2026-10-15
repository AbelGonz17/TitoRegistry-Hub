using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa a un empleado de la empresa.
/// </summary>
public class Employee : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public decimal CommissionRate { get; private set; } // Porcentaje ej: 25.0 para 25%
    public bool IsActive { get; private set; } = true;

    // Relación con registros de servicios
    private readonly List<ServiceRecord> _serviceRecords = new();
    public IReadOnlyCollection<ServiceRecord> ServiceRecords => _serviceRecords.AsReadOnly();

    // Constructor privado para Entity Framework Core
    private Employee() { }

    public Employee(string code, string fullName, string email, string phone, string role, string department, decimal commissionRate)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código del empleado no puede estar vacío.", nameof(code));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre del empleado es obligatorio.", nameof(fullName));

        if (commissionRate < 0 || commissionRate > 100)
            throw new ArgumentOutOfRangeException(nameof(commissionRate), "La comisión debe estar entre 0% y 100%.");

        Code = code.Trim().ToUpperInvariant();
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        Role = role.Trim();
        Department = department.Trim();
        CommissionRate = commissionRate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula la ganancia que corresponde al empleado para un servicio según su porcentaje de comisión asignado.
    /// </summary>
    /// <param name="servicePrice">Precio cobrado por el servicio.</param>
    /// <returns>Monto que acumula el empleado.</returns>
    public decimal CalculateEarnings(decimal servicePrice)
    {
        if (servicePrice <= 0) return 0m;
        return Math.Round(servicePrice * (CommissionRate / 100m), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Actualiza el porcentaje de comisión del empleado.
    /// </summary>
    public void UpdateCommissionRate(decimal newRate)
    {
        if (newRate < 0 || newRate > 100)
            throw new ArgumentOutOfRangeException(nameof(newRate), "La comisión debe estar entre 0% y 100%.");

        CommissionRate = newRate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la información de perfil y contacto del empleado.
    /// </summary>
    public void UpdateProfile(string fullName, string email, string phone, string role, string department)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre del empleado no puede estar vacío.", nameof(fullName));

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        Role = role.Trim();
        Department = department.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Desactiva al empleado para impedir nuevos registros de servicio.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactiva al empleado en el sistema.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
