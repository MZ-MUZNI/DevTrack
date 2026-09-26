# Repository Guidelines

## Project Structure & Architecture

DevTrack is a layered ASP.NET Core MVC application targeting .NET 10. Source projects live in `src/`:

- `DevTrack.Core`: domain entities and repository contracts; keep it dependency-free.
- `DevTrack.Application`: business services that depend on Core.
- `DevTrack.Infrastructure`: EF Core contexts, migrations, and repository implementations.
- `DevTrack.Web`: MVC controllers, Razor views, Identity pages, and static files in `wwwroot/`.
- `DevTrack.Api`: HTTP API host.

Tests are in `tests/DevTrack.Tests`. Preserve the dependency direction: UI hosts may reference Application and Infrastructure; Core must not reference other project layers.

## Build, Test, and Development Commands

Run commands from the repository root:

```powershell
dotnet restore                    # restore NuGet packages
dotnet build DevTrack.slnx        # compile all projects
dotnet test DevTrack.slnx         # execute xUnit tests
dotnet run --project src/DevTrack.Web  # start the MVC application
```

The app uses two LocalDB contexts. Apply migrations with `dotnet ef database update --project src/DevTrack.Infrastructure --startup-project src/DevTrack.Web --context DevTrackDbContext`, then repeat using `DevTrackIdentityDbContext`. See `README.md` for prerequisites and full setup.

## Coding Style & Naming

Use C# conventions: four-space indentation, file-scoped namespaces, nullable reference types, and implicit usings. Name public types and members in PascalCase; use camelCase for parameters and locals. Keep interfaces prefixed with `I` (for example, `IWorkItemRepository`). Place domain concepts in Core, business orchestration in Application, and persistence details in Infrastructure. Follow existing Razor, Bootstrap, and controller patterns in `DevTrack.Web`.

## Testing Guidelines

Tests use xUnit and coverlet. Add focused tests alongside the affected layer under `tests/DevTrack.Tests`, with descriptive names such as `CreateAsync_WhenProjectMissing_ThrowsInvalidOperationException`. Run `dotnet test DevTrack.slnx` before opening a pull request. New behavior should include tests where practical; there is no enforced coverage threshold.

## Commits & Pull Requests

Use short, imperative commit subjects consistent with history: `Add auth policy` or `Refine README formatting`. Keep commits scoped to one logical change. Pull requests should explain the behavior change, list validation performed, link related issues when available, and include screenshots for MVC/UI changes. Do not commit secrets, LocalDB files, or generated build output.
