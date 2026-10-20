# 12. Glossary

<!-- Domain and technical terms used throughout the documentation -->

Terms are drawn from `docs/product/story-map.md` and the "Domain Model" section (§5) of each `docs/specs/*.md` feature spec, using the project's own wording wherever possible. The **Status** column distinguishes terms that are real classes/tables in `src/RecipeManagementWebApp/Domain/` or `Data/RecipeDbContext.cs` today from terms that exist only as specified intent in `docs/specs/`.

| Term | Definition | Status |
| ---- | ---------- | ------ |
| Recipe | The core aggregate root: a single recipe a cook has added to their collection, with title, servings, ingredients, steps, an optional photo, and timestamps. | Implemented (`Domain/Recipe.cs`, `RecipeDbContext`) |
| Ingredient | A single ingredient within a recipe, structured as quantity (optional), unit (optional), item (required), and note (optional) so it can unlock fridge matching (Story 014) and scaling (Story 018). | Implemented (`Domain/Ingredient.cs`, `RecipeDbContext`) |
| Step | A single free-text cooking instruction belonging to a recipe, kept simple today but extensible for future metadata (e.g., detected durations, Story 021). | Implemented (`Domain/Step.cs`, `RecipeDbContext`) |
| Photo | An optional photo associated with a recipe (filename, MIME type, storage reference, upload timestamp). | Implemented (`Domain/Photo.cs`, `RecipeDbContext`) |
| Tag | A single free-text label applied to a recipe (e.g., "vegetarian", "quick"), stored lowercase/normalized, recipe-scoped with no shared global tag entity or predefined taxonomy. | Specified only (spec 008; displayed in spec 006, matched in specs 014/015) |
| PantryItem | A single item in the cook's pantry — category (Fridge/Freezer/Cupboards), item name, and a free-text human-readable quantity — used to drive the "what can I cook" ranking (Story 014). | Specified only (spec 012) |
| QuickEntryIngredient | A transient, never-persisted ingredient name typed by the cook (e.g., "chicken, cabbage, rice") that is unioned with PantryItems for a single availability calculation, then discarded. | Specified only (spec 013) |
| RecipeAvailability | The ranked-matching result for a recipe against the pantry: total ingredient count, available/missing ingredient lists, and a percentage-available score. | Specified only (spec 014) |
| SearchQuery | A value object wrapping the raw and normalized (trimmed, lowercased) search text used to match recipes by title or ingredient. | Specified only (spec 009) |
| ImportedRecipeDraft | An in-progress, unconfirmed recipe extracted from an external source (URL, pasted text, or photo via OCR); not a Recipe until confirmed and saved (Story 005), and discarded otherwise. | Specified only (specs 002, 003, 004, 005) |
| CookSession | The in-memory state of one pass through cook mode for a recipe: session servings, current step index, start/completion timestamps, and (per spec 022) the set of checked-off ingredient IDs. | Specified only (specs 019, 020, 021, 022) |
| Timer | A client-side, session-only countdown started from a step's detected duration (e.g., "simmer 20 min"), never persisted; multiple timers can run concurrently within a CookSession. | Specified only (spec 021) |
| ScaledQuantity | A value object representing an ingredient quantity (decimal, range, or fraction) recalculated for a new serving size; scaling is display-only and never persisted to the Recipe. | Specified only (spec 007) |
| CookLogEntry | An immutable, append-only record of a single instance of cooking a recipe (recipe ID, `cookedAt` timestamp, servings prepared), forming the audit trail behind "last cooked" and frequency features. | Specified only (spec 023; read by specs 016, 017, 024) |
| CookHistorySummary | A derived, read-only aggregation over a recipe's CookLogEntries: the ordered entry list plus total/this-month/this-week counts, computed using calendar-based (not rolling) boundaries. | Specified only (spec 024) |

## Other Specified-Only Value Objects

These narrower value objects appear in individual specs' Domain Model sections and are included for completeness, though they are not in the "must include" list above.

| Term | Definition | Status |
| ---- | ---------- | ------ |
| IngredientMatch | A per-ingredient result of matching a recipe's ingredient against the pantry: the recipe ingredient name, the matching PantryItem (if any), and an `isAvailable` flag. | Specified only (spec 014) |
| TagFilterSelection | The set of currently selected tag names used to narrow a RecipeAvailability result set with AND semantics; session/page-level only, not a saved preference. | Specified only (spec 015) |
| SurpriseCandidatePool | The derived pool of recipes eligible for "surprise me" random selection after recently-cooked recipes are excluded (default 7-day window), with a fallback flag if exclusion emptied the pool. | Specified only (spec 016) |
| RelativeTimeLabel | A value object pairing a raw `lastCookedAt` timestamp (or null) with its human-readable relative display text (e.g., "3 days ago", "Never cooked"). | Specified only (spec 017) |
| DetectedDuration | The result of pattern-based (not NLP) parsing of a step's text for a time duration, yielding the matched raw text and a duration in seconds, used to offer a Timer. | Specified only (spec 021) |
| CookFrequency | A value object summarizing how often a recipe has been cooked: total count, last-cooked timestamp and relative label, plus this-month/this-week counts. | Specified only (spec 023) |

## Ambiguities and Notes

- **CookSession vs. persistence**: Specs 019–022 describe CookSession as in-memory/session state with no persistence, but spec 019's table also lists `id`, `startedAt`, and `completedAt` as if it were a durable entity. Treat it as ephemeral/session-scoped per the explicit domain rules in specs 020 and 022, not as a persisted aggregate, unless a future spec revises this.
- **Edit/delete semantics (spec 010)**: The Domain Model notes an optional `isDeleted` flag but explicitly leaves "soft or hard delete" as an open question — the glossary entry for Recipe does not assume soft deletes are implemented.
- **RecipeAvailability vs. IngredientMatch**: Spec 014 defines both a recipe-level result (`RecipeAvailability`) and a per-ingredient result (`IngredientMatch`); the two are easy to conflate and are listed separately above.
- **Product-level activity names** ("Capture Recipes," "Build the Collection," "Decide What to Cook Tonight," "Cook It") from `docs/product/story-map.md` are organizing labels for groups of stories, not domain entities, and are intentionally omitted from the entity table above.
