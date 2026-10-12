using Microsoft.AspNetCore.Mvc;
using Scheduling.Api.Diagnostics;

namespace Scheduling.Api.Controllers;

/// <summary>
/// Conforming, with one sanctioned gap: a liveness probe documents no response body, so
/// `bindings.tan` carries
///     bind Scheduling.Api.Controllers.HealthController to Controller except [ProducesResponseType]
/// and the missing attribute produces no diagnostic at all.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ApiControllerBase
{
    private readonly ILogger<HealthController> _logger;

    /// <summary>Creates the controller.</summary>
    public HealthController(ILogger<HealthController> logger) => _logger = logger;

    /// <summary>Liveness, plus the ids this instance was seeded with.</summary>
    [HttpGet]
    public Task<ActionResult<object>> GetAsync()
    {
        _logger.LogTrace("Health probe.");
        return Task.FromResult<ActionResult<object>>(Ok(new
        {
            status = "healthy",
            seededProviders = SeedInspector.SeededProviderIds,
        }));
    }
}
