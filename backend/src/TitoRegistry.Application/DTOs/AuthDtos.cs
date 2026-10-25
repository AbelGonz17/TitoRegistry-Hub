using System.ComponentModel.DataAnnotations;

namespace TitoRegistry.Application.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(4, ErrorMessage = "La contraseña debe tener al menos 4 caracteres.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberUser { get; set; } = true;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Opcional: Si se deja en blanco, la contraseña inicial será el código asignado (ej: CHO-007)
    /// </summary>
    public string? Password { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Role { get; set; } = "Chofer"; // SuperAdmin, Admin, Chofer

    public string Department { get; set; } = "Operaciones";

    [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0% y 100%.")]
    public decimal CommissionRate { get; set; } = 25.0m;
}

public class UpdateUserDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    public string FullName { get; set; } = string.Empty;

    public string? Password { get; set; } // Opcional si no se desea cambiar

    public string Phone { get; set; } = string.Empty;

    public string Role { get; set; } = "Chofer";

    public string Department { get; set; } = "Operaciones";

    [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0% y 100%.")]
    public decimal CommissionRate { get; set; } = 25.0m;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO para el cambio de contraseña de un usuario autenticado.
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}
