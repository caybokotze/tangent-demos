namespace Scheduling.Application.ViewModels;

/// <summary>CONFORMING: one weekly working window of one provider.</summary>
public sealed record AvailabilityViewModel(Guid ProviderId, string Day, TimeOnly StartsAt, TimeOnly EndsAt);
