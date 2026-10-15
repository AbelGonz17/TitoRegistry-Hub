using Microsoft.AspNetCore.Mvc;
using TitoRegistry.Application.Common.Interfaces;

namespace TitoRegistry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public StatusController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "Online",
            architecture = "Clean Architecture",
            system = "TitoRegistry Backend API",
            timestamp = DateTime.UtcNow,
            databaseConfigured = _context != null
        });
    }
}
