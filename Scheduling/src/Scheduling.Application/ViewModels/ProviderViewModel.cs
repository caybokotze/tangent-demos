namespace Scheduling.Application.ViewModels;

/// <summary>CONFORMING: a positional record of primitives, get-only, no fields.</summary>
public sealed record ProviderViewModel(Guid Id, string DisplayName, string Speciality);
