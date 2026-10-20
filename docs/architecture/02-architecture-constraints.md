# 2. Architecture Constraints

## Technical Constraints

<!-- Hardware, software, infrastructure, and technology constraints -->

| Constraint | Explanation |
| ---------- | ----------- |
| .NET 10.0 target framework | `TargetFramework=net10.0` in both `RecipeManagementWebApp.csproj` and `RecipeManagementWebApp.Tests.csproj`. |
| Nullable reference types mandatory | `Nullable=enable` in both csproj files — optional reference-typed members must be declared `string?` etc.; this is a binding coding constraint, not just a style preference. |
| ImplicitUsings enabled | `ImplicitUsings=enable` in both csproj files — common namespaces are implicitly imported; avoid redundant `using` statements for the implicit set. |
| Blazor Server as the only UI technology | `Program.cs` calls `AddInteractiveServerComponents()` / `AddInteractiveServerRenderMode()`; `BlazorDisableThrowNavigationException=true` is set in the web app csproj, constraining Blazor Server navigation-exception behavior. No WebAssembly or SPA frontend is used. |
| Persistence: EF Core + SQLite only | `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0 (+ `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 as its native dependency) is the only persistence package referenced — no other EF provider, no Dapper/raw ADO.NET, no other RDBMS evidenced anywhere in the codebase. |
| No EF Core migrations | `Program.cs` calls `Database.EnsureCreated()` at startup rather than `dbContext.Database.Migrate()` — schema evolution via migrations is not currently part of the toolchain/process. |
| Local filesystem photo storage | `IPhotoStorageService`/`PhotoStorageOptions` store files under `wwwroot/uploads` (served at `/uploads`) — constrains deployment to a model where the app server has writable local disk; no cloud/object storage (e.g., S3/Blob) is wired up. |
| Mandated test stack | xUnit 2.9.3 (+ `xunit.runner.visualstudio` 3.1.4) is the test framework; `coverlet.collector` 6.0.4 is the mandated coverage-collection tool. Tests reference EF Core Sqlite directly rather than mocking the data layer. |

## Organizational Constraints

<!-- Team structure, schedule, budget, and process constraints -->

| Constraint | Explanation |
| ---------- | ----------- |
| Single-user / personal-tool scope | Per `docs/product/story-map.md`, the product is explicitly scoped to a single home cook; multi-user accounts, sharing, and social features are ruled out "for now" — this constrains the architecture away from multi-tenancy, auth/identity concerns, and per-user data isolation. |
| No CI pipeline today | No `.github/workflows` directory exists in the repo — there is currently no automated build/test gate on commits or PRs. This is a statement of current state, not a recommendation. |
| No containerization today | No `Dockerfile` or `docker-compose.yml` exists — the only documented way to run the app is `dotnet run` (per CLAUDE.md Quick Start); deployment today is local/manual only. |
| Scaffolding-only development history | Git history (`init`, `starter pack`, `CLAUDE.md init`, `src folder`, `Add files via upload`, `mcp json`, one merge commit) shows setup-only commits with no evidence of a branching/PR review process — flagged as unknown rather than invented. |
| CLAUDE.md path drift | CLAUDE.md's Quick Start section describes the project living at `RecipeManagementWebApp/`, but the actual layout is `src/RecipeManagementWebApp/` — anyone following CLAUDE.md's literal commands from the repo root needs to account for the `src/` prefix. |

## Conventions

<!-- Coding standards, documentation guidelines, naming conventions -->

| Convention | Explanation |
| ---------- | ----------- |
| Nullable-safe optional members | Optional reference/value-typed members are declared with `?` (e.g., `Ingredient.Quantity` is `decimal?`, `Unit`/`Note` are nullable strings in the domain model) — enforced by `Nullable=enable`, not just a style guide. |
| `Result<T>` for expected-failure error handling | `Common/Result.cs` provides a `Result<T>` success/failure wrapper with an error-message list; this is the project's convention for handling expected/validation failures instead of throwing exceptions for control flow. |
| Max-length validation enforced at the EF Core model level | `Data/RecipeDbContext.cs` sets explicit max lengths (Title 200, Item 200, Unit 50, Note 300, Step.Text 1000). |
| Routable pages under `Components/Pages/` | Pages use the `@page` directive and `@rendermode InteractiveServer` for interactive components — evidenced by `Home.razor`, `AddRecipe.razor`, `RecipeDetail.razor`. |
| Branching/commit-message conventions | TODO — git history is scaffolding-only and doesn't evidence a real convention; not invented here. |
