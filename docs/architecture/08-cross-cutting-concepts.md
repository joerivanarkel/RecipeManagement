# 8. Cross-cutting Concepts

## Domain Model

<!-- Core domain entities and their relationships -->

The domain model is implemented in `src/RecipeManagementWebApp/Domain/`:
`Recipe` (Id, Title, Servings, Ingredients, Steps, optional Photo,
CreatedAt/UpdatedAt), `Ingredient` (Quantity decimal?, Unit, Item, Note),
`Step` (Text), and `Photo` (Filename, MimeType, StoredAt). Full entity
relationships and responsibilities are described in `05-building-block-view.md`;
term definitions live in `12-glossary.md`. This section focuses on the
cross-cutting patterns applied around that model (validation, error handling,
persistence), not the model's own structure.

## Security

<!-- Authentication, authorization, data protection -->

- **No authentication or authorization is implemented anywhere in the
  application.** There are no user accounts, login pages, identity
  middleware, or authorization policies/attributes on any page or service.
  Every page and action is reachable by anyone who can reach the app.
- This is a deliberate characteristic of the current design, not a gap: per
  `docs/product/story-map.md`, RecipeManagement is explicitly a personal,
  single-user "what can I cook tonight" tool, and multi-user support is
  explicitly ruled out. There is no requirement driving multi-tenant access
  control, so none has been built.
- **Antiforgery protection** is enabled via `app.UseAntiforgery()` in
  `Program.cs`, protecting Blazor Server's form posts/interactive callbacks
  against CSRF.
- **Transport security**: `app.UseHsts()` and `app.UseHttpsRedirection()` are
  both present. HSTS is only registered when `!app.Environment.IsDevelopment()`
  (standard ASP.NET Core template default); `UseHttpsRedirection()` runs in
  all environments.
- No secrets, API keys, or data-at-rest encryption are present in the
  codebase; `appsettings.json` holds only a local SQLite connection string
  and a photo-storage path.
- TODO: if the product ever moves beyond single-user (contrary to current
  story-map scope), authentication/authorization will need to be designed
  from scratch — nothing in the current code provides a starting point.

## Persistence

<!-- Data storage strategy, ORM, database access -->

- **ORM**: EF Core via `RecipeDbContext` (`src/RecipeManagementWebApp/Data/RecipeDbContext.cs`),
  using the SQLite provider (`UseSqlite`).
- **Database file**: configured through `ConnectionStrings:RecipeDb` in
  `appsettings.json` (`Data Source=App_Data/recipes.db`). `Program.cs` throws
  `InvalidOperationException` at startup if this value is missing, and
  ensures the containing directory exists before the app serves requests.
- **Schema management**: there are **no EF Core migrations**. `Program.cs`
  calls `Database.EnsureCreated()` once at startup inside a DI scope. Schema
  changes currently require dropping/recreating the database or hand-editing
  it — there is no migration history to roll forward or back.
- **Schema constraints**: `RecipeDbContext` configures cascade deletes
  (Recipe → Ingredients/Steps) and enforces max lengths matching
  `RecipeValidator`'s constants: Title 200, Item 200, Unit 50, Note 300,
  Step.Text 1000.
- **Photo storage** is not in the database: binary photo files live on the
  local filesystem under `wwwroot/uploads` (served at `/uploads`), managed by
  `IPhotoStorageService`/`PhotoStorageService`; only photo metadata (`Photo`
  entity: Filename, MimeType, StoredAt) is persisted via EF Core.

## Logging and Monitoring

<!-- Observability approach, structured logging, metrics -->

- There is **no custom logging framework, structured logging library, or
  centralized log aggregation** configured anywhere in the codebase.
- `appsettings.json` and `appsettings.Development.json` both configure only
  the default ASP.NET Core `Logging:LogLevel` settings: `Default: Information`,
  `Microsoft.AspNetCore: Warning`. This is the out-of-the-box template
  configuration, not a deliberately designed logging strategy.
- No Serilog/NLog/OpenTelemetry package references were found, no
  `ILogger<T>` usage in application services (`RecipeService`,
  `PhotoStorageService`, `RecipeValidator`), and no metrics or health-check
  endpoints are registered in `Program.cs`.
- TODO: revisit this section if observability becomes a requirement — nothing
  here should be read as an implied monitoring capability.

## Error Handling

<!-- Error handling strategy, exception hierarchy -->

- **`Result<T>` is the project's chosen pattern for expected/validation
  failures, used instead of throwing exceptions.** Defined in
  `src/RecipeManagementWebApp/Common/Result.cs`: a success/failure wrapper
  with `Succeeded` (bool), `Value` (nullable `T`), and `Errors`
  (`IReadOnlyList<string>`); created via `Result<T>.Success(value)` or
  `Result<T>.Failure(params string[] errors)` / `Result<T>.Failure(IReadOnlyList<string> errors)`.
- **`RecipeValidator`** (`src/RecipeManagementWebApp/Services/RecipeValidator.cs`)
  is a static validator implementing spec 001 FR-002..FR-007, returning
  `Result<ValidatedRecipeInput>`. It accumulates *all* violations into one
  error list (required title, servings as a positive integer, required
  ingredient item, max-length checks on title/item/unit/note/step text,
  positive-decimal ingredient quantities) rather than failing on the first
  problem, and silently drops blank ingredient rows and whitespace-only steps
  rather than rejecting them.
- **Consumption chain**: `RecipeService.CreateAsync` calls
  `RecipeValidator.Validate(input)` first; on failure it short-circuits and
  returns `Result<CreateRecipeOutcome>.Failure(validation.Errors)` without
  touching the database. `AddRecipe.razor`'s `SaveAsync` calls
  `RecipeService.CreateAsync(...)`, checks `result.Succeeded`, and on failure
  copies `result.Errors` straight into an `_errors` list rendered as a
  Bootstrap `alert-danger` list. No exception is thrown or caught for
  validation failures — `Result<T>` is the sole channel.
- A secondary, non-fatal case reuses the same pattern: when photo upload
  fails, `RecipeService.CreateAsync` still succeeds in saving the recipe but
  carries a `PhotoWarning` string through `CreateRecipeOutcome` (rendered as
  an `alert-warning`) — photo upload is optional and must not block saving
  (comment cites FR-013), distinct from the hard validation failures above.
- **Client-side validation in `AddRecipe.razor` is minimal and is not based
  on Blazor's `EditForm`/`DataAnnotations` validation**: the page uses plain
  HTML inputs bound with `@bind`/`@bind:event="oninput"`, not an `EditForm`.
  The only real pre-submit client-side checks are on the photo file
  (`OnPhotoSelected` checks a content-type allowlist and a 5MB size cap,
  setting `_photoError`); `maxlength` attributes on the title/step inputs are
  a UI affordance only. All business-rule validation (required fields,
  numeric parsing, length limits) happens server-side in `RecipeValidator`,
  invoked from the Save button's `@onclick="SaveAsync"` handler.
- `SaveAsync` also wraps the save attempt in a `try/catch (Exception)` that
  falls back to a generic "Failed to save recipe. Please try again." message
  — the one place an actual .NET exception (e.g. unexpected I/O/DB failure)
  is caught in the UI layer, deliberately generic rather than surfacing
  exception details to the user.
- **Unhandled-exception / HTTP-error pages**: `Program.cs` registers
  `app.UseExceptionHandler("/Error", createScopeForErrors: true)`
  (non-Development only), backed by `Components/Pages/Error.razor`, which
  shows a generic error message plus a request/trace ID and explicitly warns
  against enabling Development mode in deployed environments.
  `app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true)`
  re-executes any non-success status code (e.g. 404) to
  `Components/Pages/NotFound.razor`, a simple "Sorry, the content you are
  looking for does not exist" page rendered inside `MainLayout`.

## Configuration

<!-- How the system is configured across environments -->

- Configuration follows the standard ASP.NET Core layered model:
  `appsettings.json` (base) + `appsettings.Development.json` (Development
  overrides), plus environment variables / `ASPNETCORE_ENVIRONMENT`.
- `appsettings.json` defines: `Logging:LogLevel`, `AllowedHosts: "*"`,
  `ConnectionStrings:RecipeDb` (SQLite file path), and a `PhotoStorage`
  section (`RootPath: wwwroot/uploads`, `RequestPathPrefix: /uploads`) bound
  to `PhotoStorageOptions` via `builder.Services.Configure<PhotoStorageOptions>(...)`.
- `appsettings.Development.json` currently only restates the same default
  `Logging` values — it carries no Development-specific overrides beyond
  what the base file already has.
- The database connection string is required: `Program.cs` throws
  `InvalidOperationException` at startup if `ConnectionStrings:RecipeDb` is
  missing, so there is no silent fallback to an in-memory or default store.

## Testing

<!-- Testing strategy and approach (unit, integration, e2e) -->

- `src/RecipeManagementWebApp.Tests/` is an xUnit test project covering
  `RecipeValidatorTests`, `PhotoStorageServiceTests`, `IngredientFormatterTests`,
  and `RecipeServiceTests` — unit/service-level tests around the validation
  and persistence logic described above.
- No integration or end-to-end tests (e.g. bUnit component tests, Playwright)
  were found; UI components (`AddRecipe.razor`, `Home.razor`,
  `RecipeDetail.razor`) are not covered by automated tests.
- TODO: no test-strategy document or coverage target was found in the repo;
  if coverage gaps need formal tracking, record them in
  `11-risks-and-technical-debt.md` rather than here.

## Build and Deployment

<!-- CI/CD pipeline, build process, artifact management -->

- There is **no CI/CD pipeline**: no `.github/workflows/`, no Dockerfile, and
  no docker-compose file exist anywhere in the repository.
- Build/run is local-only via `dotnet build` / `dotnet run`, with
  `https`/`http` launch profiles defined in `Properties/launchSettings.json`.
  See `07-deployment-view.md` for the full picture of how the app is actually
  launched and hosted today.
