# 1. Introduction and Goals

## Requirements Overview

<!-- Key functional requirements and driving forces for the system -->

RecipeManagement is a **personal, single-user cookbook application**, not a multi-tenant
or social product. Per `docs/product/story-map.md`, the driving goal is to close a
"capture → decide → cook" loop for one home cook:

- **Capture**: get recipes into one place with minimal friction (manual entry today;
  URL/text/photo import are specified but not yet implemented).
- **Decide**: answer "what's for dinner tonight?", ultimately driven by what's in the
  pantry and what hasn't been cooked recently.
- **Cook**: walk through a recipe hands-free/one-handed with a phone on the counter,
  with servings scaling and a step-by-step cook mode.

The project is built as an ASP.NET Core Blazor Server app (.NET 10, nullable reference
types enabled) backed by SQLite via EF Core (see `src/RecipeManagementWebApp/Program.cs`,
`Data/RecipeDbContext.cs`). Photos are stored on the local filesystem under
`wwwroot/uploads`, not in cloud storage.

**Implementation status (as of this writing)**: of the 24 stories defined in the story
map, only **Story 001 (add recipe manually)**, **Story 006 (browse recipes grid)**, and
**Story 007 (view recipe detail, including servings scaling)** are implemented in `src/`.
Everything else — pantry (012), ingredient-availability matching (014), tags as a
first-class feature (008), cook mode (019), cook log (023), importers (002–005), search
(009), editing a recipe (010 — note that *deleting* a recipe is already implemented via
`RecipeService.DeleteAsync`; only edit is still missing), and the remaining "Should"/"Could"
stories — is specified in `docs/specs/` but not yet built. This document describes the goals for the system the
specs and story map define; later arc42 sections should be read with this
spec-vs-implementation gap in mind.

**Explicitly out of scope** (per the story map's "Won't Have"): multi-user support, user
accounts, recipe sharing, weekly meal planning, and shopping-list generation. This is
deliberately a single-player tool.

## Quality Goals

<!-- Top 3-5 quality goals for the architecture, ordered by priority -->

| Priority | Quality Goal | Motivation |
| -------- | ------------ | ---------- |
| 1 | **Fast, low-friction interactions** | Every implemented and specified feature carries a sub-second performance NFR: recipe save < 500ms p95 (spec 001), grid render < 2s for 100+ recipes (spec 006), recipe detail load < 500ms and servings recalculation < 100ms (spec 007), pantry-match results < 500ms (spec 014), cook-mode step navigation < 100ms (spec 019). The app exists to answer "what's for dinner tonight?" quickly — sluggish interactions directly undermine the product's purpose. |
| 2 | **Data integrity and reliability of the recipe collection** | Recipes are persisted atomically, all-or-nothing (spec 001 NFR-002); a failed photo upload must not block a recipe save (spec 001 NFR-005); servings-scaling math must be exact for decimals/fractions/ranges since "incorrect scaled quantities could ruin the dish" (spec 007 NFR-005); pantry and ingredient-matching results must be deterministic (spec 014 NFR-004) and survive app restarts (spec 012 NFR-004). As a single home cook's only copy of their recipe collection, correctness and durability matter more than throughput or scale. |
| 3 | **Usable hands-free/one-handed in the kitchen** | Cook mode is specified to be touch-optimized with large, easily-tapped buttons for wet hands, 18pt+ high-contrast step text readable from across the kitchen, and full-screen on mobile (spec 019). Forms and grids elsewhere must stay keyboard-navigable for users with limited mouse access (specs 001, 006, 007). This reflects the primary persona's actual usage context: phone on the counter while cooking, not a desk-bound user. |
| 4 | **Responsive across devices** | Recipes are captured/planned from a desktop and consulted/cooked-from on a phone, so layouts must adapt from desktop (3-4 column grid) down to mobile (1-2 columns) and remain readable on all three breakpoints (specs 006, 007). |

## Stakeholders

<!-- Key stakeholders and their expectations -->

| Role/Name | Expectations |
| --------- | ------------ |
| Primary user — single home cook (personal use) | Per `docs/product/story-map.md`: wants their whole recipe collection in one place instead of scattered across bookmarks/screenshots/cookbooks, a fast answer to "what can I make right now" based on pantry contents and recent cooking history, and a hands-free guide while actually cooking. There is no secondary/admin/multi-user persona — this is explicitly a single-player tool. |
| Developer/maintainer — `joerivanarkel` (repository owner) | Builds and evolves the app per the story map's MoSCoW build order; owns the GitHub repository (`joerivanarkel/RecipeManagement`) used for issue tracking per `CLAUDE.md`. No separate product owner, business sponsor, or operations stakeholder is evidenced in the repo — the maintainer and the primary user are, in practice, likely the same person building a tool for their own use. |
| TODO | No other stakeholders (e.g., external integrators, support teams, compliance reviewers) are evidenced by the code, specs, or git history. Add here if/when they emerge. |
