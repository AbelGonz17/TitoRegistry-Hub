using TitoRegistry.Application.DTOs;

namespace TitoRegistry.Application.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, string? currentUserRole = null, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, string? currentUserRole = null, int? currentUserId = null, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteUserAsync(int id, string? currentUserRole = null, CancellationToken cancellationToken = default);
}
