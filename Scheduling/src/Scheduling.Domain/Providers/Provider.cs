using Tangent;

namespace Scheduling.Domain.Providers;

/// <summary>
/// Someone patients can book: a clinician, a room, a machine. CONFORMING to `contract Entity`
/// and `contract AggregateRoot`.
/// </summary>
[Tangent(Contracts.Entity)]
public sealed class Provider : AggregateRoot<Guid>
{
    private readonly List<Availability> _workingHours;

    private Provider(Guid id, string displayName, string speciality, List<Availability> workingHours)
        : base(id)
    {
        DisplayName = displayName;
        Speciality = speciality;
        _workingHours = workingHours;
    }

    /// <summary>The name patients see.</summary>
    public string DisplayName { get; }

    /// <summary>What the provider is booked for.</summary>
    public string Speciality { get; }

    /// <summary>The weekly working windows, in no particular order.</summary>
    public IReadOnlyList<Availability> WorkingHours => _workingHours;

    /// <summary>Registers a provider with the windows they work.</summary>
    public static Provider Register(Guid id, string displayName, string speciality, IEnumerable<Availability> workingHours)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(speciality);
        ArgumentNullException.ThrowIfNull(workingHours);

        return new Provider(id, displayName, speciality, [.. workingHours]);
    }

    /// <summary>Adds a working window and returns the provider.</summary>
    public Provider Open(Availability window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _workingHours.Add(window);
        return this;
    }

    /// <summary>The windows the provider works on <paramref name="day"/>.</summary>
    public IReadOnlyList<Availability> WorkingHoursOn(DayOfWeek day) =>
        [.. _workingHours.Where(window => window.Day == day)];
}
