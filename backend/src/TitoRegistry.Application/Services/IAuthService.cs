using TitoRegistry.Application.DTOs;

namespace TitoRegistry.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
