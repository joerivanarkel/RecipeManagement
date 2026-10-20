# 6. Runtime View

<!-- Key runtime scenarios showing how building blocks interact -->

## Scenario 1: Browse Recipes (`/`)

<!-- Home.razor — the default landing page -->

```
Browser (active Blazor Server SignalR circuit)
  → GET "/" renders Home.razor (@rendermode InteractiveServer)
  → Home.razor.OnInitializedAsync()
  → IRecipeService.GetSummariesAsync()
  → RecipeDbContext.Recipes, ordered by CreatedAt descending
  → projected directly into RecipeSummary DTOs (Id, Title, PhotoUrl, Tags)
  → Home.razor renders a responsive Bootstrap card grid:
      - photo (Photo.StoredAt) or a "No Photo" placeholder
      - title
      - tag badges with "+N more" truncation (SplitTags)
  → if there are zero recipes: empty-state message + link to /recipes/add
  → clicking a card navigates (same circuit) to /recipes/{id} — see Scenario 3
```

**Gap:** `RecipeService.GetSummariesAsync()` currently hardcodes
`Tags: Array.Empty<string>()` for every recipe. There is no Tag entity or
persistence wired up yet (Story 008 is specified but not implemented), even
though `Home.razor` already contains working UI logic to render tags if they
were populated. Treat the tag badges in the UI today as effectively a no-op.

## Scenario 2: Add a Recipe (`/recipes/add`)

<!-- AddRecipe.razor — recipe creation form -->

```
Browser (same circuit)
  → GET "/recipes/add" renders AddRecipe.razor
  → user fills Title, Servings, dynamic Ingredient rows
    (Quantity/Unit/Item/Note), dynamic Step rows, optional Photo (InputFile)
  → NavigationLock + _isDirty tracking + JS interop (recipeApp.confirmLeave)
    guards against losing unsaved changes on internal navigation
  → photo file (if any) is validated client-side in OnPhotoSelected:
      content-type must be jpeg/png/gif, size must be <= 5MB
      (rejected inline, no server round trip, if invalid)
  → user clicks "Save Recipe" → SaveAsync():
      1. builds NewRecipeInput from the raw form fields/rows
      2. wraps the selected photo (if any) in a PhotoUploadRequest
      3. calls IRecipeService.CreateAsync(input, photoRequest)
  → RecipeService.CreateAsync:
      1. RecipeValidator.Validate(input) — on failure, returns
         Result<CreateRecipeOutcome>.Failure(errors); NO database write occurs
      2. on success: builds a new Recipe aggregate (new Guid id,
         CreatedAt/UpdatedAt = UtcNow) with child Ingredient/Step entities
         (DisplayOrder preserved from list order)
      3. if a photo was supplied: IPhotoStorageService.SavePhotoAsync(...)
         persists the file under wwwroot/uploads (PhotoStorageOptions);
         a storage failure here does NOT fail the recipe save — it is
         downgraded to a non-fatal PhotoWarning (FR-013)
      4. dbContext.Recipes.Add(recipe) + SaveChangesAsync (single call,
         cascade-inserts Ingredients/Steps/Photo)
      5. returns Result<CreateRecipeOutcome>.Success(...)
  → AddRecipe.razor renders the outcome:
      - success: success banner + link to "/", form reset, any PhotoWarning
        shown as a separate warning banner
      - validation failure: error list rendered as an alert
      - unhandled exception: caught, shown as a generic
        "Failed to save recipe. Please try again." message
```

## Scenario 3: View Recipe Detail (`/recipes/{id}`)

<!-- RecipeDetail.razor — single-recipe view, servings scaling, delete -->

```
Browser (same circuit)
  → navigates to /recipes/{Id:guid} (e.g. from a card on Scenario 1)
  → RecipeDetail.razor.OnParametersSetAsync()
  → IRecipeService.GetByIdAsync(Id)
  → RecipeDbContext.Recipes, eager-loaded:
      Ingredients (ordered by DisplayOrder), Steps (ordered by DisplayOrder),
      Photo
  → if null: render "Recipe not found" view with a link back to "/"
  → if found:
      - photo (or placeholder) and title render
      - servings slider (_selectedServings, bounded 1..max(20, Servings))
        rescales ingredient display text client-side only, via
        IngredientFormatter.FormatLine(ingredient, recipe.Servings,
        _selectedServings) — recomputed per slider move within the same
        circuit; NO new service/DB call per adjustment
      - ingredients render as a prose-formatted list (IngredientFormatter)
      - steps render as an ordered list, or "No steps added" if empty
      - "Start Cooking" / "Edit Recipe" buttons are present but DISABLED
        ("Coming soon") — UI placeholders for specs not yet implemented
        (cook mode, recipe editing); they trigger no backend calls
      - "Delete Recipe":
          1. JS confirm dialog (recipeApp.confirmLeave) gates the action
          2. on confirm: IRecipeService.DeleteAsync(Id) removes the Recipe
             row; cascade delete (configured in RecipeDbContext) removes its
             Ingredients/Steps/Photo row; IPhotoStorageService
             .DeletePhotoFile removes the stored photo file from disk
          3. on success: NavigationManager.NavigateTo("/")
          4. on exception: caught, shown inline, recipe left intact
```

## Cross-Cutting Concern: Persistent Blazor Server Circuit

Every scenario above executes over a single, long-lived Blazor Server
SignalR connection ("circuit") established when the browser first loads a
page — not a stateless HTTP request/response per interaction. Component
methods (`OnInitializedAsync`, button click handlers, slider `oninput`,
etc.) run server-side, and only DOM patches stream back over the socket.

`ReconnectModal.razor` is the UI shown whenever that circuit drops (network
blip, idle timeout, app restart, etc.) while Blazor attempts to reconnect;
until reconnection succeeds, the page is inert. This applies uniformly to
browsing, adding, and viewing/deleting recipes: none of them function
without an active circuit, and a circuit loss mid-interaction (e.g. while
filling in the Add Recipe form) can lose any unsaved, not-yet-persisted form
state even though `NavigationLock`/`_isDirty` protects against accidental
in-app navigation.

## Error/Edge Case Scenarios

- **Add Recipe validation failure**: `RecipeValidator.Validate` rejects the
  input (e.g. missing title, no ingredients) before any database write;
  errors are returned as a string list via `Result<T>.Failure` and rendered
  as an alert list in `AddRecipe.razor`. No partial recipe is ever persisted.
- **Optional photo upload failure**: invalid content-type or oversized files
  (>5MB) are rejected client-side before submit; a server-side storage
  failure during `SavePhotoAsync` does not fail the whole recipe save — the
  recipe is still created, and a `PhotoWarning` is surfaced as a separate
  warning banner (FR-013).
- **Unexpected exception on save**: caught in `AddRecipe.razor`'s
  `SaveAsync`, shown as a generic "Failed to save recipe. Please try again."
  — no raw exception detail is exposed to the user.
- **Recipe not found**: `RecipeDetail.razor` handles a null
  `GetByIdAsync` result (e.g. deleted or mistyped id) by rendering a
  dedicated "Recipe not found" view rather than throwing.
- **Delete failure**: caught in `RecipeDetail.razor`'s `DeleteAsync`, shown
  inline ("Failed to delete recipe. Please try again.") without navigating
  away; the recipe remains intact.
- **Unsaved changes navigation guard**: `AddRecipe.razor` uses
  `NavigationLock` plus a JS-interop confirm dialog to prevent accidental
  loss of in-progress form data on *internal* navigation (does not cover
  closing the browser tab or external navigation beyond the standard browser
  prompt, which Blazor does not control).
- **Dropped SignalR circuit**: handled uniformly by `ReconnectModal.razor`
  (see Cross-Cutting Concern above) — not scenario-specific, but affects the
  resilience of every scenario above.
- **TODO / open question**: no server-side re-validation of the
  servings-slider bounds is visible beyond the `min`/`max` on the HTML range
  input, and no concurrency handling (e.g. optimistic concurrency tokens) is
  evidenced for the case where two circuits edit/delete the same recipe at
  the same time. Flagging as an open item rather than inventing a described
  mitigation that doesn't exist in the code.
