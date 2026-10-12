# Tangent demos

Example solutions that use [Tangent](https://github.com/caybokotze/tangent), an architectural conformance checker for .NET, the way a user does: through the NuGet packages and the `dotnet tangent` tool, never through Tangent's source.

| Demo | What it shows |
|---|---|
| [Scheduling](Scheduling) | An appointment-scheduling API with four layers, seven contracts, six deliberate violations and three ignores. |

Each demo is a stand-alone solution in its own folder, with its own `.tangent/` contracts and its own README.

## Packages

Until the first release is on nuget.org, the demos use local packs. Clone [tangent](https://github.com/caybokotze/tangent) next to this repository and pack it:

```
tangent/          dotnet pack Tangent.slnx -c Release   ->   artifacts/packages, version 0.1.0-dev
tangent-demos/    this repository
```

Each demo's `nuget.config` points at `../../tangent/artifacts/packages`. A new pack keeps the version `0.1.0-dev`, so before you build a demo against it, clear the cached copies and stop the build server:

```bash
rm -rf ~/.nuget/packages/tangent.*
dotnet build-server shutdown
```

## Seeing a demo in the viewer

In a demo folder, export the graph, then open <https://caybokotze.github.io/tangent/> and choose the file. The viewer reads it in your browser and never uploads it.

```bash
dotnet tangent graph --include-unbound --include-source
```
