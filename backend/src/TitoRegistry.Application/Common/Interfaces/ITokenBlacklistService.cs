namespace TitoRegistry.Application.Common.Interfaces;

/// <summary>
/// Servicio para la invalidación y revocación en memoria o caché de tokens JWT activos (cierre de sesión y cambio de credenciales).
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Registra un identificador de token (JTI) en la lista negra hasta su fecha de expiración.
    /// </summary>
    void BlacklistToken(string jti, DateTime expiration);

    /// <summary>
    /// Determina si un identificador de token (JTI) se encuentra revocado.
    /// </summary>
    bool IsBlacklisted(string jti);
}
