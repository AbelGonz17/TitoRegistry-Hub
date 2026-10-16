using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime Expiration) GenerateToken(User user);
}
