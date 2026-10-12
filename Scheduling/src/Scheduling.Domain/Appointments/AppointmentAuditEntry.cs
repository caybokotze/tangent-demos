namespace Scheduling.Domain.Appointments;

/// <summary>
/// One append-only row of the audit trail. It breaks `contract Entity` on purpose — a public
/// constructor and public setters — and `bindings.tan` ignores it by name with a reason, so
/// the analyzer reports nothing and `tangent check --show-ignored` reports TNG9000.
/// </summary>
public sealed class AppointmentAuditEntry : Entity<Guid>
{
    /// <summary>Creates an empty row for the repository to fill in.</summary>
    public AppointmentAuditEntry()
        : base(Guid.NewGuid())
    {
    }

    /// <summary>The appointment the row is about.</summary>
    public Guid AppointmentId { get; set; }

    /// <summary>What happened: <c>booked</c>, <c>rescheduled</c>, <c>cancelled</c>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>When it happened.</summary>
    public DateTimeOffset At { get; set; }
}
