# 4. Solution Strategy

<!-- Summary of fundamental decisions and solution strategies that shape the architecture -->

## Technology Decisions

<!-- Key technology choices and their rationale -->

- **Blazor Server with InteractiveServer render mode** (`AddInteractiveServerComponents()` / `.AddInteractiveServerRenderMode()` in `Program.cs`) is the chosen UI architecture — not Blazor WebAssembly, not static/SSR-only rendering. Component logic (event handlers, state) executes on the server inside a SignalR "circuit"; the browser renders a thin client that relays DOM diffs and UI events over a persistent WebSocket connection. Practical implications:
  - Component state (in-progress form state, loaded recipe lists, etc.) lives in server memory per connected circuit, not in the browser — losing the circuit loses in-progress UI state.
  - `Components/Layout/ReconnectModal.razor` exists specifically to handle this: it shows "Rejoining the server…" / "Failed to rejoin" / paused-session UI when the SignalR circuit drops, a direct consequence of choosing Blazor Server over WebAssembly.
  - This fits the product's scope as a personal, single-user, likely-localhost tool (see `docs/product/story-map.md`) — no separate public API or client bundle is needed for a second class of client.

- **SQLite via EF Core**, with the schema created by `Database.EnsureCreated()` at startup (`Program.cs`) rather than versioned EF Core migrations. This is a deliberate, low-ceremony choice appropriate for a personal single-user app with no deployed data to preserve yet. Flagging it as a strategy that will need revisiting (switching to `dotnet ef migrations`) once the schema needs versioned, backward-compatible evolution against an existing `recipes.db` file — `EnsureCreated()` cannot apply incremental schema changes to an existing database.

- **Local filesystem photo storage**, not cloud blob storage: `IPhotoStorageService`/`PhotoStorageService` persist uploaded photos under a configured root path (`PhotoStorage:RootPath = wwwroot/uploads`) and serve them back through a configured request path prefix (`PhotoStorage:RequestPathPrefix = /uploads`), both bound from `appsettings.json` via `PhotoStorageOptions`. This matches the single-machine, personal-tool deployment model and avoids any dependency on external storage infrastructure.

## Top-level Decomposition

<!-- How the system is broken down into components/modules -->

The system uses a simple, pragmatic layering — not a full hexagonal/clean-architecture or CQRS setup:

1. **Presentation — Razor Components** (`Components/Pages/*.razor`, `Components/Layout/*.razor`): `Home.razor` (browse grid, now with a favourites-only filter), `AddRecipe.razor` (recipe creation), `RecipeDetail.razor` (recipe viewing, with favouriting, tagging, and cook-tracking UI), `Pantry.razor` (pantry list), rendered with `@rendermode InteractiveServer`. (`Counter.razor` and `Weather.razor` are unused Blazor starter-kit template leftovers, not part of the product.)
2. **Application/business logic — Services** (`Services/IRecipeService` + `RecipeService`, `Services/IPhotoStorageService` + `PhotoStorageService`, and newly `Services/ITagService` + `TagService`, `Services/IPantryService` + `PantryService`, `Services/ICookLogService` + `CookLogService`), plus supporting helpers `RecipeValidator`, `PantryItemValidator`, and `IngredientFormatter`. The new services follow the same interface + implementation + validator shape as `IRecipeService`/`RecipeService`/`RecipeValidator`, reinforcing that these service interfaces are the only abstraction boundary in the system — there is no separate repository-pattern layer wrapping EF Core, and no domain-event or CQRS infrastructure.
3. **Persistence — EF Core** (`Data/RecipeDbContext`) mapping the domain model (`Domain/Recipe`, `Ingredient`, `Step`, `Photo`, and newly `Tag`, `PantryItem`, `CookLogEntry`) directly to a SQLite file, with cascade deletes from `Recipe` to its `Ingredients`/`Steps`/`Photo`/`Tags`, and length constraints enforced at the EF Core model level (e.g., `Title` 200 chars, `Step.Text` 1000 chars). `CookLogEntry` relates back to `Recipe` via a plain `HasOne<Recipe>()` reference (no navigation property back onto `Recipe`), unlike the owned-collection style used for `Ingredients`/`Steps`/`Tags`.

Services are registered as scoped (`AddScoped<IRecipeService, RecipeService>()`, `AddScoped<IPhotoStorageService, PhotoStorageService>()`, `AddScoped<ITagService, TagService>()`, `AddScoped<IPantryService, PantryService>()`, `AddScoped<ICookLogService, CookLogService>()`) and injected directly into pages — there is no mediator/handler indirection between a page and its service call.

## Approaches to Achieve Quality Goals

<!-- How key quality goals from section 1 are addressed -->

| Quality Goal | Approach |
| ------------ | -------- |
| Predictable handling of expected failures (validation errors, missing/failed photo upload, etc.) | `Common/Result<T>` (`Result.cs`) is the project-wide pattern for expected/recoverable failures: services return `Result<T>.Success(value)` / `Result<T>.Failure(errors)` instead of throwing exceptions for control flow. `IRecipeService.CreateAsync` returns `Result<CreateRecipeOutcome>`, where `CreateRecipeOutcome` also carries a non-fatal `PhotoWarning` so a failed photo save doesn't fail the whole recipe creation. The newer `ITagService`, `IPantryService`, and `ICookLogService` also return `Result<T>` (some nullable, e.g. `Result<PantryItem>?`, to additionally signal "not found" distinctly from "invalid input"), so the pattern holds across the feature set added so far. Exceptions are reserved for truly unrecoverable conditions (e.g., the startup check that throws if `ConnectionStrings:RecipeDb` is missing). |
| Simplicity / low operational overhead, matching the personal single-user scope | SQLite file + `EnsureCreated()` and local-filesystem photo storage avoid any external infrastructure dependency (no separate DB server process, no cloud storage account), consistent with `docs/product/story-map.md`'s explicit non-goals (no multi-user, no sharing, no meal planning). |
| Responsive UI without a separate client build/deploy step | Blazor Server's InteractiveServer render mode keeps all UI logic and state server-side, removing the need for a JS/WASM client bundle or a separate front-end API — at the cost of requiring a persistent connection, mitigated by `ReconnectModal.razor`. |

<!-- TODO: cross-check against the full quality-goal list in 01-introduction-and-goals.md / 10-quality-requirements.md once those sections are finalized; only the goals with clear code-level evidence are listed above. -->

## Organizational Decisions

<!-- Development process, delegation, team structure decisions -->

<!-- TODO: no evidence in the codebase or git history of formal organizational/process decisions (branching strategy, review process, team structure). Git history is scaffolding commits only (init, starter pack, CLAUDE.md init, src folder, Add files via upload, mcp json, a merge commit). Left as a placeholder rather than inventing one. -->
