using TitoRegistry.Domain.Common;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un servicio o viaje de transporte registrado.
/// Refleja fielmente las columnas del reporte de operaciones de Transporte Tito:
/// Referencia/Cliente, Tipo de Servicio, Descripción del Servicio, Fecha y Precio.
/// </summary>
public class ServiceRecord : BaseEntity
{
    public string ServiceCode { get; private set; } = string.Empty;

    public int DriverId { get; private set; }
    public virtual User? Driver { get; private set; }

    public int ServiceTypeId { get; private set; }
    public virtual ServiceType? ServiceType { get; private set; }

    // Columna "Referencia / CLIENTE" (ej: "Gloris", "Señor Inacio", "Doña Clara", "Referencia")
    public string ClientReference { get; private set; } = string.Empty;

    // Columna "DESCRIBA EL SERVICIO REALIZADO" (ej: "Recoger el señor Carlo", "Viaje a punta cana")
    public string Description { get; private set; } = string.Empty;

    // Columna "FECHA DEL SERVICIO"
    public DateTime ServiceDate { get; private set; }

    // Columna "PRECIO DEL SERVICIO"
    public decimal PriceCharged { get; private set; }

    public ServiceStatus Status { get; private set; } = ServiceStatus.Completado;
    public string Notes { get; private set; } = string.Empty;

    // Constructor privado para Entity Framework Core
    private ServiceRecord() { }

    public ServiceRecord(
        string serviceCode,
        int driverId,
        int serviceTypeId,
        string clientReference,
        string description,
        DateTime serviceDate,
        decimal priceCharged,
        ServiceStatus status = ServiceStatus.Completado,
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(serviceCode))
            throw new ArgumentException("El código de servicio es obligatorio.", nameof(serviceCode));

        if (driverId <= 0)
            throw new ArgumentException("El chofer asignado no es válido.", nameof(driverId));

        if (serviceTypeId <= 0)
            throw new ArgumentException("El tipo de servicio no es válido.", nameof(serviceTypeId));

        if (string.IsNullOrWhiteSpace(clientReference))
            throw new ArgumentException("La referencia o cliente es obligatoria.", nameof(clientReference));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del servicio es obligatoria.", nameof(description));

        if (priceCharged < 0)
            throw new ArgumentOutOfRangeException(nameof(priceCharged), "El precio cobrado no puede ser negativo.");

        ServiceCode = serviceCode.Trim().ToUpperInvariant();
        DriverId = driverId;
        ServiceTypeId = serviceTypeId;
        ClientReference = clientReference.Trim();
        Description = description.Trim();
        ServiceDate = serviceDate == default ? DateTime.UtcNow : serviceDate;
        PriceCharged = priceCharged;
        Status = status;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method para registrar un nuevo servicio validando estado del chofer y tipo de servicio.
    /// </summary>
    public static ServiceRecord Create(
        string serviceCode,
        User driver,
        ServiceType serviceType,
        string clientReference,
        string description,
        DateTime serviceDate,
        decimal priceCharged,
        string notes = "")
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(serviceType);

        if (!driver.IsActive)
            throw new InvalidOperationException($"El chofer {driver.FullName} se encuentra inactivo.");

        if (!serviceType.IsActive)
            throw new InvalidOperationException($"El tipo de servicio '{serviceType.Name}' está inactivo en el catálogo.");

        return new ServiceRecord(
            serviceCode,
            driver.Id,
            serviceType.Id,
            clientReference,
            description,
            serviceDate,
            priceCharged,
            ServiceStatus.Completado,
            notes
        );
    }

    /// <summary>
    /// Actualiza los datos operativos del servicio.
    /// </summary>
    public void Update(
        int serviceTypeId,
        string clientReference,
        string description,
        DateTime serviceDate,
        decimal priceCharged,
        string notes = "")
    {
        if (serviceTypeId <= 0)
            throw new ArgumentException("El tipo de servicio no es válido.", nameof(serviceTypeId));

        if (string.IsNullOrWhiteSpace(clientReference))
            throw new ArgumentException("La referencia o cliente es obligatoria.", nameof(clientReference));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del servicio es obligatoria.", nameof(description));

        if (priceCharged < 0)
            throw new ArgumentOutOfRangeException(nameof(priceCharged), "El precio no puede ser negativo.");

        ServiceTypeId = serviceTypeId;
        ClientReference = clientReference.Trim();
        Description = description.Trim();
        ServiceDate = serviceDate;
        PriceCharged = priceCharged;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = ServiceStatus.Cancelado;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"Cancelado: {reason}" : $"{Notes} | Cancelado: {reason}";
        UpdatedAt = DateTime.UtcNow;
    }
}
