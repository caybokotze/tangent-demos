using Microsoft.AspNetCore.Mvc.Testing;

namespace Scheduling.Tests;

/// <summary>
/// One API instance with its own in-memory stores. Every test gets a fresh one, so booking in
/// one test never collides with booking in another.
/// </summary>
public sealed class SchedulingApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The seeded general practitioner, who works weekdays 09:00-17:00 UTC.</summary>
    public static readonly Guid AdaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>The next Monday, at <paramref name="hour"/> o'clock UTC.</summary>
    public static DateTimeOffset NextMondayAt(int hour)
    {
        DateTime today = DateTime.UtcNow.Date;
        int days = (((int)DayOfWeek.Monday - (int)today.DayOfWeek) + 7) % 7;
        if (days == 0)
        {
            days = 7;
        }

        return new DateTimeOffset(today.AddDays(days).AddHours(hour), TimeSpan.Zero);
    }
}
