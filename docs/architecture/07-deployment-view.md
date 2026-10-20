# 7. Deployment View

## Infrastructure Level 1

<!-- Overview of the deployment environment -->

Today the system has **no production or cloud deployment** — it runs only
as a locally-launched developer process. There is a single node: the
developer's local machine, running one ASP.NET Core process (Kestrel) that
hosts the Blazor Server app, reads/writes an embedded SQLite database file,
and serves uploaded photos from the local filesystem. There is no load
balancer, no separate database server, no CDN, and no reverse proxy in
front of Kestrel.

```
[Developer machine]
   |
   +-- dotnet run (Kestrel process)
   |      |
   |      +-- Blazor Server app (RecipeManagementWebApp)
   |      |      - Razor Components, InteractiveServer render mode
   |      |      - Serves browser over WebSocket (SignalR circuit)
   |      |
   |      +-- EF Core -> SQLite file: App_Data/recipes.db
   |      |      (Database.EnsureCreated() at startup; no migrations)
   |      |
   |      +-- Local filesystem: wwwroot/uploads
   |             (served at request path /uploads)
   |
   +-- Browser (client), connects to http://localhost:5256
          or https://localhost:7113
```

| Node | Description |
| ---- | ----------- |
| Developer machine | The only environment the system currently runs in. Hosts the Kestrel process, the SQLite database file (`App_Data/recipes.db`), and the uploaded-photo files (`wwwroot/uploads`). No separate app server, database server, or storage tier exists — everything is co-located in one process on one machine. |

**Launch profiles** (from `Properties/launchSettings.json`), both
`ASPNETCORE_ENVIRONMENT=Development` only — there is no Staging or
Production profile and no corresponding `appsettings.Staging.json` /
`appsettings.Production.json`:

| Profile | URL(s) | Environment |
| ------- | ------ | ----------- |
| `http`  | `http://localhost:5256` | Development |
| `https` | `https://localhost:7113` (+ `http://localhost:5256`) | Development |

**No production deployment infrastructure exists yet.** Specifically, as of
this writing the repository contains:
- No `Dockerfile` or `docker-compose.yml` — the app is not containerized.
- No `.github/workflows/` or other CI/CD pipeline definitions — there is no
  automated build, test, or deploy pipeline.
- No cloud hosting configuration (e.g., Azure App Service, AWS, etc.) and
  no `appsettings.Production.json`.
- No reverse proxy (e.g., nginx, YARP) or forwarded-headers configuration —
  `Program.cs` only conditionally applies `UseHsts()` outside Development
  and always calls `UseHttpsRedirection()`, with no evidence of a proxy in
  front of Kestrel.

The only documented way to run the system is locally via `dotnet build` /
`dotnet run` (see root `CLAUDE.md`), using SQLite as an embedded file-based
database and the local filesystem for photo uploads. This is consistent
with the product's current scope: a personal, single-user tool
(`docs/product/story-map.md`), still pre-MVP, with only recipe creation,
browsing, and detail view implemented.

TODO: Once a real hosting target is chosen (e.g., a single small VM, a PaaS
like Azure App Service, or a container platform), document it here,
including how the SQLite file and `wwwroot/uploads` directory are
persisted across deployments/restarts (both are currently local-disk state
with no backup or replication strategy).

## Infrastructure Level 2 (if needed)

<!-- Detailed view of individual deployment nodes -->

Not applicable today — there is only the single local-machine node
described above, with no internal subdivision (no separate web/app/db
tiers). Revisit this section if/when a real multi-node deployment exists.

## Mapping: Building Blocks to Infrastructure

<!-- Which building blocks run on which infrastructure nodes -->

| Building Block | Infrastructure Node |
| -------------- | ------------------- |
| Blazor Server UI (`Components/Layout`, `Components/Pages`) | Developer machine (Kestrel process) |
| `Services/` (`IRecipeService`/`RecipeService`, `IPhotoStorageService`/`PhotoStorageService`, `RecipeValidator`, `IngredientFormatter`) | Developer machine (Kestrel process) |
| `Data/RecipeDbContext` + SQLite database (`App_Data/recipes.db`) | Developer machine (local disk, same process) |
| Uploaded photo files (`wwwroot/uploads`, served at `/uploads`) | Developer machine (local disk, served by the same Kestrel process) |
