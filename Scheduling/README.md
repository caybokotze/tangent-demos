# Scheduling

A small appointment-scheduling API with four layers. Tangent arrives only through NuGet: `Tangent.Attributes` and `Tangent.Analyzers` in [Directory.Build.props](Directory.Build.props), and `Tangent.Cli` as a local tool in [dotnet-tools.json](dotnet-tools.json). The contracts are in [.tangent/](.tangent).

The code has six deliberate violations and three ignores. [VIOLATIONS.md](VIOLATIONS.md) lists each one with its fix.

```
Api            -> Application, Domain
Application    -> Domain
Infrastructure -> Application, Domain
Domain         -> nothing
```

## Run it

Pack Tangent first. The [root README](../README.md) explains how.

```bash
dotnet tool restore
dotnet build Scheduling.slnx              # 6 TNG warnings; the build succeeds
dotnet test Scheduling.slnx               # 7 tests
dotnet tangent check --show-ignored       # 6 violations, 3 ignored
dotnet tangent check --explain            # adds the rule text and the offending line
dotnet tangent diff                       # the 2 fixes Tangent can make, as a patch
dotnet tangent graph --include-unbound    # then open https://caybokotze.github.io/tangent/
dotnet run --project src/Scheduling.Api --urls http://localhost:5223
```

The API has two seeded providers. Try `GET /health`, `GET /api/providers` and `GET /api/availability/11111111-1111-1111-1111-111111111111/slots?day=2026-09-28&durationMinutes=60`.

## Things to try

- Open `src/Scheduling.Api/Controllers/AvailabilityController.cs`. The class name has the warning "Controller: type must be sealed". Apply the fix from the IDE, and the warning goes away.
- Add a repository to the `AppointmentsController` constructor. The IDE reports TNG0106, because a controller may inject only `IMediator` and `ILogger<*>`.
- Change a rule in `.tangent/controllers.tan`. The C# warnings change without a rebuild.
- Fail CI on the violations: `dotnet build -warnaserror:TNG0101,TNG0106,TNG0107,TNG0201,TNG0202,TNG0301`. MSBuild needs the exact codes; `TNG*` matches nothing.
