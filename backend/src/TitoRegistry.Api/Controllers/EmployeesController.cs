using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para la gestión y consulta de métricas de empleados.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IServiceRegistryService _serviceRegistry;

    public EmployeesController(IServiceRegistryService serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
    }

    /// <summary>
    /// Lista todos los empleados activos de la empresa.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Colección de empleados disponibles para selección.</returns>
    /// <response code="200">Lista de empleados obtenida con éxito.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEmployees(CancellationToken cancellationToken)
    {
        var employees = await _serviceRegistry.GetAllEmployeesAsync(cancellationToken);
        return Ok(employees);
    }

    /// <summary>
    /// Obtiene el resumen financiero, total acumulado y estadísticas de un empleado.
    /// </summary>
    /// <remarks>
    /// Este endpoint es el núcleo de la Etapa 1 para que el empleado consulte:
    /// - Total acumulado ganado por sus servicios completados.
    /// - Cantidad de servicios realizados.
    /// - Total facturado a clientes.
    /// - Promedio de ganancia por servicio.
    /// - Lista de sus servicios más recientes.
    /// </remarks>
    /// <param name="id">Identificador único del empleado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resumen y acumulados del empleado.</returns>
    /// <response code="200">Resumen y acumulados encontrados.</response>
    /// <response code="404">No se encontró ningún empleado con el Id especificado.</response>
    [HttpGet("{id:int}/summary")]
    [ProducesResponseType(typeof(EmployeeSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeSummary([FromRoute] int id, CancellationToken cancellationToken)
    {
        var summary = await _serviceRegistry.GetEmployeeSummaryAsync(id, cancellationToken);
        if (summary == null)
            return NotFound(new { message = $"No se encontró ningún empleado con Id {id}." });

        return Ok(summary);
    }
}
