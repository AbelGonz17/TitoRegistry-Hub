namespace TitoRegistry.Domain.Enums;

/// <summary>
/// Representa el estado actual de un servicio registrado.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Servicio registrado pero aún pendiente de ejecución o aprobación.
    /// </summary>
    Pendiente = 1,

    /// <summary>
    /// Servicio actualmente en proceso de realización.
    /// </summary>
    EnProceso = 2,

    /// <summary>
    /// Servicio ejecutado y validado satisfactoriamente. Acumula para el empleado.
    /// </summary>
    Completado = 3,

    /// <summary>
    /// Servicio cancelado. No genera acumulado para el empleado.
    /// </summary>
    Cancelado = 4
}
