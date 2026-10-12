using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Appointments;
using Scheduling.Application.ViewModels;
using Scheduling.Domain;
using Scheduling.Domain.Repositories;
using Scheduling.Domain.Services;
using Scheduling.Infrastructure;
using Scheduling.Infrastructure.Repositories;

// The composition root, and the only file in Scheduling.Api that names an Infrastructure type.
// It has no namespace, so it belongs to no layer and the `Api -> Application, Domain` rule in
// project.tan does not reach it. Everything below this line is wiring.

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Domain services.
builder.Services.AddSingleton<IOverlapPolicy, OverlapPolicy>();
builder.Services.AddSingleton<ISlotFinderService, SlotFinderService>();
builder.Services.AddSingleton<IClock, SystemClock>();

// Infrastructure.
builder.Services.AddSingleton<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddSingleton<IProviderRepository, ProviderRepository>();
builder.Services.AddSingleton<IUserContext>(new StaticUserContext("demo@scheduling.local"));

// Application: the mediator and one handler per request. The mediator is scoped, not a
// singleton: it resolves handlers from the IServiceProvider it was given, and a singleton would
// hand it the root provider, which refuses to build scoped services.
builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<IRequestHandler<BookAppointmentCommand, AppointmentViewModel>, BookAppointmentCommandHandler>();
builder.Services.AddScoped<IRequestHandler<RescheduleAppointmentCommand, AppointmentViewModel>, RescheduleAppointmentCommandHandler>();
builder.Services.AddScoped<IRequestHandler<CancelAppointmentCommand, AppointmentViewModel>, CancelAppointmentCommandHandler>();
builder.Services.AddScoped<IRequestHandler<GetAppointmentQuery, AppointmentViewModel?>, GetAppointmentQueryHandler>();
builder.Services.AddScoped<IRequestHandler<ListAppointmentsForDayQuery, IReadOnlyList<AppointmentViewModel>>, ListAppointmentsForDayQueryHandler>();
builder.Services.AddScoped<IRequestHandler<FindAvailableSlotsQuery, IReadOnlyList<TimeSlotViewModel>>, FindAvailableSlotsQueryHandler>();
builder.Services.AddScoped<IRequestHandler<ListProvidersQuery, IReadOnlyList<ProviderViewModel>>, ListProvidersQueryHandler>();
builder.Services.AddScoped<IRequestHandler<GetProviderAvailabilityQuery, IReadOnlyList<AvailabilityViewModel>>, GetProviderAvailabilityQueryHandler>();

WebApplication app = builder.Build();

// A broken domain invariant is a 409, not a 500.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    Exception? error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    ProblemDetails problem = error switch
    {
        SchedulingRuleViolationException rule => new ProblemDetails
        {
            Title = "Scheduling rule violated",
            Detail = rule.Message,
            Status = StatusCodes.Status409Conflict,
        },
        ArgumentException argument => new ProblemDetails
        {
            Title = "Invalid request",
            Detail = argument.Message,
            Status = StatusCodes.Status400BadRequest,
        },
        _ => new ProblemDetails
        {
            Title = "Unexpected error",
            Status = StatusCodes.Status500InternalServerError,
        },
    };

    context.Response.StatusCode = problem.Status!.Value;
    await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
}));

app.MapControllers();

await app.RunAsync();

/// <summary>Named so <c>WebApplicationFactory&lt;Program&gt;</c> can find the entry point.</summary>
public partial class Program;
