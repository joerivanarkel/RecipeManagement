# 11. Risks and Technical Debt

## Risks

<!-- Known technical risks, ordered by priority -->

| Risk | Probability | Impact | Mitigation |
| ---- | ----------- | ------ | ---------- |
| No EF Core migrations exist; `Program.cs` calls `Database.EnsureCreated()` at startup, which does not evolve an existing schema. Any future change to `Recipe`/`Ingredient`/`Step`/`Photo` has no upgrade path for an existing `recipes.db` file. | Medium — will be hit the next time any entity shape changes | High — risk of crashes or silent data loss against existing local databases | Adopt EF Core Migrations (`dotnet ef migrations add`) before the next schema change, and switch startup to `Database.Migrate()`. |
| Significant spec-vs-implementation gap: `docs/product/story-map.md` defines 24 stories (001-024), covered by 23 spec files in `docs/specs/*.md` (story 018's scope is folded into spec 007); only Stories 001 (add recipe), 006 (browse grid), and 007 (recipe detail) are implemented. None of the "Decide What to Cook Tonight" (pantry, matching, surprise-me) or "Cook It" (cook mode, timers, cook log) epics exist in code. | N/A — current state, not a probabilistic event | Medium — readers of specs/architecture docs could assume capabilities that don't exist yet | Treat specs and story map as roadmap, not "as-built" documentation; keep this section as the explicit pointer to current scope. |
| Persistence is local-machine-only: a single SQLite file plus recipe photos stored on local disk under `wwwroot/uploads`. This matches the product's personal, single-user intent today. | Low, given the current single-user/single-machine scope | High if that assumption ever changes — some PaaS platforms wipe local filesystem state between deploys, and there is no multi-device access path | Document the single-machine assumption explicitly wherever deployment is discussed; if remote or multi-device access is ever required, plan a move to hosted DB + blob/object storage before deploying to ephemeral infrastructure. |
| Photo upload validation (5MB size limit, allowed content types `image/jpeg`/`image/png`/`image/gif`) is defined independently in two places: `AddRecipe.razor` (`MaxPhotoSizeBytes`, `AllowedPhotoContentTypes`) and `PhotoStorageService` (`MaxFileSizeBytes`, `AllowedContentTypes`). Values currently match but there is no shared source. | Medium — likely to drift if one side is updated without the other (e.g. adding a new supported format) | Low-to-Medium — today it only risks inconsistent error messages, not a correctness bug | Extract a single shared constant/options source for allowed types and max size, consumed by both the Razor component and the service. |

## Technical Debt

<!-- Known technical debt items -->

| Item | Description | Effort | Priority |
| ---- | ----------- | ------ | -------- |
| No CI/CD pipeline | No `.github/workflows/`, `Dockerfile`, or `docker-compose.yml` exist anywhere in the repo. The xUnit suite in `RecipeManagementWebApp.Tests` (covering `RecipeValidator`, `PhotoStorageService`, `IngredientFormatter`, `RecipeService`) only runs when a developer remembers to run `dotnet test` locally — nothing runs automatically on push or PR. | Small–Medium | Medium |
| No component-level (Blazor/bUnit) tests | Unit tests cover the service layer only. `Home.razor`, `AddRecipe.razor`, and `RecipeDetail.razor` have zero automated coverage — e.g. `AddRecipe.razor`'s dirty-state tracking, navigation-lock confirmation, and photo-selection validation are only verified manually. | Medium | Medium |
| Unused starter-kit template pages | `Counter.razor` and `Weather.razor` under `Components/Pages/` are leftover ASP.NET Core Blazor template scaffolding, not part of the product (not in the story map, not linked from nav in any product flow). Pure clutter. | Trivial | Low |
| `Tags` field on `RecipeSummary` is not backed by persisted data | `RecipeSummary.Tags` is hardcoded to `Array.Empty<string>()` in `RecipeService.GetSummariesAsync()`; the DTO's own doc comment notes this is a placeholder until Story 008 adds tagging. No `Tag` entity exists in `RecipeDbContext` or `Domain/`. Known and intentional today, but a trap for anyone extending browse/filter features without noticing the gap. | Medium (new entity + mapping + a migration, once migrations exist) | Low until Story 008 is scheduled |
