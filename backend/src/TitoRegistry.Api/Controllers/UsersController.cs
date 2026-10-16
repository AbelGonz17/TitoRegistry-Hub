using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para la gestión completa (CRUD) de usuarios del sistema.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos los usuarios activos del sistema (Administradores y Choferes).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Colección de usuarios no eliminados.</returns>
    /// <response code="200">Lista de usuarios obtenida exitosamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Obtiene los detalles de un usuario específico por su identificador.
    /// </summary>
    /// <param name="id">Identificador único del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datos del usuario.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new { message = $"No se encontró ningún usuario con Id {id}." });

        return Ok(user);
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema con contraseña encriptada (BCrypt).
    /// </summary>
    /// <remarks>
    /// Si el rol es 'Chofer', automáticamente se sincroniza con el módulo de empleados
    /// para permitirle registrar servicios y acumular comisiones.
    /// </remarks>
    /// <param name="dto">Datos para la creación del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario creado con su código generado.</returns>
    /// <response code="201">Usuario creado exitosamente.</response>
    /// <response code="400">Datos inválidos o correo ya existente.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _userService.CreateUserAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetUserById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al crear el usuario." });
        }
    }

    /// <summary>
    /// Actualiza la información, rol, departamento o contraseña de un usuario existente.
    /// </summary>
    /// <param name="id">Identificador único del usuario.</param>
    /// <param name="dto">Datos actualizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Usuario modificado correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] int id,
        [FromBody] UpdateUserDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _userService.UpdateUserAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al actualizar el usuario." });
        }
    }

    /// <summary>
    /// Realiza el borrado lógico (Soft Delete) de un usuario.
    /// </summary>
    /// <param name="id">Identificador único del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Confirmación del borrado lógico.</returns>
    /// <response code="200">Usuario eliminado lógicamente.</response>
    /// <response code="404">Usuario no encontrado o ya eliminado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDeleteUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        var deleted = await _userService.SoftDeleteUserAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"No se encontró ningún usuario activo con Id {id}." });

        return Ok(new { message = $"El usuario {id} fue eliminado lógicamente (Soft Delete) con éxito." });
    }
}
