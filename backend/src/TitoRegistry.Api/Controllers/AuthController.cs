using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador de autenticación y emisión de tokens JWT.
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
    /// <remarks>
    /// Si el sistema se inicia por primera vez y no existe ningún usuario registrado,
    /// el primer inicio de sesión se aprovisiona automáticamente como SuperAdmin.
    /// </remarks>
    /// <param name="request">Credenciales de inicio de sesión.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Token JWT con tiempo de expiración y perfil del usuario autenticado.</returns>
    /// <response code="200">Inicio de sesión exitoso con token JWT generado.</response>
    /// <response code="400">Datos de solicitud inválidos.</response>
    /// <response code="401">Credenciales incorrectas o cuenta inactiva.</response>
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
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Información del usuario en sesión.</returns>
    /// <response code="200">Perfil del usuario autenticado.</response>
    /// <response code="401">No autorizado (Token faltante o expirado).</response>
    /// <response code="404">Usuario no encontrado.</response>
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
}
