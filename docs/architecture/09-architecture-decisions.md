# 9. Architecture Decisions

<!-- Important, expensive, large-scale, or risky architecture decisions.
     Individual ADRs are stored in docs/architecture/decisions/ -->

## Decision Log

| ID     | Decision                                                      | Status   | Date       |
| ------ | -------------------------------------------------------------| -------- | ---------- |
| ADR-01 | Blazor Server as the UI technology                            | Accepted | Undated¹   |
| ADR-02 | SQLite + `EnsureCreated()` (no EF Core Migrations)             | Accepted | Undated¹   |
| ADR-03 | Local filesystem photo storage (not cloud blob storage)        | Accepted | Undated¹   |
| ADR-04 | `Result<T>` for error/validation signaling instead of exceptions | Accepted | Undated¹ |
| ADR-05 | Structured ingredients (quantity/unit/item/note) vs. free text  | Accepted | Undated¹   |
| ADR-06 | Tags as per-recipe rows (owned 1:many), not a shared Tag catalog | Accepted | Undated¹   |
| ADR-07 | Cook log as an immutable audit trail, decoupled from Recipe via nullable FK + `SetNull` | Accepted | Undated¹ |

¹ No dated commit or design doc records when these decisions were made;
  git history for this repo is scaffolding-only (see below). Dates are
  omitted rather than invented.

> **Note on provenance:** This project's git history (`init`, `starter
> pack`, `CLAUDE.md init`, `src folder`, `Add files via upload`, `mcp
> json`) does not contain feature-by-feature commits with decision
> rationale. The entries below are reconstructed from the current
> implementation and `docs/specs/`, not from a recorded decision log. Where
> rationale isn't evidenced, this is stated explicitly rather than
> invented.
>
> Also note: this file and its table historically pointed to a
> `docs/architecture/decisions/` / `adr/` folder for full ADR writeups.
> That folder does not exist in the repository today. Rather than
> fabricate a set of standalone ADR files to fill the gap, the full
> lightweight records are kept inline below until/unless individual ADR
> files are actually created.

### ADR-01: Blazor Server as the UI technology

- **Context:** `Program.cs` registers `AddRazorComponents()` with
  `.AddInteractiveServerComponents()` and maps components with
  `.AddInteractiveServerRenderMode()`. There is no WebAssembly hosting
  model, no SPA frontend framework (React/Angular/etc.), and no separate
  API project — the app is a single ASP.NET Core project rendering
  interactive UI via server-side Blazor over a persistent circuit.
- **Decision:** Use Blazor Server (interactive server-side rendering) as
  the sole UI technology.
- **Consequences:** Simpler deployment (single process, no separate API/
  client build); state and C# logic stay server-side; requires a
  persistent connection (SignalR-based circuit) between browser and
  server, which is consistent with the project's single-user, locally-run
  usage model described in `docs/product/story-map.md`.
- **Rationale not documented**; decision inferred from current
  implementation.

### ADR-02: SQLite with `Database.EnsureCreated()`, no EF Core Migrations

- **Context:** `Program.cs` configures `RecipeDbContext` with
  `UseSqlite(connectionString)`, where the connection string is required
  from configuration (throws if missing). At startup, the app calls
  `Database.EnsureCreated()` inside a service scope. No `Migrations/`
  folder or migration classes exist in the project.
- **Decision:** Use SQLite as the database engine, and create the schema
  via `EnsureCreated()` rather than EF Core Migrations.
- **Consequences:** Fits a single-user, local-file deployment model (no
  separate database server to provision); schema changes currently
  require either dropping/recreating the SQLite file or manually evolving
  it, since `EnsureCreated()` and Migrations are mutually exclusive in EF
  Core — this is a known limitation for future schema evolution and is
  also tracked as a risk in Section 11 (Risks & Technical Debt).
- **Rationale not documented**; decision inferred from current
  implementation. No commit message or doc explains why Migrations were
  skipped in favor of `EnsureCreated()`.

### ADR-03: Local filesystem photo storage (not cloud blob storage)

- **Context:** `PhotoStorageService` writes uploaded photo files directly
  to a configured local directory (`PhotoStorageOptions.RootPath`) using
  `Directory.CreateDirectory` / `File.Create` / `File.Delete`, and serves
  them back under a configured request path prefix. There is no
  integration with any cloud blob/object storage SDK (e.g., Azure Blob
  Storage, S3) anywhere in the codebase.
- **Decision:** Store recipe photos on the local filesystem under
  `wwwroot/uploads` (or the configured root), not in cloud storage.
- **Consequences:** Works without external dependencies or cloud
  credentials, matching the single-user/local-deployment model; photos
  are not available if the app is moved to a different host/container
  without also moving the storage directory; no CDN/edge caching benefits
  that cloud blob storage would provide.
- **Rationale not documented**; decision inferred from current
  implementation.

### ADR-04: `Result<T>` as the error/validation signaling pattern

- **Context:** `Common/Result.cs` defines a generic `Result<T>` with
  `Succeeded`, `Value`, and `Errors`, with `Success`/`Failure` factory
  methods. Services such as `PhotoStorageService.SavePhotoAsync`,
  `RecipeService`, and `RecipeValidator` return `Result<T>` for expected
  failure cases (e.g., invalid file format, file too large, storage I/O
  errors, validation failures) rather than throwing exceptions for those
  cases.
- **Decision:** Use a `Result<T>` success/failure wrapper as the primary
  mechanism for signaling expected failures (validation errors, storage
  failures) across service boundaries, reserving exceptions for truly
  exceptional/unexpected conditions.
- **Consequences:** Callers (Razor components) must check `Succeeded`
  and handle `Errors` explicitly rather than relying on try/catch for
  expected validation failures; keeps control flow for predictable
  failure cases (bad upload, validation errors) explicit and visible in
  method signatures.
- **Rationale not documented**; decision inferred from current
  implementation.

### ADR-05: Structured ingredients (quantity/unit/item/note) instead of free text

- **Context:** `Domain/Ingredient.cs` models an ingredient as discrete
  fields — `Quantity` (`decimal?`), `Unit` (`string?`), `Item`
  (`string`, required), `Note` (`string?`), plus `DisplayOrder` — rather
  than a single free-text ingredient line (e.g. "2 cups flour, sifted").
  `docs/specs/001-add-recipe-manually.md` reflects the same structured
  shape at the spec level, and `Services/IngredientFormatter.cs` exists
  specifically to re-render these structured fields back into a readable
  line for display.
- **Decision:** Represent each recipe ingredient as structured data
  (quantity, unit, item, note) rather than a single free-text string.
- **Consequences:** More implementation effort for input/display (needs a
  formatter to reconstitute a human-readable line, more form fields in
  `AddRecipe.razor`) in exchange for machine-usable data; this is
  foundational for later, not-yet-built features like
  ingredient-availability/pantry matching (spec 014), since free-text
  ingredients would not support that without separate NLP parsing.
- **Rationale not documented explicitly as a design doc**, but this is the
  one decision in this list with the strongest corroborating evidence
  beyond the code itself — it is reflected consistently in both the
  domain model and the spec (`docs/specs/001-add-recipe-manually.md`),
  suggesting it was a deliberate choice rather than an accidental default.

### ADR-06: Tags modeled as per-recipe rows, not a shared tag catalog

- **Context:** `Domain/Tag.cs` is a simple entity with `Id`, `RecipeId`,
  and `Name` — there is no unique constraint or lookup table for tag
  names, and no join table. `RecipeDbContext.OnModelCreating` wires
  `Recipe.Tags` via `HasMany(r => r.Tags).WithOne().HasForeignKey(t =>
  t.RecipeId).OnDelete(DeleteBehavior.Cascade)`, i.e. a plain one-to-many
  from `Recipe` to `Tag`, cascade-deleted with the recipe. This matches
  `docs/specs/008-tag-recipes.md`, which describes free-form, per-recipe
  tagging rather than a shared/reusable tag vocabulary.
- **Decision:** Represent tags as rows owned by a single recipe (one tag
  row per recipe-tag pairing, duplicated by name across recipes) instead
  of a shared `Tag` catalog with a many-to-many join table.
- **Consequences:** Much simpler schema and querying (no join table, no
  dedup/merge logic for tag names); tags cannot be renamed or managed
  globally across recipes (renaming a tag means editing it on every
  recipe it appears on), and there's no built-in protection against
  near-duplicate tag names (e.g. "Vegan" vs "vegan") across recipes; tags
  are correctly removed when their owning recipe is deleted via cascade.
- **Rationale not documented**; decision inferred from current
  implementation and spec 008.

### ADR-07: Cook log as an immutable audit trail, decoupled from Recipe

- **Context:** `Domain/CookLogEntry.cs` models `RecipeId` as nullable
  (`Guid?`) rather than required, and is its own top-level entity (not
  nested under `Recipe`), tracking `CookedAt` and `Servings`. In
  `RecipeDbContext.OnModelCreating`, the relationship is configured as
  `entity.HasOne<Recipe>().WithMany().HasForeignKey(c =>
  c.RecipeId).OnDelete(DeleteBehavior.SetNull)`, with an explicit code
  comment: *"Cook log is an immutable audit trail ... so recipe deletion
  must not cascade-delete history — RecipeId is nullable with SetNull
  instead."* This aligns with `docs/specs/023-mark-recipe-cooked.md`.
- **Decision:** Treat cook log entries as a standalone, append-only
  history independent of the `Recipe` aggregate's lifecycle: deleting a
  recipe detaches its cook log entries (`RecipeId` set to `null`) instead
  of deleting them.
- **Consequences:** Cook history survives recipe deletion, satisfying the
  "preserve history" intent in spec 023; orphaned cook log entries (with
  `RecipeId == null`) must be handled explicitly by any UI/service code
  that reads the log (e.g. showing "deleted recipe" instead of a title);
  this is a deliberate deviation from the cascade-delete pattern used
  elsewhere in the same `DbContext` (Ingredients, Steps, Photo, Tags all
  cascade with their recipe).
- **Rationale is documented inline in code** (`RecipeDbContext.cs`
  comment above the `CookLogEntry` mapping), unlike most other entries in
  this log — this is the clearest evidenced decision in this set.
