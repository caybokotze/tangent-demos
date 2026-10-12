using Scheduling.Application.ViewModels;
using Scheduling.Domain.Appointments;

namespace Scheduling.Application.Appointments;

/// <summary>The one place a domain appointment becomes a view model.</summary>
internal static class AppointmentMapping
{
    internal static AppointmentViewModel ToViewModel(this Appointment appointment) => new()
    {
        Id = appointment.Id,
        ProviderId = appointment.ProviderId,
        PatientName = appointment.PatientName,
        StartsAt = appointment.Slot.Start,
        EndsAt = appointment.Slot.End,
        Status = appointment.Status,
    };
}
