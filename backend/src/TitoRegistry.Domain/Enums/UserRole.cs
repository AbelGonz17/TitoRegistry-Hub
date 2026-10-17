namespace TitoRegistry.Domain.Enums;

/// <summary>
/// Roles de usuario dentro del sistema TitoRegistry.
/// Define las capacidades de acceso y perfil operativo.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Propietario / Administrador Principal con acceso irrestricto.
    /// </summary>
    SuperAdmin = 1,

    /// <summary>
    /// Personal administrativo encargado de revisar registros, liquidaciones y catálogos.
    /// </summary>
    Administrador = 2,

    /// <summary>
    /// Conductor / Chofer operativo encargado de registrar viajes y consultar sus pagos.
    /// </summary>
    Chofer = 3
}
