using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.DTOs;
using TitoRegistry.Application.Services;

namespace TitoRegistry.Api.Controllers;

/// <summary>
/// Controlador para el cálculo, generación y consulta de reportes de liquidación de choferes (planilla oficial).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class SettlementsController : ControllerBase
{
    private readonly IServiceRegistryService _serviceRegistry;
    private readonly ILogger<SettlementsController> _logger;

    public SettlementsController(IServiceRegistryService serviceRegistry, ILogger<SettlementsController> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Calcula en tiempo real la liquidación mensual de un chofer según sus servicios y deducciones legales.
    /// </summary>
    /// <remarks>
    /// Refleja exactamente la fórmula de Transporte Tito:
    /// 1. Total Bruto = Suma de servicios realizados en el mes
    /// 2. Retención Empresa (30%)
    /// 3. Subtotal Operativo (70%)
    /// 4. Comisión del Chofer (ej: 25% sobre el Subtotal)
    /// 5. Deducciones: ISR y TSS
    /// 6. Incentivo de Teléfono (+RD$ 1,000)
    /// 7. Total Neto a Pagar
    /// </remarks>
    [HttpGet("calculate")]
    [ProducesResponseType(typeof(DriverSettlementReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateSettlement(
        [FromQuery] int driverId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] decimal? companyRetentionRate,
        [FromQuery] decimal? driverCommissionRate,
        [FromQuery] decimal? isr,
        [FromQuery] decimal? tss,
        [FromQuery] decimal? phoneIncentive,
        [FromQuery] decimal otherIncentives = 0m,
        [FromQuery] decimal otherDeductions = 0m,
        CancellationToken cancellationToken = default)
    {
        // Si el usuario es Chofer, solo puede calcular su propia liquidación
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (role == "Chofer" && int.TryParse(userIdStr, out int currentDriverId))
        {
            driverId = currentDriverId;
        }

        if (month < 1 || month > 12)
            return BadRequest(new { message = "El mes debe estar entre 1 y 12." });

        if (year < 2020 || year > 2100)
            return BadRequest(new { message = "El año especificado no es válido." });

        try
        {
            var report = await _serviceRegistry.CalculateDriverSettlementAsync(
                driverId: driverId,
                month: month,
                year: year,
                customCompanyRetentionRate: companyRetentionRate,
                customDriverCommissionRate: driverCommissionRate,
                customIsr: isr,
                customTss: tss,
                customPhoneIncentive: phoneIncentive,
                otherIncentives: otherIncentives,
                otherDeductions: otherDeductions,
                cancellationToken: cancellationToken
            );

            return Ok(report);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al calcular liquidación para el chofer {DriverId}", driverId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al calcular la liquidación." });
        }
    }

    /// <summary>
    /// Guarda o aprueba formalmente la liquidación mensual de un chofer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Administrador")]
    [ProducesResponseType(typeof(DriverSettlementReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveSettlement([FromBody] SaveSettlementDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var saved = await _serviceRegistry.SaveSettlementAsync(dto, cancellationToken);
            return Ok(saved);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar la liquidación.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al guardar la liquidación." });
        }
    }

    /// <summary>
    /// Consulta el historial de liquidaciones cerradas o guardadas.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<DriverSettlementReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettlementHistory(
        [FromQuery] int? driverId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (role == "Chofer" && int.TryParse(userIdStr, out int currentDriverId))
        {
            driverId = currentDriverId;
        }

        var history = await _serviceRegistry.GetSettlementHistoryAsync(driverId, year, cancellationToken);
        return Ok(history);
    }
}
