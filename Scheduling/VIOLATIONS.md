# Deliberate violations in `demo/Scheduling`

Six architectural breaks, all of them on purpose, each marked in the source with a
`// VIOLATION (TNGxxxx): why` comment. Every one is a **warning**, so `dotnet build` reports
all six and still succeeds — which is the point: the demo exists to show the diagnostics, and
CI is what turns them fatal (`dotnet build -warnaserror:TNG0101,TNG0106,...`, or
`dotnet tangent check --strict`).

Paths are relative to `demo/Scheduling/`. Line and column are what the analyzer and
`dotnet tangent check` actually reported on the commit this file was written against.

## The six

| # | Code | File:line:col | Type / member | Contract + rule | What is wrong | Fix |
|---|---|---|---|---|---|---|
| 1 | **TNG0101** | `src/Scheduling.Api/Controllers/AvailabilityController.cs:17:14` | `AvailabilityController` | `Controller` — `sealed` (`controllers.tan:8`) | The class is not `sealed`. Tier 0, structural. | **Yes.** `dotnet tangent diff` writes `public sealed class`; the IDE offers the same lightbulb. |
| 2 | **TNG0106** | `src/Scheduling.Api/Controllers/AppointmentsController.cs:26:108` | `AppointmentsController..ctor` | `Controller` — `ctor { injects only IMediator, ILogger<*> }` (`controllers.tan:13`) | Also injects `IUserContext`, putting an Application service inside an HTTP adapter. | No mechanical fix. Drop the parameter; the handler already stamps the audit trail with the user. |
| 3 | **TNG0107** | `src/Scheduling.Domain/Providers/Availability.cs:34:35` | `Availability.EndsAt` | `Entity` — `properties { forbid public set }` (`entities.tan:8`) | Public setter on an entity property. | **Yes.** The fix rewrites `set` to `init`. The better hand-written fix is `private set` plus an `ExtendTo(TimeOnly)` behaviour. |
| 4 | **TNG0201** | `src/Scheduling.Application/ViewModels/AppointmentViewModel.cs:28:39` | `AppointmentViewModel.Status` | `ViewModel` — `forbid using Scheduling.Domain.**` (`services.tan:26`) | The wire format carries the domain enum `AppointmentStatus`, so renaming it silently changes the API. Note that `Application -> Domain` **is** an allowed layer edge; this rule is narrower than layering. | No mechanical fix (Tier 1 never has one). Change the property to `string` and map with `.ToString()`. |
| 5 | **TNG0202** | `src/Scheduling.Api/Diagnostics/SeedInspector.cs:18:61` | `SeedInspector.SeededProviderIds` | `project.tan:7` — `Api -> Application, Domain` | The Api layer reads two constants out of `Scheduling.Infrastructure.Repositories.ProviderRepository`. | No mechanical fix. Move the seed ids into the Domain, or read them through `IProviderRepository`. |
| 6 | **TNG0301** | `src/Scheduling.Application/Appointments/CancelAppointmentCommand.cs:25:88` | `CancelAppointmentCommandHandler..ctor` (`clock`) | `Handler` — `ctor { require each parameter assigned to private readonly field }` (`handlers.tan:15`) | `clock` is parked in a mutable `private IClock _clock`. Tier 2, flow. | No mechanical fix. Add `readonly`. |

Coverage, against the brief: a Tier 0 structural rule (1), a public setter on an entity (3), a
`forbid using` reference rule (4), a layer dependency violation (5), an `injects only` violation
(2), and a Tier 2 flow rule (6).

## What `dotnet tangent diff` can fix

Only Tier 0 violations carry a mechanical fix, so a `diff` run over this demo proposes exactly
two edits — #1 and #3. The other four are reference and flow violations and deliberately carry
none: there is no single right answer to "you depend on the wrong layer".

## Suppressed on purpose

These are genuine rule breaks that an ignore removes. The expected result is **no warning**;
`dotnet tangent check --show-ignored` surfaces them as TNG9000.

| File | Type / member | Mechanism | What it suppresses |
|---|---|---|---|
| `src/Scheduling.Api/Controllers/HealthController.cs` | `HealthController.GetAsync` | `bind Scheduling.Api.Controllers.HealthController to Controller except [ProducesResponseType]` in `bindings.tan` | The missing `[ProducesResponseType]` — verified: commenting the `bind` line out makes TNG0104 appear at `HealthController.cs:23:39`. |
| `src/Scheduling.Api/Controllers/AppointmentsController.cs` | `AppointmentsController.ExportIcsAsync` | `[TangentIgnore("calendar feed returns a file, not a view model", Contract = "Controller")]` on the method | `Task<FileContentResult>` instead of `Task<ActionResult<*>>` (TNG0105) and the missing `[ProducesResponseType]` (TNG0104). |
| `src/Scheduling.Domain/Appointments/AppointmentAuditEntry.cs` | `AppointmentAuditEntry` | `ignore ... from Entity reason "append-only audit row, written field by field by the repository"` in `bindings.tan` | A public constructor (TNG0101) and three public setters (TNG0107). |

All three are listed by `tangent check --show-ignored` as hidden TNG9000 diagnostics.

## Conforming types

These must produce **zero** diagnostics. If any of them ever reports, the analyzer has a false
positive.

| Type | Contract |
|---|---|
| `Scheduling.Api.Controllers.AppointmentsController` | `Controller` (apart from #2) |
| `Scheduling.Api.Controllers.ProvidersController` | `Controller` |
| `Scheduling.Api.Controllers.HealthController` | `Controller` (with the `except` clause) |
| `Scheduling.Domain.Appointments.Appointment` | `Entity`, `AggregateRoot` |
| `Scheduling.Domain.Providers.Provider` | `Entity`, `AggregateRoot` |
| `Scheduling.Domain.Services.SlotFinderService` | `DomainService` |
| `Scheduling.Domain.Services.OverlapPolicy` | `DomainService` |
| `Scheduling.Infrastructure.Repositories.AppointmentRepository` | `Repository` |
| `Scheduling.Infrastructure.Repositories.ProviderRepository` | `Repository` |
| `Scheduling.Application.Appointments.BookAppointmentCommandHandler` | `Handler` |
| `Scheduling.Application.Appointments.RescheduleAppointmentCommandHandler` | `Handler` |
| `Scheduling.Application.Appointments.GetAppointmentQueryHandler` | `Handler` |
| `Scheduling.Application.Appointments.ListAppointmentsForDayQueryHandler` | `Handler` |
| `Scheduling.Application.Appointments.FindAvailableSlotsQueryHandler` | `Handler` |
| `Scheduling.Application.Appointments.ListProvidersQueryHandler` | `Handler` |
| `Scheduling.Application.Appointments.GetProviderAvailabilityQueryHandler` | `Handler` |
| `Scheduling.Application.ViewModels.ProviderViewModel` | `ViewModel` |
| `Scheduling.Application.ViewModels.TimeSlotViewModel` | `ViewModel` |
| `Scheduling.Application.ViewModels.AvailabilityViewModel` | `ViewModel` |
