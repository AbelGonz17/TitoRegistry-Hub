using TitoRegistry.Domain.Common;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un servicio ejecutado y registrado por un empleado.
/// Contiene la información del cliente, monto cobrado y el acumulado inmutable del empleado.
/// </summary>
public class ServiceRecord : BaseEntity
{
    public string ServiceCode { get; private set; } = string.Empty;

    public int EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public int ServiceTypeId { get; private set; }
    public ServiceType? ServiceType { get; private set; }

    public string ClientName { get; private set; } = string.Empty;
    public string ClientAddress { get; private set; } = string.Empty;
    public string ClientPhone { get; private set; } = string.Empty;

    public DateTime ServiceDate { get; private set; }
    public decimal PriceCharged { get; private set; }
    public decimal EmployeeEarnings { get; private set; } // Monto congelado que acumula el empleado

    public ServiceStatus Status { get; private set; } = ServiceStatus.Completado;
    public string Notes { get; private set; } = string.Empty;

    // Constructor privado para Entity Framework Core
    private ServiceRecord() { }

    public ServiceRecord(
        string serviceCode,
        int employeeId,
        int serviceTypeId,
        string clientName,
        string clientAddress,
        string clientPhone,
        DateTime serviceDate,
        decimal priceCharged,
        decimal employeeEarnings,
        ServiceStatus status = ServiceStatus.Completado,
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(serviceCode))
            throw new ArgumentException("El código de servicio es obligatorio.", nameof(serviceCode));

        if (employeeId <= 0)
            throw new ArgumentException("El empleado asignado no es válido.", nameof(employeeId));

        if (serviceTypeId <= 0)
            throw new ArgumentException("El tipo de servicio no es válido.", nameof(serviceTypeId));

        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(clientName));

        if (priceCharged < 0)
            throw new ArgumentOutOfRangeException(nameof(priceCharged), "El precio cobrado no puede ser negativo.");

        if (employeeEarnings < 0)
            throw new ArgumentOutOfRangeException(nameof(employeeEarnings), "La ganancia del empleado no puede ser negativa.");

        ServiceCode = serviceCode.Trim().ToUpperInvariant();
        EmployeeId = employeeId;
        ServiceTypeId = serviceTypeId;
        ClientName = clientName.Trim();
        ClientAddress = clientAddress?.Trim() ?? string.Empty;
        ClientPhone = clientPhone?.Trim() ?? string.Empty;
        ServiceDate = serviceDate == default ? DateTime.UtcNow : serviceDate;
        PriceCharged = priceCharged;
        EmployeeEarnings = employeeEarnings;
        Status = status;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Método de fábrica (Factory method) para crear un nuevo registro de servicio
    /// calculando y congelando la ganancia del empleado a partir de su tasa actual.
    /// </summary>
    public static ServiceRecord Create(
        string serviceCode,
        Employee employee,
        ServiceType serviceType,
        string clientName,
        string clientAddress,
        string clientPhone,
        DateTime serviceDate,
        decimal priceCharged,
        string notes = "")
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(serviceType);

        if (!employee.IsActive)
            throw new InvalidOperationException($"El empleado {employee.FullName} se encuentra inactivo y no puede registrar servicios.");

        if (!serviceType.IsActive)
            throw new InvalidOperationException($"El servicio '{serviceType.Name}' está inactivo en el catálogo.");

        // Cálculo de ganancia encapsulado en el dominio del empleado
        decimal calculatedEarnings = employee.CalculateEarnings(priceCharged);

        return new ServiceRecord(
            serviceCode,
            employee.Id,
            serviceType.Id,
            clientName,
            clientAddress,
            clientPhone,
            serviceDate,
            priceCharged,
            calculatedEarnings,
            ServiceStatus.Completado,
            notes
        );
    }

    /// <summary>
    /// Marca el servicio como completado para que sume formalmente al acumulado del empleado.
    /// </summary>
    public void CompleteService()
    {
        if (Status == ServiceStatus.Cancelado)
            throw new InvalidOperationException("No se puede completar un servicio que ya fue cancelado.");

        Status = ServiceStatus.Completado;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancela el servicio y deja en cero el acumulado del empleado para este registro.
    /// </summary>
    public void CancelService(string cancellationReason)
    {
        Status = ServiceStatus.Cancelado;
        EmployeeEarnings = 0m;
        Notes = string.IsNullOrWhiteSpace(Notes) 
            ? $"Cancelado: {cancellationReason}" 
            : $"{Notes} | Cancelado: {cancellationReason}";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza notas o detalles operativos adicionales del servicio.
    /// </summary>
    public void UpdateNotes(string newNotes)
    {
        Notes = newNotes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
