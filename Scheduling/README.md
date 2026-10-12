# Scheduling — the Tangent end-to-end demo

A small appointment-scheduling API, built the way a Tangent **user** builds one: four clean
layers, a `.tangent/` folder describing them, and Tangent arriving entirely through **NuGet
packages** — `Tangent.Attributes`, `Tangent.Analyzers`, and `Tangent.Cli` as a local `dotnet`
tool. Nothing here references Tangent's own source. ([`samples/Orders`](https://github.com/caybokotze/tangent/tree/main/samples/Orders) does, on purpose; this
demo is the other half of the story.)

It ships **six deliberate architectural violations**, all warnings, each marked in the source.
`dotnet build` reports all six and succeeds; CI turns them fatal. The full table, with codes,
lines and fixes, is in [VIOLATIONS.md](VIOLATIONS.md).

```
Scheduling/
├── .tangent/                 project.tan, controllers.tan, handlers.tan, repositories.tan,
│                             entities.tan, services.tan, bindings.tan
├── .editorconfig             severity mapping for the TNG diagnostics
├── nuget.config              local = ../../tangent/artifacts/packages, plus nuget.org
├── Directory.Build.props     TFM + the two Tangent package references, for every project
├── dotnet-tools.json         the Tangent.Cli local tool manifest
├── Scheduling.slnx
├── src/
│   ├── Scheduling.Domain           entities, value objects, domain services, repository interfaces
│   ├── Scheduling.Application      mediator, commands/queries + handlers, view models
│   ├── Scheduling.Infrastructure   in-memory repositories, clock, user context
│   └── Scheduling.Api              controllers, composition root
└── tests/Scheduling.Api.Tests      xunit + Microsoft.AspNetCore.Mvc.Testing, over real HTTP
```

## What this demo shows

1. **The analyzer works from a package.** No project reference, no `AdditionalFiles` item in any
   `.csproj`: the analyzer package's own `build/Tangent.Analyzers.props` finds `.tangent/` and
   hands every `.tan` file to the compiler.
2. **The source generator works from the same package.** `[Tangent(Contracts.Controller)]`
   compiles because `Tangent.Generators`, bundled in `Tangent.Analyzers`, emits
   `Tangent.Contracts` from the `.tan` files. Misspell a contract and the *compiler* stops you,
   before the analyzer's TNG0001 does.
3. **One wiring point.** Both package references live in `Directory.Build.props`, so every
   project in the solution — the test project included — gets the attributes and the analyzer.
4. **The CLI agrees with the build**, because it runs the same analyzer over the same IR.
5. **Layers, contracts, ignores and the graph**, on a codebase you can run and `curl`.

## The architecture, as `.tangent/project.tan` states it

```tangent
project "Scheduling" {
    root: "src/"

    layer Api            = Scheduling.Api.**
    layer Application    = Scheduling.Application.**
    layer Domain         = Scheduling.Domain.**
    layer Infrastructure = Scheduling.Infrastructure.**

    allow Microsoft.AspNetCore.**, Microsoft.Extensions.**, Microsoft.OpenApi.**

    Api            -> Application, Domain
    Application    -> Domain
    Infrastructure -> Application, Domain
    Domain         -> nothing
}
```

`project.tan` declares an **allow list**, so every external package these layers may touch is
named: `Microsoft.AspNetCore.**`, `Microsoft.Extensions.**` and `Microsoft.OpenApi.**`. An allow list
switches the model to strict externals — a namespace that no layer declares and no glob matches is
TNG0205 — which is what stops the Domain from quietly taking a dependency on EF Core or on a
serializer. `System.**` and Tangent's own `Tangent.**` attributes are always permitted.

Two things deliberately sit *outside* every layer glob:

- **`Program.cs`** has no namespace, so the composition root — the one place that may name a
  concrete repository — belongs to no layer and TNG0202 cannot reach it.
- **The test project** uses `<RootNamespace>Scheduling.Tests</RootNamespace>`. Had it kept
  `Scheduling.Api.Tests`, it would have matched `layer Api = Scheduling.Api.**` and every test
  that composes the stack would have reported a layer violation of its own.

The contracts also use the closed-set and location rules:

```tangent
contract Repository {                    // .tangent/repositories.tan
    implements only I{Name}              // its own interface, and nothing else
    forbid using contract Repository     // no repository reaches for another repository
}

contract Handler {                       // .tangent/handlers.tan
    require name ends with "Handler"
    require lives in Application
}
```

`forbid using contract Repository` is the rule a namespace glob cannot express: it follows the
contract, so it keeps working when a repository moves, and it catches a reference in a method body
that `injects only` never sees.

---

# Walkthrough

Everything below is real captured output, trimmed only of absolute paths, the `helpUri` suffix
on build warnings, and MSBuild's restore chatter.

## 0. Prerequisites

```console
$ dotnet --version
10.0.112
```

The packages come from `../../tangent/artifacts/packages`, produced by `dotnet pack Tangent.slnx -c Release`
in a clone of [tangent](https://github.com/caybokotze/tangent) next to this repository. Local packs are always version `0.1.0-dev`. `nuget.config` points `local` at that folder.

Restore the CLI tool once:

```console
$ dotnet tool install Tangent.Cli --version 0.1.0-dev --add-source ../../tangent/artifacts/packages
You can invoke the tool from this directory using the following commands: 'dotnet tool run tangent' or 'dotnet tangent'.
Tool 'tangent.cli' (version '0.1.0-dev') was successfully installed.

$ dotnet tangent --version
0.1.0-dev+a2c26052018d3f9d8d4d7e746d879dff9cc6b033
```

On a clean clone the manifest is already committed, so `dotnet tool restore` is enough.

## 1. `dotnet build` — the analyzer, from the package

```console
$ dotnet build Scheduling.slnx
  src/Scheduling.Domain/Providers/Availability.cs(34,35): warning TNG0107: Entity: property EndsAt must not have a public setter. (rule Entity/properties/accessor#0, entities.tan:8)
  src/Scheduling.Application/ViewModels/AppointmentViewModel.cs(28,39): warning TNG0201: ViewModel: reference to AppointmentStatus in forbidden namespace Scheduling.Domain.**. (rule ViewModel/type/using#0, services.tan:26)
  src/Scheduling.Application/Appointments/CancelAppointmentCommand.cs(25,88): warning TNG0301: Handler: constructor parameter 'clock' is not assigned to a private readonly field. (rule Handler/ctor/each-param-assigned-to-field#0, handlers.tan:15)
  src/Scheduling.Api/Controllers/AvailabilityController.cs(17,14): warning TNG0101: Controller: type must be sealed. (rule Controller/type/modifier#0, controllers.tan:8)
  src/Scheduling.Api/Controllers/AppointmentsController.cs(26,108): warning TNG0106: Controller: constructor may only inject IMediator | ILogger<*>; found Scheduling.Application.Abstractions.IUserContext user. (rule Controller/ctor/injects-only#0, controllers.tan:13)
  src/Scheduling.Api/Diagnostics/SeedInspector.cs(18,61): warning TNG0202: Layer Api may not depend on layer Infrastructure (AdaId). (rule layer:Api->Infrastructure, project.tan:7)

Build succeeded.
    6 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.70
```

Six warnings, zero errors — and every warning names the rule and the `.tan` file and line that
declared it. Note that they come from *five* different projects sharing one `.tangent` folder.

The tests pass over the same build:

```console
$ dotnet test Scheduling.slnx
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 925 ms - Scheduling.Api.Tests.dll (net10.0)
```

## 2. The CI gate — the same build, with the warnings made fatal

```console
$ dotnet build Scheduling.slnx -t:Rebuild -warnaserror:TNG0101,TNG0106,TNG0107,TNG0201,TNG0202,TNG0301
  src/Scheduling.Api/Controllers/AppointmentsController.cs(26,108): error TNG0106: Controller: constructor may only inject IMediator | ILogger<*>; found Scheduling.Application.Abstractions.IUserContext user. (rule Controller/ctor/injects-only#0, controllers.tan:13)
  src/Scheduling.Api/Controllers/AvailabilityController.cs(17,14): error TNG0101: Controller: type must be sealed. (rule Controller/type/modifier#0, controllers.tan:8)
  src/Scheduling.Api/Diagnostics/SeedInspector.cs(18,61): error TNG0202: Layer Api may not depend on layer Infrastructure (AdaId). (rule layer:Api->Infrastructure, project.tan:7)
  src/Scheduling.Application/Appointments/CancelAppointmentCommand.cs(25,88): error TNG0301: Handler: constructor parameter 'clock' is not assigned to a private readonly field. (rule Handler/ctor/each-param-assigned-to-field#0, handlers.tan:15)
  src/Scheduling.Application/ViewModels/AppointmentViewModel.cs(28,39): error TNG0201: ViewModel: reference to AppointmentStatus in forbidden namespace Scheduling.Domain.**. (rule ViewModel/type/using#0, services.tan:26)
  src/Scheduling.Domain/Providers/Availability.cs(34,35): error TNG0107: Entity: property EndsAt must not have a public setter. (rule Entity/properties/accessor#0, entities.tan:8)

Build FAILED.
    6 Error(s)
```

> `-warnaserror:TNG*` does **not** work. MSBuild and Roslyn take a comma-separated list of exact
> ids here, not a prefix or a wildcard: `-warnaserror:TNG` and `-warnaserror:TNG*` both promote
> nothing and the build succeeds, which is the sort of green CI you do not want. List the codes,
> or put them in `<WarningsAsErrors>` (see [docs/cli.md](https://github.com/caybokotze/tangent/blob/main/docs/cli.md) §8), or use
> `dotnet tangent check --strict`, which exits 1 on any warning.

Per-diagnostic control lives in [`.editorconfig`](.editorconfig), which keeps TNG0202 at
`warning` so this demo builds green, and shows the `error` line commented out next to it.

## 3. `dotnet tangent lint` — the `.tan` files on their own

No compilation needed, so pointing it at the folder is instant:

```console
$ dotnet tangent lint .tangent
note: no solution or project was found, so contract type names were not resolved (TNG1020 skipped).

7 contracts, 4 layers, 7 files — 0 errors, 0 warnings
```

Point it at the solution and it additionally resolves every C# type name the contracts mention
(`inherits ApiControllerBase`, `injects only IMediator`, `implements IRequestHandler<*, *>`, …):

```console
$ dotnet tangent lint Scheduling.slnx

7 contracts, 4 layers, 7 files — 0 errors, 0 warnings
```

## 4. `dotnet tangent check` — the whole solution, one report

```console
$ dotnet tangent check Scheduling.slnx --sarif tangent.sarif --show-ignored
Controller
  warning TNG0106 src/Scheduling.Api/Controllers/AppointmentsController.cs:26:108  Controller: constructor may only inject IMediator | ILogger<*>; found Scheduling.Application.Abstractions.IUserContext user. (rule Controller/ctor/injects-only#0, controllers.tan:13)
  warning TNG0101 src/Scheduling.Api/Controllers/AvailabilityController.cs:17:14  Controller: type must be sealed. (rule Controller/type/modifier#0, controllers.tan:8)

Entity
  warning TNG0107 src/Scheduling.Domain/Providers/Availability.cs:34:35  Entity: property EndsAt must not have a public setter. (rule Entity/properties/accessor#0, entities.tan:8)

Handler
  warning TNG0301 src/Scheduling.Application/Appointments/CancelAppointmentCommand.cs:25:88  Handler: constructor parameter 'clock' is not assigned to a private readonly field. (rule Handler/ctor/each-param-assigned-to-field#0, handlers.tan:15)

ViewModel
  warning TNG0201 src/Scheduling.Application/ViewModels/AppointmentViewModel.cs:28:39  ViewModel: reference to AppointmentStatus in forbidden namespace Scheduling.Domain.**. (rule ViewModel/type/using#0, services.tan:26)

Layers
  warning TNG0202 src/Scheduling.Api/Diagnostics/SeedInspector.cs:18:61  Layer Api may not depend on layer Infrastructure (AdaId). (rule layer:Api->Infrastructure, project.tan:7)

Ignored
  hidden TNG9000 src/Scheduling.Api/Controllers/AppointmentsController.cs:100:6  Violation ignored: calendar feed returns a file, not a view model
  hidden TNG9000 src/Scheduling.Domain/Appointments/AppointmentAuditEntry.cs:8:21  Violation ignored: append-only audit row, written field by field by the repository

6 violations (0 errors, 6 warnings) in 5 projects, 3 ignored
Wrote SARIF log to tangent.sarif
```

Exit code **0**: `--fail-on` defaults to `error`, and none of these are errors. `--strict` exits
**1** on the same input, which is what CI should run once a codebase is clean:

```console
$ dotnet tangent check Scheduling.slnx --strict > /dev/null; echo $?
1
```

The SARIF log uploads straight to GitHub code scanning; `--json` writes the same findings as a
sorted array that diffs cleanly between runs.

## 5. `dotnet tangent diff` — the mechanical fixes, as a patch

```console
$ dotnet tangent diff Scheduling.slnx
--- a/src/Scheduling.Api/Controllers/AvailabilityController.cs
+++ b/src/Scheduling.Api/Controllers/AvailabilityController.cs
@@ -14,7 +14,7 @@
 // does the IDE lightbulb.
 [ApiController]
 [Route("api/availability")]
-public class AvailabilityController : ApiControllerBase
+public sealed class AvailabilityController : ApiControllerBase
 {
     private readonly IMediator _mediator;
     private readonly ILogger<AvailabilityController> _logger;

--- a/src/Scheduling.Domain/Providers/Availability.cs
+++ b/src/Scheduling.Domain/Providers/Availability.cs
@@ -31,7 +31,7 @@
     // Someone needed to stretch a provider's day and reached for the shortest edit: a public
     // setter. The fix is `private set` plus an `ExtendTo(TimeOnly)` behaviour on the entity.
     /// <summary>When the provider stops working.</summary>
-    public TimeOnly EndsAt { get; set; }
+    public TimeOnly EndsAt { get; init; }

     /// <summary>Opens a weekly working window.</summary>
     public static Availability On(DayOfWeek day, TimeOnly startsAt, TimeOnly endsAt) =>

2 fixes in 2 files. Re-run with --apply to write them.
```

Two fixes for six violations, and that is the design: **only Tier 0 structural rules carry a
mechanical fix.** A reference violation (TNG0201, TNG0202) or a flow violation (TNG0301) has no
single right answer, so Tangent proposes nothing and leaves the judgement to you.

`tangent diff` exits **1** whenever there is something to write, so it behaves like
`git diff --exit-code` in CI. **Do not run `--apply` here** — the six violations are the demo.

## 6. `dotnet tangent graph` — and the viewer

```console
$ dotnet tangent graph Scheduling.slnx -o tangent.graph.json --include-unbound
Wrote tangent.graph.json
  4 layers, 7 contracts, 56 nodes, 192 edges, 6 violations
  45 C# files: 15 tracked, 9 partial, 21 untracked
```

The last line is contract coverage: 21 files declare no type that any contract binds (`Program.cs`,
`IClock`, the repository interfaces, the tests). Add `--include-source` to embed the code around each
violation and the `.tan` text, for a local copy you do not commit.

`--include-unbound` matters here: without it the graph holds only the types a contract binds, and
the type that breaks layering (`ProviderRepository`, reached from `SeedInspector`) is bound to
nothing, so the illegal edge would be dropped.

A copy of this export already sits in the viewer's static folder:

```console
$ cp tangent.graph.json ../../tangent/viewer/public/scheduling.graph.json
$ cd ../../tangent/viewer && npm install && npm run dev
```

then open <http://localhost:5173/?graph=/scheduling.graph.json>.

## 7. Running the API

```console
$ dotnet run --project src/Scheduling.Api --urls http://localhost:5223
```

Two providers are seeded with fixed ids, so the calls below always work. `2026-09-28` is a
Monday; any future weekday will do.

```console
$ curl -s http://localhost:5223/health
{"status":"healthy","seededProviders":["11111111-1111-1111-1111-111111111111","22222222-2222-2222-2222-222222222222"]}

$ curl -s http://localhost:5223/api/providers
[{"id":"11111111-1111-1111-1111-111111111111","displayName":"Ada Whitfield","speciality":"General practice"},
 {"id":"22222222-2222-2222-2222-222222222222","displayName":"Ben Okafor","speciality":"Physiotherapy"}]

$ curl -s "http://localhost:5223/api/availability/11111111-1111-1111-1111-111111111111/slots?day=2026-09-28&durationMinutes=60"
[{"startsAt":"2026-09-28T09:00:00+00:00","endsAt":"2026-09-28T10:00:00+00:00"},
 {"startsAt":"2026-09-28T10:00:00+00:00","endsAt":"2026-09-28T11:00:00+00:00"},
 ...eight one-hour slots, 09:00 to 17:00...]

$ curl -s -i -X POST http://localhost:5223/api/appointments -H 'content-type: application/json' \
      -d '{"providerId":"11111111-1111-1111-1111-111111111111","patientName":"Priya Raman","startsAt":"2026-09-28T10:00:00Z","durationMinutes":30}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: /api/appointments/b97d766e-c493-49e5-8149-aed74f5adcab

{"id":"b97d766e-c493-49e5-8149-aed74f5adcab","providerId":"11111111-1111-1111-1111-111111111111","patientName":"Priya Raman","startsAt":"2026-09-28T10:00:00+00:00","endsAt":"2026-09-28T10:30:00+00:00","status":0}

$ curl -s -i -X POST http://localhost:5223/api/appointments -H 'content-type: application/json' \
      -d '{"providerId":"11111111-1111-1111-1111-111111111111","patientName":"Sam Ojo","startsAt":"2026-09-28T10:15:00Z","durationMinutes":30}'
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{"title":"Scheduling rule violated","status":409,"detail":"Provider 11111111-1111-1111-1111-111111111111 is already booked from 2026-09-28T10:00:00.0000000+00:00 to 2026-09-28T10:30:00.0000000+00:00."}

$ curl -s -X PUT http://localhost:5223/api/appointments/b97d766e-c493-49e5-8149-aed74f5adcab/slot \
      -H 'content-type: application/json' -d '{"startsAt":"2026-09-28T14:00:00Z"}'
{"id":"b97d766e-c493-49e5-8149-aed74f5adcab","providerId":"11111111-...","patientName":"Priya Raman","startsAt":"2026-09-28T14:00:00+00:00","endsAt":"2026-09-28T14:30:00+00:00","status":1}

$ curl -s -X DELETE "http://localhost:5223/api/appointments/b97d766e-c493-49e5-8149-aed74f5adcab?reason=patient%20has%20flu"
{"id":"b97d766e-c493-49e5-8149-aed74f5adcab","providerId":"11111111-...","patientName":"Priya Raman","startsAt":"2026-09-28T14:00:00+00:00","endsAt":"2026-09-28T14:30:00+00:00","status":2}
```

The no-overlap invariant is enforced in the **Domain** (`Appointment.Schedule`), surfaces as a
`SchedulingRuleViolationException`, and `Program.cs` maps it to a 409 `application/problem+json`.

There is no OpenAPI endpoint: `AddOpenApi()` lives in `Microsoft.AspNetCore.OpenApi`, which is
**not** part of the net10.0 shared framework, and its 10.0.9 release drags in a `Microsoft.OpenApi`
with a live advisory (NU1903 / GHSA-v5pm-xwqc-g5wc) that no 2.x release clears. A demo whose
point is a quiet build does not ship a high-severity restore warning.

## 8. In the editor

- **C# squiggles come from the package.** Open `AvailabilityController.cs` and the class name is
  underlined with *"Controller: type must be sealed."* `Ctrl+.` / `Alt+Enter` offers the fix —
  the same `TangentCodeFixProvider` that `tangent diff` drives. The two Tier 0 violations here
  have a lightbulb; the other four deliberately do not. This works in VS Code (C# Dev Kit),
  Rider and Visual Studio alike, because it is an ordinary Roslyn analyzer arriving through an
  ordinary `PackageReference`.
- **`.tan` editing comes from `editors/vscode`.** Syntax highlighting is a TextMate grammar;
  diagnostics, completion, hover and go-to-definition come from the language server the extension
  starts (`tangent serve`):
  ```console
  $ cd ../../tangent/editors/vscode && npm install && npm run package
  $ code --install-extension tangent-*.vsix
  ```
  Editing `.tangent/controllers.tan` then reports parse errors as you type and completes layer
  and contract names.
- The analyzer reads the `.tan` files as `AdditionalFiles`, so **editing a contract changes the
  C# diagnostics** — no rebuild of Tangent, no restart, just Roslyn's normal additional-file
  change notification.

---

# Tangent issues this demo found, and their fixes

Being the first consumer of the *packaged* analyzer surfaced four real defects. All four are fixed
in the repository; the demo no longer carries any workaround.

| # | Issue | Fix |
|---|---|---|
| 1 | `build/Tangent.Analyzers.props` resolved `TangentRoot` with `GetDirectoryNameOfFileAbove(..., '.tangent')`, which only matches files, so no `.tan` file ever reached the compiler. | The props file now walks up to twelve parent directories with an `Exists('.../.tangent')` condition. No override is needed. |
| 2 | The source generator emitted `public static class Tangent.Contracts`, so every project that referenced a sibling warned CS0436 at each `Contracts.X` use. | The class is now `internal`; each project sees only its own copy. |
| 3 | `bind ... except [Attr]` suppressed a rule silently; `--show-ignored` never listed it. | It now reports TNG9000 with the reason `bind except [ProducesResponseType] (bindings.tan:6)`. |
| 4 | The Tier 1 reference walk ignored `is` patterns, casts, `as`, explicitly typed locals, generic type arguments, array element types and `default(T)`. | All of these now report TNG0201 and TNG0202. |

If you rebuild the packages, clear the cached copies first, because the version number (`0.1.0-dev`) does not change:

```console
$ rm -rf ~/.nuget/packages/tangent.*
$ dotnet build-server shutdown
```

---

# How to adopt Tangent in your own solution

1. **Wire the packages once.** In a `Directory.Build.props` at your solution root:

   ```xml
   <ItemGroup>
     <PackageReference Include="Tangent.Attributes" Version="x.y.z" />
     <PackageReference Include="Tangent.Analyzers" Version="x.y.z" PrivateAssets="all" />
   </ItemGroup>
   <PropertyGroup>
     <!-- Until the probe in the package is fixed, point it at the folder yourself. -->
     <TangentRoot>$(MSBuildThisFileDirectory)</TangentRoot>
   </PropertyGroup>
   ```

   `x.y.z` is the latest version on nuget.org.
   `PrivateAssets="all"` keeps the analyzer out of anything you publish. `Tangent.Attributes` is
   dependency-free and its attributes are `[Conditional("TANGENT_ATTRIBUTES")]`, so nothing about
   Tangent ends up in your shipped assemblies either.

2. **Scaffold the model.**

   ```console
   $ dotnet new tool-manifest
   $ dotnet tool install Tangent.Cli   # add --prerelease while only previews exist
   $ dotnet tangent init
   ```

   `init` writes one `layer` per root namespace, every one of them at `-> nothing`. That is
   deliberate: the first `tangent check` shows your whole dependency graph as violations, and you
   open the direction up one `->` line at a time, deciding each edge on purpose.

3. **Start with layers only.** Get `project.tan` right and green before writing a single
   `contract`. Layering is the rule that pays for itself first and the one nobody argues about.

4. **Then add contracts for the shapes you already agree on.** Controllers, repositories,
   handlers, entities — start from [`.tangent/`](.tangent) here and delete what you do not
   believe. `match:` is convention-based, so a contract binds every type that fits without
   touching a line of C#. Reach for `[Tangent(Contracts.X)]` only where the convention cannot see
   it.

5. **Do not turn everything into an error on day one.** Leave the diagnostics as warnings, fix
   them a file at a time, and promote codes to `error` in `.editorconfig` as each reaches zero:

   ```ini
   [*.cs]
   dotnet_diagnostic.TNG0202.severity = error       # layering: paid off, keep it paid off
   dotnet_diagnostic.TNG0104.severity = suggestion  # still working through these
   ```

6. **Use the escape hatches instead of deleting rules.** `ignore X reason "..."` and
   `[TangentIgnore("...", Contract = "...")]` both demand a reason, and
   `tangent check --show-ignored` lists them — which makes the debt visible rather than
   invisible. A rule with three ignores is a rule worth re-reading; a rule you deleted is a
   decision nobody can find.

7. **Gate CI on the CLI as well as the build.**
   `dotnet tangent check --sarif tangent.sarif --fail-on error` gives you one report over the
   whole solution, SARIF for code scanning, and the cross-project name resolution a per-project
   analyzer cannot do. Run `dotnet restore` first: MSBuild cannot load an unrestored project.
