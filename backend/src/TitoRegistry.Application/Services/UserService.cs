using Microsoft.EntityFrameworkCore;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Application.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => MapToDto(u))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var cleanEmail = dto.Email.Trim().ToLowerInvariant();

        bool exists = await _context.Users
            .AnyAsync(u => u.Email == cleanEmail && !u.IsDeleted, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Ya existe un usuario registrado con el correo {cleanEmail}.");

        // Generar correlativo (ADM-00X o CHO-00X)
        bool isDriver = dto.Role.Equals("Chofer", StringComparison.OrdinalIgnoreCase) ||
                        dto.Role.Equals("Conductor", StringComparison.OrdinalIgnoreCase);

        string prefix = isDriver ? "CHO" : "ADM";
        int count = await _context.Users.CountAsync(u => u.Role == dto.Role, cancellationToken);
        string code = $"{prefix}-{(count + 1):D3}";

        string passwordHash = _passwordHasher.HashPassword(dto.Password);
        bool isSuperAdmin = dto.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var user = new User(
            code: code,
            fullName: dto.FullName,
            email: cleanEmail,
            passwordHash: passwordHash,
            phone: dto.Phone,
            role: dto.Role,
            department: dto.Department,
            commissionRate: dto.CommissionRate,
            isSuperAdmin: isSuperAdmin,
            avatar: string.IsNullOrWhiteSpace(dto.Avatar) ? (isDriver ? "🚐" : "👤") : dto.Avatar
        );

        _context.Users.Add(user);

        // Si es chofer y no existe como Employee, crear también el Employee para que pueda registrar viajes
        if (isDriver)
        {
            bool empExists = await _context.Employees.AnyAsync(e => e.Email == cleanEmail && !e.IsDeleted, cancellationToken);
            if (!empExists)
            {
                var emp = new Employee(
                    code: code,
                    fullName: dto.FullName,
                    email: cleanEmail,
                    phone: dto.Phone,
                    role: dto.Role,
                    department: dto.Department,
                    commissionRate: dto.CommissionRate
                );
                _context.Employees.Add(emp);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"No se encontró ningún usuario con Id {id}.");

        user.UpdateProfile(
            fullName: dto.FullName,
            phone: dto.Phone,
            role: dto.Role,
            department: dto.Department,
            commissionRate: dto.CommissionRate,
            isActive: dto.IsActive,
            avatar: dto.Avatar
        );

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.SetPasswordHash(_passwordHasher.HashPassword(dto.Password));
        }

        // Si es chofer, sincronizar con Employee
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == user.Email && !e.IsDeleted, cancellationToken);
        if (employee != null)
        {
            employee.UpdateProfile(user.FullName, user.Email, user.Phone, user.Role, user.Department);
            employee.UpdateCommissionRate(user.CommissionRate);
            if (!user.IsActive) employee.Deactivate(); else employee.Activate();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<bool> SoftDeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null)
            return false;

        user.SoftDelete();

        // Desactivar también al empleado vinculado
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == user.Email && !e.IsDeleted, cancellationToken);
        if (employee != null)
        {
            employee.SoftDelete();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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
