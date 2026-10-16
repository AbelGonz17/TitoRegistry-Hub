using TitoRegistry.Domain.Common;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa a un usuario del sistema (SuperAdmin, Admin o Chofer).
/// Cuenta con credenciales de autenticación segura y soporte para Soft Delete.
/// </summary>
public class User : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Chofer"; // SuperAdmin, Admin, Chofer
    public string Department { get; private set; } = "Operaciones";
    public decimal CommissionRate { get; private set; } = 25.0m;
    public bool IsActive { get; private set; } = true;
    public bool IsSuperAdmin { get; private set; } = false;
    public string Avatar { get; private set; } = string.Empty;

    // Constructor privado para EF Core
    private User() { }

    public User(
        string code,
        string fullName,
        string email,
        string passwordHash,
        string phone,
        string role,
        string department,
        decimal commissionRate,
        bool isSuperAdmin = false,
        string avatar = "")
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre del usuario es obligatorio.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El correo electrónico es obligatorio.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));

        Code = string.IsNullOrWhiteSpace(code) ? "USR-001" : code.Trim().ToUpperInvariant();
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Phone = phone?.Trim() ?? string.Empty;
        Role = string.IsNullOrWhiteSpace(role) ? "Chofer" : role.Trim();
        Department = string.IsNullOrWhiteSpace(department) ? "Operaciones" : department.Trim();
        CommissionRate = commissionRate < 0 ? 0 : commissionRate;
        IsSuperAdmin = isSuperAdmin || Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
        Avatar = avatar?.Trim() ?? string.Empty;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la contraseña encriptada del usuario.
    /// </summary>
    public void SetPasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza el perfil y datos operativos del usuario.
    /// </summary>
    public void UpdateProfile(
        string fullName,
        string phone,
        string role,
        string department,
        decimal commissionRate,
        bool isActive,
        string avatar = "")
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre del usuario es obligatorio.", nameof(fullName));

        FullName = fullName.Trim();
        Phone = phone?.Trim() ?? string.Empty;
        Role = string.IsNullOrWhiteSpace(role) ? Role : role.Trim();
        Department = string.IsNullOrWhiteSpace(department) ? Department : department.Trim();
        CommissionRate = commissionRate < 0 ? 0 : commissionRate;
        IsActive = isActive;
        if (!string.IsNullOrWhiteSpace(avatar))
        {
            Avatar = avatar.Trim();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Desactiva y realiza borrado lógico del usuario.
    /// </summary>
    public override void SoftDelete()
    {
        base.SoftDelete();
        IsActive = false;
    }
}
