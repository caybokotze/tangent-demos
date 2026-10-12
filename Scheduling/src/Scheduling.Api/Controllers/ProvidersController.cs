using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Appointments;
using Scheduling.Application.ViewModels;
using Tangent;

namespace Scheduling.Api.Controllers;

/// <summary>
/// CONFORMING against every rule in `contract Controller`: sealed, inherits ApiControllerBase,
/// carries [ApiController] and [Route], injects only IMediator and ILogger into private readonly
/// fields, and every action is attributed and returns a task of an action result.
/// </summary>
[Tangent(Contracts.Controller)]
[ApiController]
[Route("api/providers")]
public sealed class ProvidersController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProvidersController> _logger;

    /// <summary>Creates the controller.</summary>
    public ProvidersController(IMediator mediator, ILogger<ProvidersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Every registered provider.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProviderViewModel>>> ListAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing providers.");
        var providers = await _mediator.SendAsync(new ListProvidersQuery(), cancellationToken);
        return Ok(providers);
    }
}
