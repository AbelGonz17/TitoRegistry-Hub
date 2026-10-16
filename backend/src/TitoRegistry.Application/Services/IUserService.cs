using TitoRegistry.Application.DTOs;

namespace TitoRegistry.Application.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteUserAsync(int id, CancellationToken cancellationToken = default);
}
