using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para la gestión completa (CRUD) del catálogo de servicios con soporte de Soft Delete.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServiceTypesController : ControllerBase
{
    private readonly IServiceRegistryService _serviceRegistry;
    private readonly ILogger<ServiceTypesController> _logger;

    public ServiceTypesController(IServiceRegistryService serviceRegistry, ILogger<ServiceTypesController> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los tipos de servicios activos y no eliminados del catálogo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de servicios disponibles.</returns>
    /// <response code="200">Catálogo recuperado exitosamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveServiceTypes(CancellationToken cancellationToken)
    {
        var types = await _serviceRegistry.GetActiveServiceTypesAsync(cancellationToken);
        return Ok(types);
    }

    /// <summary>
    /// Obtiene el detalle de un tipo de servicio específico por su identificador.
    /// </summary>
    /// <param name="id">Identificador único del tipo de servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalle del servicio.</returns>
    /// <response code="200">Tipo de servicio encontrado.</response>
    /// <response code="404">No se encontró ningún tipo de servicio con el Id proporcionado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceTypeById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var serviceType = await _serviceRegistry.GetServiceTypeByIdAsync(id, cancellationToken);
        if (serviceType == null)
            return NotFound(new { message = $"No se encontró ningún tipo de servicio con Id {id}." });

        return Ok(serviceType);
    }

    /// <summary>
    /// Crea un nuevo tipo de servicio en el catálogo.
    /// </summary>
    /// <param name="dto">Datos para la creación del tipo de servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El tipo de servicio recién creado.</returns>
    /// <response code="201">Tipo de servicio creado satisfactoriamente.</response>
    /// <response code="400">Datos inválidos o campos obligatorios faltantes.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateServiceType([FromBody] CreateServiceTypeDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _serviceRegistry.CreateServiceTypeAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetServiceTypeById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de servicio.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al crear el servicio." });
        }
    }

    /// <summary>
    /// Actualiza la información y precio de un tipo de servicio existente.
    /// </summary>
    /// <param name="id">Identificador único del tipo de servicio a modificar.</param>
    /// <param name="dto">Datos actualizados del servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El tipo de servicio actualizado.</returns>
    /// <response code="200">Tipo de servicio actualizado correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">Tipo de servicio no encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateServiceType(
        [FromRoute] int id,
        [FromBody] UpdateServiceTypeDto dto,
        CancellationToken cancellationToken)
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de servicio.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al actualizar el servicio." });
        }
    }

    /// <summary>
    /// Elimina lógicamente (Soft Delete) un tipo de servicio del catálogo.
    /// </summary>
    /// <remarks>
    /// No destruye el registro físico en la base de datos para preservar la integridad
    /// de los históricos y comisiones de los servicios ya realizados por los empleados.
    /// </remarks>
    /// <param name="id">Identificador único del tipo de servicio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Confirmación del borrado lógico.</returns>
    /// <response code="200">Tipo de servicio eliminado lógicamente.</response>
    /// <response code="404">Tipo de servicio no encontrado o ya eliminado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDeleteServiceType([FromRoute] int id, CancellationToken cancellationToken)
    {
        var deleted = await _serviceRegistry.SoftDeleteServiceTypeAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"No se encontró ningún tipo de servicio activo con Id {id}." });

        return Ok(new { message = $"El tipo de servicio {id} fue eliminado lógicamente (Soft Delete) con éxito." });
    }
}
