using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para la consulta del catálogo de tipos de servicios.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServiceTypesController : ControllerBase
{
    private readonly IServiceRegistryService _serviceRegistry;

    public ServiceTypesController(IServiceRegistryService serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
    }

    /// <summary>
    /// Obtiene el catálogo de tipos de servicios activos disponibles para registrar.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de tipos de servicios con su categoría y precio base sugerido.</returns>
    /// <response code="200">Catálogo de servicios recuperado exitosamente.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveServiceTypes(CancellationToken cancellationToken)
    {
        var types = await _serviceRegistry.GetActiveServiceTypesAsync(cancellationToken);
        return Ok(types);
    }
}
