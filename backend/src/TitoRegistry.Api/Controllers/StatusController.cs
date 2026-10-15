using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.Common.Interfaces;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador de diagnóstico y salud del sistema TitoRegistry-Hub.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IApplicationDbContext context, ILogger<StatusController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado de operatividad de la API y verifica la conectividad real con la base de datos.
    /// </summary>
    /// <remarks>
    /// Este endpoint realiza una comprobación activa:
    /// - Valida que la API esté respondiendo.
    /// - Ejecuta un chequeo de conexión con la base de datos configurada en Infrastructure.
    /// - Si la base de datos no está disponible, responde con código HTTP 503 (Service Unavailable).
    /// </remarks>
    /// <returns>Detalles del estado del sistema y la base de datos.</returns>
    /// <response code="200">El sistema y la base de datos se encuentran completamente operativos.</response>
    /// <response code="503">La base de datos o un servicio crítico no está respondiendo.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        bool dbConnected = false;
        string dbStatusMessage = "Desconectada";

        try
        {
            // Verificación activa real contra la base de datos
            dbConnected = await _context.Database.CanConnectAsync(cancellationToken);
            dbStatusMessage = dbConnected ? "Conectada y disponible" : "No fue posible establecer conexión";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al comprobar la conectividad con la base de datos.");
            dbStatusMessage = $"Error: {ex.Message}";
        }

        var response = new
        {
            status = dbConnected ? "Healthy" : "Unhealthy",
            architecture = "Clean Architecture (.NET 10)",
            system = "TitoRegistry Backend API",
            timestamp = DateTime.UtcNow,
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            database = new
            {
                provider = _context.Database.ProviderName,
                connected = dbConnected,
                message = dbStatusMessage
            }
        };

        if (!dbConnected)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        return Ok(response);
    }
}
