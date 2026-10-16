using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para el registro, consulta y administración de servicios de transporte realizados por los choferes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceRegistryService _serviceRegistry;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IServiceRegistryService serviceRegistry, ILogger<ServicesController> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Consulta los servicios registrados con filtros opcionales (por chofer, rango de fechas, mes o año).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(
        [FromQuery] int? driverId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        // Si el usuario autenticado es Chofer y no es admin, solo puede ver sus propios servicios
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (role == "Chofer" && int.TryParse(userIdStr, out int currentDriverId))
        {
            driverId = currentDriverId;
        }

        var services = await _serviceRegistry.GetServicesAsync(driverId, from, to, month, year, cancellationToken);
        return Ok(services);
    }

    /// <summary>
    /// Obtiene un servicio específico por su identificador único.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var service = await _serviceRegistry.GetServiceByIdAsync(id, cancellationToken);
        if (service == null)
            return NotFound(new { message = $"No se encontró ningún servicio con Id {id}." });

        return Ok(service);
    }

    /// <summary>
    /// Registra un nuevo servicio realizado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterService([FromBody] CreateServiceRecordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Si el chofer registra su servicio y el dto.DriverId no fue provisto o es 0, usar el suyo
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (role == "Chofer" && int.TryParse(userIdStr, out int currentDriverId))
        {
            dto.DriverId = currentDriverId;
        }

        try
        {
            var created = await _serviceRegistry.RegisterServiceAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetServiceById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar servicio.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrió un error interno al registrar el servicio." });
        }
    }

    /// <summary>
    /// Actualiza un servicio registrado previamente.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ServiceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateService([FromRoute] int id, [FromBody] UpdateServiceRecordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _serviceRegistry.UpdateServiceAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar servicio {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al actualizar el servicio." });
        }
    }

    /// <summary>
    /// Realiza el borrado lógico (Soft Delete) de un servicio.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Administrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteService([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _serviceRegistry.SoftDeleteServiceAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"No se encontró ningún servicio con Id {id}." });

        return Ok(new { message = "Servicio eliminado correctamente (soft delete)." });
    }

    // --- CATÁLOGO DE TIPOS DE SERVICIOS ---

    /// <summary>
    /// Lista los tipos de servicio activos disponibles para selección.
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<ServiceTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceTypes(CancellationToken cancellationToken)
    {
        var types = await _serviceRegistry.GetActiveServiceTypesAsync(cancellationToken);
        return Ok(types);
    }

    /// <summary>
    /// Crea un nuevo tipo de servicio en el catálogo.
    /// </summary>
    [HttpPost("types")]
    [Authorize(Roles = "SuperAdmin,Administrador")]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateServiceType([FromBody] CreateServiceTypeDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _serviceRegistry.CreateServiceTypeAsync(dto, cancellationToken);
        return Created($"/api/services/types/{created.Id}", created);
    }

    /// <summary>
    /// Actualiza un tipo de servicio existente.
    /// </summary>
    [HttpPut("types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Administrador")]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateServiceType([FromRoute] int id, [FromBody] UpdateServiceTypeDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _serviceRegistry.UpdateServiceTypeAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Borrado lógico (Soft Delete) de un tipo de servicio.
    /// </summary>
    [HttpDelete("types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Administrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteServiceType([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _serviceRegistry.SoftDeleteServiceTypeAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"No se encontró ningún tipo de servicio con Id {id}." });

        return Ok(new { message = "Tipo de servicio eliminado lógicamente." });
    }
}
