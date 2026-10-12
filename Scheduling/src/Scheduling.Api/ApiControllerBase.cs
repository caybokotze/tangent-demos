using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Api;

/// <summary>
/// The base every controller in this solution inherits
/// (`contract Controller { inherits ApiControllerBase }`).
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>200 with the value, or 404 when there is none.</summary>
    protected ActionResult<T> FoundOrNotFound<T>(T? value) => value is null ? NotFound() : Ok(value);
}
