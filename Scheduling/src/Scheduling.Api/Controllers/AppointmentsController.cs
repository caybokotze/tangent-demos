using System.Text;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Appointments;
using Scheduling.Application.ViewModels;
using Tangent;

namespace Scheduling.Api.Controllers;

/// <summary>Booking, moving and cancelling appointments.</summary>
[Tangent(Contracts.Controller)]
[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AppointmentsController> _logger;

    // VIOLATION (TNG0106): `contract Controller` declares `ctor { injects only IMediator, ILogger<*> }`.
    // The controller wanted the caller's name for a log line and injected the user context to get
    // it, which puts an Application service in an adapter that is supposed to hold none. The fix
    // is to let the handler stamp the audit trail - it already does - and drop the parameter.
    private readonly IUserContext _user;

    /// <summary>Creates the controller.</summary>
    public AppointmentsController(IMediator mediator, ILogger<AppointmentsController> logger, IUserContext user)
    {
        _mediator = mediator;
        _logger = logger;
        _user = user;
    }

    /// <summary>One appointment by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentViewModel>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _mediator.SendAsync(new GetAppointmentQuery(id), cancellationToken);
        return FoundOrNotFound(appointment);
    }

    /// <summary>Every appointment on one UTC day.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentViewModel>>> ListForDayAsync(
        [FromQuery] DateOnly day,
        CancellationToken cancellationToken)
    {
        var appointments = await _mediator.SendAsync(new ListAppointmentsForDayQuery(day), cancellationToken);
        return Ok(appointments);
    }

    /// <summary>Books a provider for a patient.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentViewModel>> BookAsync(
        BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{User} is booking provider {ProviderId}.", _user.UserName, command.ProviderId);
        var appointment = await _mediator.SendAsync(command, cancellationToken);
        return Created($"/api/appointments/{appointment.Id}", appointment);
    }

    /// <summary>Moves an appointment to a new start time.</summary>
    [HttpPut("{id:guid}/slot")]
    [ProducesResponseType(typeof(AppointmentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentViewModel>> RescheduleAsync(
        Guid id,
        RescheduleRequest request,
        CancellationToken cancellationToken)
    {
        var appointment = await _mediator.SendAsync(
            new RescheduleAppointmentCommand(id, request.StartsAt),
            cancellationToken);
        return Ok(appointment);
    }

    /// <summary>Calls an appointment off.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentViewModel>> CancelAsync(
        Guid id,
        [FromQuery] string reason = "cancelled by the patient",
        CancellationToken cancellationToken = default)
    {
        var appointment = await _mediator.SendAsync(new CancelAppointmentCommand(id, reason), cancellationToken);
        return Ok(appointment);
    }

    /// <summary>
    /// The day's list as an iCalendar file. Returns a file, not a view model, so it breaks
    /// `methods where public { returns Task&lt;ActionResult&lt;*&gt;&gt; | Task&lt;IActionResult&gt; }`
    /// and carries the ignore that says so.
    /// </summary>
    [TangentIgnore("calendar feed returns a file, not a view model", Contract = "Controller")]
    [HttpGet("ics")]
    public async Task<FileContentResult> ExportIcsAsync([FromQuery] DateOnly day, CancellationToken cancellationToken)
    {
        var appointments = await _mediator.SendAsync(new ListAppointmentsForDayQuery(day), cancellationToken);

        var builder = new StringBuilder("BEGIN:VCALENDAR\nVERSION:2.0\n");
        foreach (AppointmentViewModel appointment in appointments)
        {
            builder.Append("BEGIN:VEVENT\nUID:").Append(appointment.Id)
                .Append("\nDTSTART:").Append(appointment.StartsAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'"))
                .Append("\nDTEND:").Append(appointment.EndsAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'"))
                .Append("\nSUMMARY:").Append(appointment.PatientName)
                .Append("\nEND:VEVENT\n");
        }

        builder.Append("END:VCALENDAR\n");
        return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/calendar", $"schedule-{day:yyyy-MM-dd}.ics");
    }
}

/// <summary>The body of a reschedule request.</summary>
/// <param name="StartsAt">The new UTC start time. The length of the appointment is kept.</param>
public sealed record RescheduleRequest(DateTimeOffset StartsAt);
