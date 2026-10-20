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
      - a "★" favourite marker overlay (recipe-card-favorite) if
        recipe.IsFavorite
      - photo (Photo.StoredAt) or a "No Photo" placeholder
      - title
      - tag badges (recipe.Tags, now populated from the Tag entity via
        r.Tags.Select(t => t.Name) in GetSummariesAsync) with "+N more"
        truncation (SplitTags)
  → a "Favourites only" toggle (_favouritesOnly, bound client-side in the
    same circuit) filters _visibleRecipes to recipe.IsFavorite without any
    extra service/DB call; if the toggle is on and no favourites exist, a
    dedicated empty-state message is shown instead of the grid
  → if there are zero recipes at all: empty-state message + link to
    /recipes/add
  → clicking a card navigates (same circuit) to /recipes/{id} — see Scenario 3
```

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

## Scenario 4: Toggle Favourite (`/recipes/{id}`)

<!-- RecipeDetail.razor — star button next to the title -->

```
Browser (same circuit, on RecipeDetail.razor)
  → user clicks the ★/☆ button next to the title → ToggleFavoriteAsync()
  → IRecipeService.ToggleFavoriteAsync(Id):
      1. loads the Recipe by id
      2. if not found: returns null
      3. if found: flips IsFavorite, SaveChangesAsync, returns the new
         bool state
  → RecipeDetail.razor:
      - on a non-null result: updates _recipe.IsFavorite locally (no
        reload of the whole recipe) so the star icon/colour flips
        immediately
      - on null: shows "Recipe no longer exists." in _favoriteError
  → next visit to "/" reflects the new IsFavorite value via
    GetSummariesAsync (Scenario 1), including the "Favourites only" filter
```

## Scenario 5: Tag a Recipe (`/recipes/{id}`)

<!-- RecipeDetail.razor — "Add Tag" input with suggestions, and per-tag remove button -->

```
Browser (same circuit, on RecipeDetail.razor)
  → on OnParametersSetAsync, ITagService.GetAllTagNamesAsync() preloads
    _allTagNames (distinct tag names across all recipes, sorted) to power
    inline autocomplete suggestions
  → user types into the "Add Tag" input:
      - TagSuggestions filters _allTagNames client-side (case-insensitive
        contains match, excludes tags already on this recipe, capped at 5)
        — no server round trip per keystroke
      - clicking a suggestion (SelectSuggestion) just fills the input
  → user clicks "Add Tag" → AddTagAsync():
      → ITagService.AddTagAsync(Id, name):
          1. loads the Recipe with its Tags collection; if not found,
             returns null
          2. normalizes the name (trim + ToLowerInvariant)
          3. validates: non-empty, <= 50 chars (TagService.NameMaxLength)
          4. if the normalized name already exists on this recipe: no-op,
             returns the current tag name list (idempotent, no duplicate
             Tag row)
          5. otherwise: creates a new Tag entity (new Guid, RecipeId),
             SaveChangesAsync
      → on null: "Recipe no longer exists." shown in _tagError
      → on validation failure: first error message shown in _tagError
      → on success: re-fetches the full Recipe (IRecipeService.GetByIdAsync)
        and the updated _allTagNames, clears the input
  → user clicks the "x" on a tag badge → RemoveTagAsync(tagId):
      → ITagService.RemoveTagAsync(Id, tagId) deletes the Tag row scoped to
        both recipeId and tagId (returns false if no match)
      → on success: removes the tag from the in-memory _recipe.Tags list
        (no full reload) and refreshes _allTagNames
```

## Scenario 6: Manage Pantry Items (`/pantry`)

<!-- Pantry.razor — add/edit/delete PantryItem entities, grouped by category -->

```
Browser (active circuit)
  → GET "/pantry" renders Pantry.razor (@rendermode InteractiveServer)
  → Pantry.razor.OnInitializedAsync() → IPantryService.GetAllAsync()
  → RecipeDbContext.PantryItems ordered by Category then AddedAt
  → if empty: "Your pantry is empty..." empty-state message
  → otherwise: items are grouped client-side by Category (GroupBy,
    ordered by category) into a heading + list-group per category

Add an item:
  → user selects a PantryCategory, types an item name and free-text
    quantity (e.g. "2 lbs"), clicks "Add to Pantry" → AddItemAsync()
      1. builds a NewPantryItemInput(_category, _itemName, _quantity)
      2. IPantryService.AddAsync(input):
           - PantryItemValidator.Validate: trims and requires non-empty
             item name and quantity, each <= 100 chars; on failure returns
             Result<PantryItem>.Failure(errors) — no DB write
           - on success: builds a new PantryItem (new Guid,
             AddedAt/UpdatedAt = UtcNow), SaveChangesAsync
      3. on failure: errors rendered as an alert list, form fields kept
      4. on success: form fields cleared, full item list re-fetched

Edit a quantity (inline, no navigation):
  → "Edit" button switches that list item into an inline input
    (_editingId / _editQuantity) → "Save" → SaveQuantityAsync(id):
      → IPantryService.UpdateQuantityAsync(id, quantity):
          - returns null if the item no longer exists
          - trims and re-validates length (required, <= 100 chars)
            independently of PantryItemValidator (duplicated bounds
            check, not shared validator code — TODO: consider unifying)
          - on success: updates Quantity + UpdatedAt, SaveChangesAsync
      → on a successful, non-null result: full item list re-fetched;
        inline edit state is cleared either way (success or failure)

Delete an item:
  → "Delete" button → DeleteItemAsync(id) → IPantryService.DeleteAsync(id)
    removes the row directly (no confirm dialog, unlike recipe delete in
    Scenario 3) → full item list re-fetched
```

**TODO / open question:** no UI surfaces pantry-driven recipe suggestions
yet, even though the empty-state copy ("Add items to get recipe
suggestions") implies that capability — not evidenced in `PantryService` or
`Pantry.razor` today.

## Scenario 7: Mark a Recipe as Cooked (`/recipes/{id}`)

<!-- RecipeDetail.razor — "Mark as Cooked" button and cook-frequency summary -->

```
Browser (same circuit, on RecipeDetail.razor)
  → on OnParametersSetAsync, ICookLogService.GetFrequencyAsync(Id) loads
    all CookLogEntry.CookedAt timestamps for the recipe and computes a
    CookFrequency(TotalCount, LastCookedAt, FrequencyThisMonth,
    FrequencyThisWeek) summary, rendered as muted text under the servings
    slider (e.g. "Cooked 3 times · Last cooked yesterday", or "Never
    cooked" if TotalCount == 0)
  → user clicks "Mark as Cooked" → MarkAsCookedAsync():
      1. JS confirm dialog (recipeApp.confirmLeave, reused from the delete
         flow) gates the action — cancel aborts with no service call
      2. on confirm: ICookLogService.RecordCookAsync(Id, _selectedServings)
           - returns null if the recipe no longer exists
           - if _selectedServings <= 0 (e.g. a corrupted/invalid slider
             value), falls back to the recipe's own Servings (EC-006)
           - creates a new CookLogEntry (new Guid, CookedAt = UtcNow,
             Servings = effective value), SaveChangesAsync
      3. on a non-null result: success banner "Marked as cooked. Enjoy!"
         and _cookFrequency is recomputed via GetFrequencyAsync
      4. on null: "Could not record cook — recipe no longer exists." shown
         in the same banner area, no frequency refresh
  → "Start Cooking" remains a disabled "Coming soon" placeholder (cook
    mode itself, spec 019, is not implemented) — recording a cook event
    today is a standalone action independent of any guided cook-mode flow
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
browsing, adding, viewing/deleting recipes, favouriting, tagging, pantry
management, and cook logging: none of them function without an active
circuit, and a circuit loss mid-interaction (e.g. while filling in the Add
Recipe form, or mid-edit of a pantry quantity) can lose any unsaved,
not-yet-persisted form state even though `NavigationLock`/`_isDirty`
protects against accidental in-app navigation (note: the favourite, tag,
pantry, and cook-log flows do not use `NavigationLock`/`_isDirty` — only
`AddRecipe.razor` does).

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
- **Favourite toggle on a deleted recipe**: `RecipeService.ToggleFavoriteAsync`
  returns `null` if the recipe id no longer exists (e.g. deleted in another
  circuit); `RecipeDetail.razor` surfaces "Recipe no longer exists." in
  `_favoriteError` rather than throwing.
- **Tag validation / duplicate / missing-recipe failures**: `TagService
  .AddTagAsync` returns `null` for a deleted recipe (shown as "Recipe no
  longer exists."), a `Result.Failure` for empty or >50-character tag names
  (first error shown inline), and silently no-ops (returns the existing tag
  list, no new `Tag` row) for a name that already exists on the recipe
  case-insensitively post-normalization.
- **Pantry item validation failure**: `PantryItemValidator.Validate` rejects
  empty or >100-character item name/quantity before any database write;
  errors are rendered as an alert list in `Pantry.razor` and the typed-in
  form values are preserved (not cleared) so the user can correct them.
- **Pantry quantity edit on a deleted item**: `PantryService
  .UpdateQuantityAsync` returns `null` if the item id no longer exists;
  `Pantry.razor` just exits inline-edit mode without refreshing the list or
  showing an error — this is a silent no-op, not an explicit error message
  (TODO: confirm this is the intended UX).
- **Cook log on a deleted recipe**: `CookLogService.RecordCookAsync` returns
  `null` if the recipe id no longer exists; `RecipeDetail.razor` shows
  "Could not record cook — recipe no longer exists." instead of a success
  banner, and does not refresh `_cookFrequency`.
- **Invalid servings value on cook log**: if `_selectedServings` is `<= 0`
  when "Mark as Cooked" is invoked, `CookLogService.RecordCookAsync` falls
  back to the recipe's own `Servings` value rather than persisting a
  zero/negative count (EC-006).
