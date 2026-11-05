using System.Collections.Concurrent;
using TitoRegistry.Application.Common.Interfaces;

namespace TitoRegistry.Infrastructure.Services;

/// <summary>
/// Implementación concurrente y en memoria para la lista de revocación de tokens JWT.
/// Limpia periódicamente identificadores vencidos para optimizar consumo de memoria.
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();

    public void BlacklistToken(string jti, DateTime expiration)
    {
        if (string.IsNullOrWhiteSpace(jti)) return;

        _blacklistedTokens[jti] = expiration;
        CleanExpiredTokens();
    }

    public bool IsBlacklisted(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti)) return false;

        if (_blacklistedTokens.TryGetValue(jti, out var expiration))
        {
            if (DateTime.UtcNow <= expiration)
            {
                return true;
            }

            // Expiró naturalmente, se puede remover
            _blacklistedTokens.TryRemove(jti, out _);
        }

        return false;
    }

    private void CleanExpiredTokens()
    {
        // Limpieza oportunista si la colección supera un umbral razonable
        if (_blacklistedTokens.Count > 1000)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _blacklistedTokens)
            {
                if (kvp.Value < now)
                {
                    _blacklistedTokens.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
