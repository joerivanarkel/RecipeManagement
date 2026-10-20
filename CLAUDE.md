# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RecipeManagement is an ASP.NET Core Blazor web application for managing recipes. It uses:
- **.NET 10.0** with nullable reference types enabled
- **Blazor Server** for interactive server-side components
- **Bootstrap** for UI styling
- **Razor Components** for page and layout structure

The project is structured as a single Blazor web application with components organized by concern (Layout, Pages).

## Quick Start Commands

### Development

Build the project:
```bash
dotnet build
```

Run the development server (launches at `https://localhost:7113`):
```bash
dotnet run
```

Run with HTTP only (port `http://localhost:5256`):
```bash
dotnet run --launch-profile http
```

### Project Structure

- `RecipeManagementWebApp/` — Main ASP.NET Core Blazor application
  - `Program.cs` — Application startup and service configuration
  - `Components/` — Razor components
    - `Layout/` — Shared layouts (MainLayout, NavMenu)
    - `Pages/` — Routable page components (Counter, Weather, Home, etc.)
    - `App.razor` — Root component and app configuration
    - `Routes.razor` — Router configuration with default layout
  - `Properties/launchSettings.json` — Run profiles (http/https)
  - `appsettings.json` — Configuration
  - `wwwroot/` — Static assets (CSS, images, JavaScript)

## Architecture Notes

### Application Structure

**Blazor Server Rendering**: The app uses interactive server-side rendering via `AddInteractiveServerComponents()`. This means components execute C# code on the server and communicate state changes back to the browser via a WebSocket connection.

**Routing**: Blazor routing is defined in `Routes.razor`. All routable pages inherit from `RouteComponentBase` and must be placed in `Components/Pages/`. The `MainLayout` is the default layout for all pages.

**Interactivity**: Components marked with `@rendermode InteractiveServer` run on the server. Use component parameters and cascading parameters to pass data between components.

### Key Configuration

- **Program.cs** sets up:
  - Razor component services with interactive server rendering
  - Status code page middleware (404 handling via `/not-found`)
  - Antiforgery protection
  - Static asset mapping
  - HTTPS redirection (in non-dev environments)

- **.NET Nullable**: Nullable reference types are enabled (`Nullable=enable`), so use `string?` for optional strings and provide null safety.

- **Implicit Usings**: `ImplicitUsings=enable` means common namespaces are already imported (no need for explicit `using` statements in most cases).

## Testing

This is a Blazor web application without a dedicated test project in the current setup. If adding tests, create a separate test project (e.g., `RecipeManagementWebApp.Tests`) and reference the main project.

## Common Tasks

### Adding a New Page

1. Create a new `.razor` file in `Components/Pages/`
2. Add the `@page` directive with a route (e.g., `@page "/recipes"`)
3. Implement the component logic and UI
4. The router automatically picks it up

### Modifying Styling

- Global styles: `wwwroot/app.css`
- Component-specific styles: Add `.razor.css` files next to components
- Bootstrap utilities are available via the included Bootstrap stylesheet

### Adding Interactivity

Use `@rendermode InteractiveServer` on components that need server-side event handling. Components without this directive are static (rendered at request time).

## Debugging

Run with `dotnet run` in development mode to enable hot reload and detailed error pages. The browser console and the terminal where you run `dotnet run` will show diagnostic information.

## Architecture Documentation

Formal architecture documentation follows the arc42 template in `docs/architecture/`. Refer to these documents for high-level design decisions, constraints, and quality goals.
