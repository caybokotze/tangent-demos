namespace Scheduling.Application.ViewModels;

/// <summary>CONFORMING: one bookable window, as instants rather than a domain value object.</summary>
public sealed record TimeSlotViewModel(DateTimeOffset StartsAt, DateTimeOffset EndsAt);
