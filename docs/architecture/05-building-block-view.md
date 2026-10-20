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
| Application Services (`Services/`) | Stateless/scoped services orchestrating recipe creation, retrieval, deletion, photo storage, input validation, ingredient-quantity formatting, tagging, favouriting, pantry-item management, and cook-log history. Returns `Result<T>` (see Cross-Cutting) instead of throwing for expected failures. |
| Domain (`Domain/`) | Plain C# classes (`Recipe`, `Ingredient`, `Step`, `Photo`, `Tag`, `PantryItem`, `CookLogEntry`) that are also the EF Core entities — no separate persistence model exists. |
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
| `Home.razor` (+ `Home.razor.css`) | Route `/`. Implements the browse grid (spec 006): responsive card grid via `IRecipeService.GetSummariesAsync`, photo-or-placeholder, title, a tag list with "+N more" truncation (`MaxVisibleTags`/`SplitTags`, now populated from real `RecipeSummary.Tags` data — spec 008), a favourite-star indicator driven by `RecipeSummary.IsFavorite` (spec 011), and client-side filtering to favourites-only. |
| `AddRecipe.razor` | Implements manual recipe creation (spec 001): binds a form to `NewRecipeInput`/`NewIngredientInput`, submits to `IRecipeService.CreateAsync` with an optional `PhotoUploadRequest`, surfaces validation errors and the non-fatal photo-upload warning. |
| `RecipeDetail.razor` (+ `RecipeDetail.razor.css`) | Implements recipe viewing (spec 007) plus three newer features layered on top: loads a `Recipe` via `IRecipeService.GetByIdAsync`, renders ingredients/steps in `DisplayOrder`, and uses `IngredientFormatter.FormatLine` to scale quantities for a chosen serving size. Also hosts: a favourite toggle button (★/☆) calling `IRecipeService.ToggleFavoriteAsync` (spec 011); tag display/add/remove UI with autocomplete suggestions calling `ITagService.AddTagAsync`/`RemoveTagAsync`/`GetAllTagNamesAsync` (spec 008); and a "mark as cooked" action plus cook-frequency display calling `ICookLogService.RecordCookAsync`/`GetFrequencyAsync` (spec 023). Injects `ITagService` and `ICookLogService` directly alongside `IRecipeService`. |
| `Pantry.razor` | Route `/pantry`. Implements the pantry list (spec 012): add/edit/delete `PantryItem` rows grouped by `PantryCategory` (Fridge/Freezer/Cupboards), via `IPantryService.AddAsync`/`GetAllAsync`/`UpdateQuantityAsync`/`DeleteAsync`; surfaces validation errors from `PantryItemValidator`. Does not yet link pantry contents to recipe matching (specs 013/014 — see Known Gaps). |
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
via DI; RecipeDetail.razor additionally depends directly on ITagService and
ICookLogService; Pantry.razor depends on IPantryService; Layout wraps Pages;
ReconnectModal is independent of the recipe domain and only concerns the
Blazor Server circuit]
```

### Application Services — `Services/`

| File / Type | Responsibility (evidenced from code) |
| --- | --- |
| `IRecipeService` / `RecipeService` | `CreateAsync`: validates input via `RecipeValidator`, builds a `Recipe` aggregate (Ingredients, Steps) with new GUIDs and `DisplayOrder`, optionally stores a photo via `IPhotoStorageService` — a failed photo upload is downgraded to a `PhotoWarning` string on the returned `CreateRecipeOutcome` rather than failing the whole save (FR-013). `GetSummariesAsync`: projects all recipes (newest first) to `RecipeSummary`, now including real `Tags` (`r.Tags.Select(t => t.Name)`, spec 008) and `IsFavorite` (spec 011) — no longer hard-coded placeholders. `GetByIdAsync`: eager-loads Ingredients/Steps (ordered by `DisplayOrder`) and Photo (does not `Include` Tags — `RecipeDetail.razor`'s tag UI relies on the `Recipe.Tags` navigation being populated by EF's own tracking/fixup from prior queries or lazy access; see Known Gaps re: verifying this). `DeleteAsync`: deletes the stored photo file best-effort, then removes the `Recipe` row (cascade removes Ingredients/Steps/Photo/Tags rows). `ToggleFavoriteAsync` (new, spec 011): flips `Recipe.IsFavorite` and saves; explicitly does *not* touch `UpdatedAt`, since favouriting is a non-content edit per the spec's domain rule. |
| `ITagService` / `TagService` | New services (spec 008 — tag recipes). `AddTagAsync`: loads a `Recipe` with its `Tags` included, trims/lowercases the tag name, rejects empty or >`NameMaxLength` (50) names via `Result<IReadOnlyList<string>>.Failure`, is idempotent (adding an existing tag name just returns the current list, no duplicate row), and returns `null` if the recipe doesn't exist. `RemoveTagAsync`: deletes a `Tag` row by `(recipeId, tagId)`, returns `false` if not found. `GetAllTagNamesAsync`: distinct, alphabetically sorted tag names across all recipes, used to drive the autocomplete suggestions in `RecipeDetail.razor`. |
| `IPantryService` / `PantryService` | New services (spec 012 — pantry list). `AddAsync`: validates via `PantryItemValidator`, creates a `PantryItem` with `AddedAt`/`UpdatedAt` timestamps. `GetAllAsync`: all items ordered by `Category` then `AddedAt` (drives the grouped-by-category rendering in `Pantry.razor`). `UpdateQuantityAsync`: re-validates and updates only the `Quantity` field + `UpdatedAt`; returns `null` if the item doesn't exist. `DeleteAsync`: removes a `PantryItem` by id. Does not yet expose any "recipes I can make from pantry" matching logic (specs 013/014). |
| `ICookLogService` / `CookLogService` | New services (spec 023 — mark recipe cooked). `RecordCookAsync`: verifies the recipe exists, falls back to the recipe's own `Servings` if an invalid (≤0) value was passed (edge case EC-006, e.g. from a future cook-mode caller), inserts a `CookLogEntry`. `GetFrequencyAsync`: aggregates a recipe's `CookLogEntry` rows into a `CookFrequency` (total count, last-cooked timestamp, counts this month/this week) — used by `RecipeDetail.razor` to display cook history (spec 024's frequency data, consumed here even though 024 itself is otherwise unimplemented — see Known Gaps). |
| `PantryItemValidator` (static) / `NewPantryItemInput` (record) / `CookFrequency` (record) | Supporting validation/DTO/projection types for the above: `PantryItemValidator.Validate` enforces required, ≤100-char `ItemName`/`Quantity` (spec 012 FR-002/FR-003) and returns `Result<ValidatedPantryItem>`; `NewPantryItemInput(Category, ItemName, Quantity)` is the raw form DTO for `Pantry.razor`; `CookFrequency(TotalCount, LastCookedAt, FrequencyThisMonth, FrequencyThisWeek)` is `CookLogService`'s read-model output. |
| `IPhotoStorageService` / `PhotoStorageService` | Local filesystem photo storage (NOT cloud storage) under `PhotoStorageOptions.RootPath` (default `wwwroot/uploads`), served back at `PhotoStorageOptions.RequestPathPrefix` (default `/uploads`). Validates content type (only `image/jpeg`, `image/png`, `image/gif`) and a 5 MB max size (`MaxFileSizeBytes`); IO/permission failures are caught and returned as `Result<Photo>.Failure(...)`, never thrown, so `RecipeService.CreateAsync` can treat them as non-fatal. `DeletePhotoFile` is a best-effort file delete that swallows `IOException`/`UnauthorizedAccessException` — the DB row is treated as the source of truth, not the file. |
| `PhotoStorageOptions` | Options-pattern POCO bound from the `PhotoStorage` config section (`RootPath`, `RequestPathPrefix`). |
| `RecipeValidator` (static) | Validates and normalizes `NewRecipeInput` per spec 001 FR-002..FR-007: required/length-bounded Title, Servings parsed as a positive int, Ingredients require ≥1 non-blank row with a required Item (blank rows silently skipped, per-field max lengths enforced: Item 200 / Unit 50 / Note 300), Steps with whitespace-only entries silently dropped (SC-012) and a 1000-char max. Returns `Result<ValidatedRecipeInput>`. |
| `IngredientFormatter` (static) | Formats an `Ingredient` as a prose line for display, scaling `Quantity` by `targetServings / originalServings` (spec 007 FR-004/007/008) and rounding/trimming the scaled value via `FormatQuantity`. |
| `NewRecipeInput` / `NewIngredientInput` (records) | Raw, string-typed form-binding DTOs for `AddRecipe.razor` (Servings/Quantity are strings here; parsing happens in `RecipeValidator`, not in the DTO). |
| `RecipeSummary` (record) | Lightweight projection for the browse grid: `Id`, `Title`, `PhotoUrl`, `Tags`, `IsFavorite`. `Tags` and `IsFavorite` are now populated from real data by `RecipeService.GetSummariesAsync` (specs 008/011 implemented); this record previously carried a hard-coded-empty `Tags` placeholder, which is no longer the case. |

```
[diagram placeholder: level 2 — RecipeService, TagService, PantryService, and
CookLogService each depend directly on RecipeDbContext (Data/) — there is no
shared repository/facade between them, each owns its own DbContext queries
against overlapping tables (RecipeService and TagService both touch the Tags
table, RecipeService and CookLogService both touch Recipes); RecipeService
additionally depends on IPhotoStorageService; RecipeService, PantryService,
and PhotoStorageService depend on their respective validators/Common.Result;
Components/Pages depend only on service interfaces, never directly on
RecipeDbContext]
```

### Domain — `Domain/`

Plain C# classes, also used directly as EF Core entities (no separate
persistence/DTO mapping layer):

| Type | Fields |
| --- | --- |
| `Recipe` | `Id` (Guid), `Title`, `Servings` (int), `Ingredients` (List<Ingredient>), `Steps` (List<Step>), `PhotoId` (Guid?), `Photo` (Photo?), `Tags` (List<Tag>, new — spec 008), `IsFavorite` (bool, new — spec 011), `CreatedAt`, `UpdatedAt` (DateTime). |
| `Ingredient` | `Id`, `RecipeId` (Guid), `Quantity` (decimal?), `Unit` (string?), `Item` (string, required), `Note` (string?), `DisplayOrder` (int). Structured ingredients (quantity/unit/item/note), not free text — matches spec 001's domain model. |
| `Step` | `Id`, `RecipeId`, `Text` (string), `DisplayOrder` (int). |
| `Photo` | `Id`, `RecipeId`, `Filename`, `MimeType`, `Size` (long), `StoredAt` (string — the servable URL/path), `UploadedAt` (DateTime). |
| `Tag` (new — spec 008) | `Id`, `RecipeId` (Guid), `Name` (string). One row per recipe-tag pair, no shared/global `Tag` row reused across recipes — `ITagService.GetAllTagNamesAsync` derives the distinct global tag vocabulary by `Distinct()`-ing over all rows rather than there being a separate tag dimension table. |
| `PantryItem` (new — spec 012) | `Id`, `Category` (`PantryCategory` enum: `Fridge`, `Freezer`, `Cupboards`), `ItemName` (string), `Quantity` (string — free-text, e.g. "2 lbs", not a structured amount/unit pair like `Ingredient`), `AddedAt`, `UpdatedAt` (DateTime). Standalone entity, not linked to any `Recipe` — no FK relationship exists yet to support "recipes from pantry" matching (specs 013/014). |
| `CookLogEntry` (new — spec 023) | `Id`, `RecipeId` (Guid?, nullable), `CookedAt` (DateTime), `Servings` (int). `RecipeId` is nullable by design: it is set to `null` on recipe deletion (see Persistence) rather than the whole log entry being cascade-deleted, so cook history survives recipe deletion. |

### Persistence — `Data/`

`RecipeDbContext` (EF Core, SQLite via `UseSqlite`) exposes `DbSet<Recipe>`,
`DbSet<Ingredient>`, `DbSet<Step>`, `DbSet<Photo>`, `DbSet<Tag>` (new),
`DbSet<PantryItem>` (new), `DbSet<CookLogEntry>` (new). Configuration in
`OnModelCreating`:
- Cascade delete from `Recipe` to `Ingredients`, `Steps`, `Photo`
  (one-to-one via `HasForeignKey<Photo>(p => p.RecipeId)`), and now also
  `Tags` (`HasMany(r => r.Tags)...OnDelete(DeleteBehavior.Cascade)`).
- `CookLogEntry` is the one exception to cascade delete: its relationship to
  `Recipe` is configured with `DeleteBehavior.SetNull` on `RecipeId`, so
  deleting a recipe nulls out `CookLogEntry.RecipeId` rather than deleting
  the log row — an explicit design choice (commented in code) to preserve
  cook history as an audit trail across recipe deletion (spec 023).
- Max lengths: `Title` 200, `Ingredient.Item` 200, `Ingredient.Unit` 50,
  `Ingredient.Note` 300, `Step.Text` 1000, `Tag.Name` 50 (new),
  `PantryItem.ItemName` 100 (new), `PantryItem.Quantity` 100 (new);
  `Ingredient.Quantity` stored as `decimal(18,2)`; `Recipe.IsFavorite` is
  `IsRequired()` (new).
- `PantryItem` has no foreign-key relationship to `Recipe` — it is
  configured as a fully standalone entity.
- No separate repository/unit-of-work abstraction — `RecipeService`,
  `TagService`, `PantryService`, and `CookLogService` each use
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
| Filter recipes by tag (as opposed to tag CRUD itself, which is implemented) | 015 (filter by tag) | `ITagService`/`TagService` support add/remove/list-all-names, but `IRecipeService.GetSummariesAsync` has no tag filter parameter and `Home.razor` only filters client-side by favourite, not by tag. |
| Pantry-to-recipe matching | 013 (quick entry), 014 (recipes by available ingredients) | `PantryItem` has no FK to `Recipe`/`Ingredient`, and neither `IPantryService` nor `IRecipeService` exposes a "recipes I can make" query. |
| `ImportedRecipeDraft` (or similar) | 002-005 (import from URL/text/photo, review import) | No import pipeline, no draft/staging type; `RecipeService.CreateAsync` only supports direct manual input (`NewRecipeInput`). |
| `CookSession` / cook-mode runtime state | 019 (cook-mode step-by-step), 020 (screen wake lock) | No session/state type; `RecipeDetail.razor` has a "mark as cooked" action and cook-frequency display (spec 023/024 data), but does not drive a step-by-step cook mode. |
| `Timer` | 021 (timer from step) | No timer type or JS interop for timers found. |
| Cook history detail view / full spec 024 | 024 (cook history and frequency) | `CookLogService.GetFrequencyAsync` returns aggregate counts (total/this month/this week/last cooked) consumed by `RecipeDetail.razor`, but there is no dedicated history list/detail UI beyond that summary — TODO: confirm against spec 024's full acceptance criteria whether more is required. |
| Search | 009 (search recipes) | No search method on `IRecipeService`; `GetSummariesAsync` returns all recipes unfiltered. |
| Edit recipe | 010 (edit/delete — delete is implemented via `DeleteAsync`; edit is not) | `IRecipeService` has no `UpdateAsync`/edit method; `AddRecipe.razor` only creates. |

Specs 001 (add recipe manually), 006 (browse recipes grid), 007 (view recipe
detail), 008 (tag recipes), 011 (favourite recipe), 012 (pantry list), and
023 (mark recipe cooked) now have corresponding building blocks realized in
`src/` (`Tag`/`ITagService`/`TagService`, `PantryItem`/`IPantryService`/
`PantryService`, `CookLogEntry`/`ICookLogService`/`CookLogService`,
`Recipe.IsFavorite`, `Pantry.razor`, and the favourite/tag/cook-log UI in
`RecipeDetail.razor` and `Home.razor`). The remaining specs listed above are
still gaps.
