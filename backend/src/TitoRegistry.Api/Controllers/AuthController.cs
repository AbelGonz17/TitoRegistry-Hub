using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador de autenticación, emisión de tokens JWT y seguridad del usuario.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Inicia sesión y genera un Token JWT firmado con los claims del usuario.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el inicio de sesión para {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno durante la autenticación." });
        }
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado a partir del Token Bearer actual.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(subClaim) || !int.TryParse(subClaim, out int userId))
        {
            return Unauthorized(new { message = "Token de autenticación inválido o sin identificador de usuario." });
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(new { message = "El usuario asociado a este token no existe o fue dado de baja." });

        return Ok(user);
    }

    /// <summary>
    /// Cambia la contraseña del usuario actualmente autenticado validando su contraseña actual.
    /// </summary>
    /// <param name="dto">Contraseña actual y nueva contraseña.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Confirmación del cambio de contraseña.</returns>
    /// <response code="200">Contraseña cambiada con éxito.</response>
    /// <response code="400">Datos inválidos o longitud insuficiente.</response>
    /// <response code="401">Contraseña actual incorrecta o token inválido.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(subClaim) || !int.TryParse(subClaim, out int userId))
        {
            return Unauthorized(new { message = "Token de autenticación inválido o sin identificador de usuario." });
        }

        try
        {
            await _authService.ChangePasswordAsync(userId, dto, cancellationToken);
            return Ok(new { success = true, message = "Contraseña actualizada exitosamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar la contraseña del usuario {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Ocurrió un error interno al actualizar la contraseña." });
        }
    }
}
