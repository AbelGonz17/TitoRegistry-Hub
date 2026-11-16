using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TitoRegistry.Application.Common.Interfaces;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador de diagnóstico, salud y keep-alive (Cron Job) del sistema TitoRegistry-Hub.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IApplicationDbContext context, IWebHostEnvironment env, ILogger<StatusController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado de operatividad de la API y verifica la conectividad real con la base de datos.
    /// Ideal para Cron Jobs (cron-job.org / UptimeRobot) porque mantiene despiertos tanto Render como Supabase.
    /// </summary>
    [HttpGet]
    [HttpHead]
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
            dbStatusMessage = _env.IsDevelopment() 
                ? $"Error: {ex.Message}" 
                : "Error de conectividad en el servicio de datos.";
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

    /// <summary>
    /// Endpoint ultraligero de ping (keep-alive) para Cron Jobs externos.
    /// </summary>
    [HttpGet("ping")]
    [HttpHead("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { ok = true, status = "awake", timestamp = DateTime.UtcNow });
    }
}