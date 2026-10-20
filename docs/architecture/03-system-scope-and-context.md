# 3. System Scope and Context

## Business Context

<!-- Communication partners and the data exchanged with the system from a business perspective -->

RecipeManagement is a self-contained, single-user application. As implemented today, there is exactly one communication partner: the home cook who uses the app directly. There are **no external third-party systems wired into the running application** — no external recipe/content APIs, no authentication/identity providers, no email or notification services, no cloud storage, no payment systems. The app's own local SQLite database file and local filesystem (for uploaded photos) are internal implementation details of the system, not external partners.

The domain has recently grown to include tagging (`Tag`), a pantry list (`PantryItem`), and a cook log (`CookLogEntry`), alongside the existing recipe model's new `IsFavorite`/`Tags` properties (see `docs/specs/008-tag-recipes.md`, `011-favourite-recipe.md`, `012-pantry-list.md`, `023-mark-recipe-cooked.md`). These are additional business capabilities for the same single actor (the Home Cook) — favouriting, tagging, maintaining a pantry, and marking recipes as cooked are all local record-keeping features within the existing single-user scope. They do not introduce a second actor or any external communication partner.

```
[diagram placeholder: business context — single actor "Home Cook" interacting directly with the RecipeManagement system; no external systems]
```

| Communication Partner | Inputs | Outputs |
| ---------------------- | ------ | ------- |
| Home Cook (single human actor, via browser) | Recipe details entered through forms (title, servings, ingredients, steps), uploaded photos, browse/search/view requests, tag assignments, favourite toggles, pantry item entries, "mark as cooked" actions | Rendered recipe grid and recipe detail pages (including favourites-only filter and tag display), pantry list page (`Pantry.razor`), cook log history, confirmation/error feedback (via the `Result<T>` pattern, see `Common/Result.cs`) |

**Planned but not yet built** (per `docs/specs/002-import-recipe-from-url.md`, `003-import-recipe-from-text.md`, `004-import-recipe-from-photo.md`): future stories envision importing recipes from an external web page URL (scraping structured markup or heuristic fallback), from pasted free text (segmentation pipeline), and from a photographed cookbook page (OCR + the same text segmentation pipeline). None of these introduce a live external communication partner yet — as of this writing there is no HTTP client, scraping library, or OCR service referenced anywhere in `src/`. This is called out here only as a forward-looking note; it is not part of the current system scope.

## Technical Context

<!-- Technical interfaces (channels, protocols, hardware) mapping to business I/O -->

The system is a single ASP.NET Core Blazor Server process. The browser communicates with the server over HTTP(S) for the initial page load and then over a persistent WebSocket (SignalR circuit) for interactive server-rendered UI updates — this is standard Blazor Server behavior, confirmed by `AddInteractiveServerComponents()` / `.AddInteractiveServerRenderMode()` in `src/RecipeManagementWebApp/Program.cs`. All persistence is local to the host machine; there are no outbound network calls to other services.

```
[diagram placeholder: technical context — Browser <--HTTPS/WebSocket--> Blazor Server app (Program.cs) <--> SQLite file (App_Data/recipes.db) and local filesystem (wwwroot/uploads)]
```

| Technical Interface | Description |
| -------------------- | ----------- |
| Browser <-> Blazor Server app | HTTPS (ports from `Properties/launchSettings.json`: 7113 https profile, 5256 http profile) for initial render; interactive components upgrade to a SignalR/WebSocket circuit for server-side event handling (`InteractiveServer` render mode). `UseAntiforgery()` and, outside Development, `UseHsts()`/`UseExceptionHandler("/Error")` are configured in `Program.cs`. |
| Blazor Server app <-> SQLite database | EF Core `RecipeDbContext` over `Microsoft.Data.Sqlite`, connection string `Data Source=App_Data/recipes.db` (`appsettings.json`). Schema is created via `Database.EnsureCreated()` at startup — there are no EF Core migrations. This is an embedded database file on the same host, not a networked database service. The new `Tag`, `PantryItem`, and `CookLogEntry` entities (exposed via `ITagService`/`TagService`, `IPantryService`/`PantryService`, `ICookLogService`/`CookLogService`, registered in `Program.cs`) persist through this same `RecipeDbContext`/SQLite file — no new technical interface is introduced. |
| Blazor Server app <-> local filesystem (photo storage) | `IPhotoStorageService`/`PhotoStorageService`, configured via `PhotoStorageOptions` (`RootPath=wwwroot/uploads`, `RequestPathPrefix=/uploads` in `appsettings.json`). Uploaded recipe photos are written to and served from the local filesystem under `wwwroot`; there is no cloud/object storage integration. |
| Status/error pages | `UseStatusCodePagesWithReExecute("/not-found")` re-executes the pipeline to an internal `/not-found` route — an internal routing detail, not an external interface. |

No external APIs, identity providers, messaging/notification services, or cloud infrastructure are referenced in `Program.cs`, `appsettings.json`, or the project's `.csproj` dependencies as of this writing. If that changes (e.g., the URL/OCR import stories above are implemented), this table should gain rows for the corresponding outbound HTTP/OCR service interfaces.
