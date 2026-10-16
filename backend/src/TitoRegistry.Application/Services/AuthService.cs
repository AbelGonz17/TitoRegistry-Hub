using Microsoft.EntityFrameworkCore;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var cleanEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == cleanEmail && !u.IsDeleted, cancellationToken);

        // Si la base de datos no tiene usuarios creados, aprovisionar al primer usuario como SuperAdmin automáticamente
        if (user == null)
        {
            int totalUsers = await _context.Users.CountAsync(cancellationToken);
            if (totalUsers == 0)
            {
                string displayName = cleanEmail.Split('@')[0].Replace('.', ' ').Replace('_', ' ');
                if (string.IsNullOrWhiteSpace(displayName)) displayName = "Super Administrador";

                string passwordHash = _passwordHasher.HashPassword(request.Password);

                user = new User(
                    code: "ADM-001",
                    fullName: char.ToUpper(displayName[0]) + displayName[1..],
                    email: cleanEmail,
                    passwordHash: passwordHash,
                    phone: "",
                    role: "SuperAdmin",
                    department: "Dirección General & Propietario",
                    commissionRate: 0,
                    isSuperAdmin: true,
                    avatar: "👑"
                );

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new UnauthorizedAccessException("Credenciales incorrectas o usuario no registrado.");
            }
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Tu cuenta se encuentra inactiva. Contacta al administrador.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Contraseña incorrecta. Verifique sus datos e intente nuevamente.");
        }

        var (token, expiration) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        return user != null ? MapToDto(user) : null;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Code = user.Code,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Role = user.Role,
        Department = user.Department,
        CommissionRate = user.CommissionRate,
        IsActive = user.IsActive,
        IsSuperAdmin = user.IsSuperAdmin,
        Avatar = user.Avatar,
        CreatedAt = user.CreatedAt
    };
}
