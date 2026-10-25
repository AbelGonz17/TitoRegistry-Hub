using TitoRegistry.Domain.Common;
using TitoRegistry.Domain.Enums;

namespace TitoRegistry.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa a un usuario del sistema (SuperAdmin, Administrador o Chofer).
/// Cuenta con credenciales de autenticación segura, rol tipado y datos operativos básicos.
/// </summary>
public class User : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Chofer;
    public string Department { get; private set; } = "Operaciones";
    public decimal CommissionRate { get; private set; } = 25.0m;
    public bool IsActive { get; private set; } = true;
    public bool MustChangePassword { get; private set; } = true;

    // Relaciones de navegación
    private readonly List<ServiceRecord> _serviceRecords = new();
    public virtual IReadOnlyCollection<ServiceRecord> ServiceRecords => _serviceRecords.AsReadOnly();

    private readonly List<DriverSettlement> _settlements = new();
    public virtual IReadOnlyCollection<DriverSettlement> Settlements => _settlements.AsReadOnly();

    private readonly List<DriverBenefit> _driverBenefits = new();
    public virtual IReadOnlyCollection<DriverBenefit> DriverBenefits => _driverBenefits.AsReadOnly();

    // Configuración fiscal y de retenciones legales (TSS e ISR)
    public virtual DriverPayrollConfig? PayrollConfig { get; private set; }

    // Constructor privado para EF Core
    private User() { }

    public User(
        string code,
        string fullName,
        string email,
        string passwordHash,
        string phone,
        UserRole role,
        string department,
        decimal commissionRate,
        bool mustChangePassword = true)
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
        Role = role;
        Department = string.IsNullOrWhiteSpace(department) ? "Operaciones" : department.Trim();
        CommissionRate = commissionRate < 0 ? 0 : commissionRate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza la contraseña encriptada del usuario.
    /// </summary>
    /// <summary>
    /// Completa el cambio obligatorio de contraseña en el primer acceso.
    /// </summary>
    public void CompleteFirstPasswordChange(string newPasswordHash)
    {
        SetPasswordHash(newPasswordHash);
        MustChangePassword = false;
        UpdatedAt = DateTime.UtcNow;
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
        UserRole role,
        string department,
        decimal commissionRate,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre del usuario es obligatorio.", nameof(fullName));

        FullName = fullName.Trim();
        Phone = phone?.Trim() ?? string.Empty;
        Role = role;
        Department = string.IsNullOrWhiteSpace(department) ? Department : department.Trim();
        CommissionRate = commissionRate < 0 ? 0 : commissionRate;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Asigna o actualiza un beneficio recurrente para este chofer.
    /// </summary>
    public void AssignBenefit(Benefit benefit, decimal? customAmount = null, string notes = "")
    {
        ArgumentNullException.ThrowIfNull(benefit);
        var amount = customAmount ?? benefit.DefaultAmount;
        var existing = _driverBenefits.FirstOrDefault(b => b.BenefitId == benefit.Id);
        if (existing != null)
        {
            existing.UpdateAmount(amount, notes);
            existing.Activate();
        }
        else
        {
            _driverBenefits.Add(new DriverBenefit(Id, benefit.Id, amount, true, notes));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Desactiva un beneficio asignado a este chofer.
    /// </summary>
    public void RemoveBenefit(int benefitId)
    {
        var existing = _driverBenefits.FirstOrDefault(b => b.BenefitId == benefitId);
        if (existing != null)
        {
            existing.Deactivate();
            UpdatedAt = DateTime.UtcNow;
        }
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
