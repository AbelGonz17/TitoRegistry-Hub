using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para el registro y consulta de servicios realizados por los empleados.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    /// Registra un nuevo servicio realizado por un empleado.
    /// </summary>
    /// <remarks>
    /// Al registrar el servicio:
    /// - Se valida que el empleado y el tipo de servicio existan y se encuentren activos.
    /// - Se calcula automáticamente la ganancia correspondiente al empleado según su porcentaje de comisión asignado.
    /// - Se congela el valor del acumulado en el registro histórico.
    /// </remarks>
    /// <param name="dto">Datos del servicio a registrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El servicio registrado con su código único y ganancia acumulada calculada.</returns>
    /// <response code="201">Servicio registrado exitosamente.</response>
    /// <response code="400">Datos inválidos o empleado/servicio inactivo.</response>
    /// <response code="404">Empleado o tipo de servicio no encontrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterService([FromBody] CreateServiceRecordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _serviceRegistry.RegisterServiceAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetServicesByEmployee), new { employeeId = created.EmployeeId }, created);
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
    /// Obtiene el historial de servicios registrados por un empleado específico.
    /// </summary>
    /// <param name="employeeId">Identificador único del empleado.</param>
    /// <param name="from">Fecha inicial opcional para filtrar el periodo.</param>
    /// <param name="to">Fecha final opcional para filtrar el periodo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de servicios registrados por el empleado ordenados cronológicamente.</returns>
    /// <response code="200">Lista de servicios recuperada exitosamente.</response>
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(typeof(List<ServiceRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServicesByEmployee(
        [FromRoute] int employeeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var services = await _serviceRegistry.GetServicesByEmployeeAsync(employeeId, from, to, cancellationToken);
        return Ok(services);
    }
}
