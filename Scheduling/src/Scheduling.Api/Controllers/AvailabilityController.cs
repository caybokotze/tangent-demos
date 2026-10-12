using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Appointments;
using Scheduling.Application.ViewModels;

namespace Scheduling.Api.Controllers;

/// <summary>
/// What a provider works, and when they are free.
/// </summary>
// VIOLATION (TNG0101): `contract Controller` requires `sealed`. Nothing derives from this
// controller and nothing should; the class was simply never marked. This is the Tier 0
// structural rule with a code fix - `dotnet tangent diff` writes the `sealed` for you, and so
// does the IDE lightbulb.
[ApiController]
[Route("api/availability")]
public class AvailabilityController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AvailabilityController> _logger;

    /// <summary>Creates the controller.</summary>
    public AvailabilityController(IMediator mediator, ILogger<AvailabilityController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>The weekly working windows of one provider.</summary>
    [HttpGet("{providerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AvailabilityViewModel>>> GetAsync(
        Guid providerId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Reading availability for provider {ProviderId}.", providerId);
        var windows = await _mediator.SendAsync(new GetProviderAvailabilityQuery(providerId), cancellationToken);
        return Ok(windows);
    }

    /// <summary>The free slots of one provider on one UTC day.</summary>
    [HttpGet("{providerId:guid}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeSlotViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TimeSlotViewModel>>> GetSlotsAsync(
        Guid providerId,
        [FromQuery] DateOnly day,
        [FromQuery] int durationMinutes = 30,
        CancellationToken cancellationToken = default)
    {
        var slots = await _mediator.SendAsync(
            new FindAvailableSlotsQuery(providerId, day, durationMinutes),
            cancellationToken);
        return Ok(slots);
    }
}
