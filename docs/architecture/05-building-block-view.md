# 5. Building Block View

## Level 1: Overall System

<!-- Top-level decomposition of the system into its main building blocks -->

The application is a single ASP.NET Core project
(`src/RecipeManagementWebApp/`) with no separate class libraries or service
boundaries — all building blocks below are folders/namespaces inside one
deployable process, composed via constructor-injected DI in `Program.cs`.

```
[diagram placeholder: level 1 building blocks — Presentation (Components/) ->
Application Services (Services/) -> Domain (Domain/) -> Persistence (Data/),
with Common/ as a cross-cutting dependency used by Services and Domain is
used by all layers above it]
```

| Building Block | Description |
| -------------- | ----------- |
| Presentation (`Components/`) | Razor Components (Blazor Server, `InteractiveServer` render mode) implementing pages and shared layout. Talks only to the Application Services layer via DI. |
| Application Services (`Services/`) | Stateless/scoped services orchestrating recipe creation, retrieval, deletion, photo storage, input validation and ingredient-quantity formatting. Returns `Result<T>` (see Cross-Cutting) instead of throwing for expected failures. |
| Domain (`Domain/`) | Plain C# classes (`Recipe`, `Ingredient`, `Step`, `Photo`) that are also the EF Core entities — no separate persistence model exists. |
| Persistence (`Data/`) | `RecipeDbContext` (EF Core over SQLite) — the only persistence mechanism; no repository abstraction sits between services and the DbContext. |
| Cross-Cutting (`Common/`) | `Result<T>` success/failure wrapper, the project's standard way of signaling expected failures without exceptions. |

## Level 2: Component Details

<!-- Decomposition of individual building blocks from level 1 -->

### Presentation — `Components/`

<!-- Purpose, interfaces, contained building blocks -->

Razor Components rendered with Blazor Server (`@rendermode InteractiveServer`
on interactive pages, wired up via `AddInteractiveServerComponents()` and
`AddInteractiveServerRenderMode()` in `Program.cs`).

**`Components/Pages/` — routable pages**

| File | Role |
| --- | --- |
| `Home.razor` (+ `Home.razor.css`) | Route `/`. Implements the browse grid (spec 006): responsive card grid via `IRecipeService.GetSummariesAsync`, photo-or-placeholder, title, and a tag list with "+N more" truncation (tags are currently always empty — see Known Gaps). |
| `AddRecipe.razor` | Implements manual recipe creation (spec 001): binds a form to `NewRecipeInput`/`NewIngredientInput`, submits to `IRecipeService.CreateAsync` with an optional `PhotoUploadRequest`, surfaces validation errors and the non-fatal photo-upload warning. |
| `RecipeDetail.razor` (+ `RecipeDetail.razor.css`) | Implements recipe viewing (spec 007): loads a `Recipe` via `IRecipeService.GetByIdAsync`, renders ingredients/steps in `DisplayOrder`, and uses `IngredientFormatter.FormatLine` to scale quantities for a chosen serving size. |
| `Error.razor` | Framework error page, target of `UseExceptionHandler("/Error", ...)` in non-dev environments. |
| `NotFound.razor` | Framework not-found page, target of `UseStatusCodePagesWithReExecute("/not-found", ...)`. |
| `Counter.razor`, `Weather.razor` | **Not part of the product.** Unused ASP.NET Core Blazor starter-kit template leftovers, not reachable from product navigation/story map; candidates for removal. |

**`Components/Layout/` — shared chrome**

| File | Role |
| --- | --- |
| `MainLayout.razor` (+ `.razor.css`) | Default layout wrapping all pages (set in `Routes.razor`). |
| `NavMenu.razor` (+ `.razor.css`) | Site navigation. |
| `ReconnectModal.razor` (+ `.razor.css`, `.razor.js`) | Blazor Server reconnect UI shown when the SignalR circuit drops — a direct consequence of the InteractiveServer render-mode choice. |

```
[diagram placeholder: level 2 — Pages depend on IRecipeService / IPhotoStorageService
via DI; Layout wraps Pages; ReconnectModal is independent of the recipe domain
and only concerns the Blazor Server circuit]
```

### Application Services — `Services/`

| File / Type | Responsibility (evidenced from code) |
| --- | --- |
| `IRecipeService` / `RecipeService` | `CreateAsync`: validates input via `RecipeValidator`, builds a `Recipe` aggregate (Ingredients, Steps) with new GUIDs and `DisplayOrder`, optionally stores a photo via `IPhotoStorageService` — a failed photo upload is downgraded to a `PhotoWarning` string on the returned `CreateRecipeOutcome` rather than failing the whole save (FR-013). `GetSummariesAsync`: projects all recipes (newest first) to `RecipeSummary`, with `Tags` hard-coded to `Array.Empty<string>()`. `GetByIdAsync`: eager-loads Ingredients/Steps (ordered by `DisplayOrder`) and Photo. `DeleteAsync`: deletes the stored photo file best-effort, then removes the `Recipe` row (cascade removes Ingredients/Steps/Photo rows). |
| `IPhotoStorageService` / `PhotoStorageService` | Local filesystem photo storage (NOT cloud storage) under `PhotoStorageOptions.RootPath` (default `wwwroot/uploads`), served back at `PhotoStorageOptions.RequestPathPrefix` (default `/uploads`). Validates content type (only `image/jpeg`, `image/png`, `image/gif`) and a 5 MB max size (`MaxFileSizeBytes`); IO/permission failures are caught and returned as `Result<Photo>.Failure(...)`, never thrown, so `RecipeService.CreateAsync` can treat them as non-fatal. `DeletePhotoFile` is a best-effort file delete that swallows `IOException`/`UnauthorizedAccessException` — the DB row is treated as the source of truth, not the file. |
| `PhotoStorageOptions` | Options-pattern POCO bound from the `PhotoStorage` config section (`RootPath`, `RequestPathPrefix`). |
| `RecipeValidator` (static) | Validates and normalizes `NewRecipeInput` per spec 001 FR-002..FR-007: required/length-bounded Title, Servings parsed as a positive int, Ingredients require ≥1 non-blank row with a required Item (blank rows silently skipped, per-field max lengths enforced: Item 200 / Unit 50 / Note 300), Steps with whitespace-only entries silently dropped (SC-012) and a 1000-char max. Returns `Result<ValidatedRecipeInput>`. |
| `IngredientFormatter` (static) | Formats an `Ingredient` as a prose line for display, scaling `Quantity` by `targetServings / originalServings` (spec 007 FR-004/007/008) and rounding/trimming the scaled value via `FormatQuantity`. |
| `NewRecipeInput` / `NewIngredientInput` (records) | Raw, string-typed form-binding DTOs for `AddRecipe.razor` (Servings/Quantity are strings here; parsing happens in `RecipeValidator`, not in the DTO). |
| `RecipeSummary` (record) | Lightweight projection for the browse grid: `Id`, `Title`, `PhotoUrl`, `Tags`. The code comment on this type states explicitly that `Tags` is always empty "until Story 008 adds tagging" — i.e., this field is a placeholder for a feature that is specified (spec 008) but not implemented. |

```
[diagram placeholder: level 2 — RecipeService depends on RecipeDbContext (Data/)
and IPhotoStorageService; RecipeService and PhotoStorageService both depend on
RecipeValidator/Common.Result; Components/Pages depend only on the two service
interfaces, never directly on RecipeDbContext]
```

### Domain — `Domain/`

Plain C# classes, also used directly as EF Core entities (no separate
persistence/DTO mapping layer):

| Type | Fields |
| --- | --- |
| `Recipe` | `Id` (Guid), `Title`, `Servings` (int), `Ingredients` (List<Ingredient>), `Steps` (List<Step>), `PhotoId` (Guid?), `Photo` (Photo?), `CreatedAt`, `UpdatedAt` (DateTime). |
| `Ingredient` | `Id`, `RecipeId` (Guid), `Quantity` (decimal?), `Unit` (string?), `Item` (string, required), `Note` (string?), `DisplayOrder` (int). Structured ingredients (quantity/unit/item/note), not free text — matches spec 001's domain model. |
| `Step` | `Id`, `RecipeId`, `Text` (string), `DisplayOrder` (int). |
| `Photo` | `Id`, `RecipeId`, `Filename`, `MimeType`, `Size` (long), `StoredAt` (string — the servable URL/path), `UploadedAt` (DateTime). |

### Persistence — `Data/`

`RecipeDbContext` (EF Core, SQLite via `UseSqlite`) exposes `DbSet<Recipe>`,
`DbSet<Ingredient>`, `DbSet<Step>`, `DbSet<Photo>`. Configuration in
`OnModelCreating`:
- Cascade delete from `Recipe` to `Ingredients`, `Steps`, and `Photo`
  (one-to-one via `HasForeignKey<Photo>(p => p.RecipeId)`).
- Max lengths: `Title` 200, `Ingredient.Item` 200, `Ingredient.Unit` 50,
  `Ingredient.Note` 300, `Step.Text` 1000; `Ingredient.Quantity` stored as
  `decimal(18,2)`.
- No separate repository/unit-of-work abstraction — `RecipeService` uses
  `RecipeDbContext` directly.
- No EF Core migrations are used; `Program.cs` calls
  `Database.EnsureCreated()` at startup instead.

### Cross-Cutting — `Common/`

`Result<T>` (`Common/Result.cs`) — the project's actual error-handling
pattern for expected/recoverable failures (used instead of exceptions):
`Succeeded` (bool), `Value` (T?), `Errors` (IReadOnlyList<string>), with
`Result<T>.Success(value)` and `Result<T>.Failure(...)` factory methods. Used
by `RecipeValidator`, `IPhotoStorageService`, and `IRecipeService.CreateAsync`.

## Level 3: Internal Structure (if needed)

<!-- Further decomposition only where architecturally significant -->

No building block currently warrants a Level 3 decomposition — each service
in `Services/` is a single class/static type with no internal sub-components.

### Known gaps: specified but not yet implemented building blocks

The following building blocks appear in `docs/specs/*.md` but have **no
corresponding type, entity, or service in `src/` today**. Listed here so the
architecture documentation stays honest about current state vs. the broader
specified system:

| Specified building block | Spec(s) | Evidence of absence |
| --- | --- | --- |
| `Tag` entity / tagging service | 008 (tag-recipes), 015 (filter by tag) | `RecipeSummary.Tags` is hard-coded to an empty array; no `Tag` class or DbSet exists. |
| `Pantry` / `PantryItem` | 012 (pantry-list), 014 (recipes by available ingredients) | No pantry-related type anywhere under `Domain/`, `Services/`, or `Data/`. |
| `ImportedRecipeDraft` (or similar) | 002-005 (import from URL/text/photo, review import) | No import pipeline, no draft/staging type; `RecipeService.CreateAsync` only supports direct manual input (`NewRecipeInput`). |
| `CookSession` / cook-mode runtime state | 019 (cook-mode step-by-step), 020 (screen wake lock) | No session/state type; `RecipeDetail.razor` only displays a recipe, it does not drive a step-by-step mode. |
| `Timer` | 021 (timer from step) | No timer type or JS interop for timers found. |
| `CookLogEntry` / cook history | 023 (mark recipe cooked), 024 (cook history and frequency) | No log/history entity; `Recipe` has no "last cooked" or cook-count field. |
| Favourite flag | 011 (favourite recipe) | `Recipe` has no boolean/favourite field. |
| Search | 009 (search recipes) | No search method on `IRecipeService`; `GetSummariesAsync` returns all recipes unfiltered. |
| Edit recipe | 010 (edit/delete — delete is implemented via `DeleteAsync`; edit is not) | `IRecipeService` has no `UpdateAsync`/edit method; `AddRecipe.razor` only creates. |

Only specs 001 (add recipe manually), 006 (browse recipes grid), and 007
(view recipe detail) have corresponding building blocks fully realized in
`src/` today.
