---
name: project-recipe-vision
description: What RecipeManagement is actually for — personal cookbook plus a "what's for dinner tonight" decision engine; meal planning and sharing are explicitly out of scope
metadata:
  type: project
---

RecipeManagement is a **single-user personal** app with three jobs: capture recipes, decide what to cook tonight, and guide cooking. Story map lives at `docs/product/story-map.md` (24 numbered stories across 4 activities).

**Why:** The user loses recipes across bookmarks, screenshots and cookbooks, and stalls at 6pm deciding what to make. The decision is driven by exactly two signals they named: **what's in the fridge right now** and **what they've cooked too recently**. That pairing (pantry matching + cook-log recency) is the heart of the product, not a nice-to-have.

**Explicitly ruled out by the user:**
- Weekly/Sunday meal planning and shopping-list generation — they said this is not how they use it
- Sharing, social features, accounts, multi-user — personal tool only

**Confirmed must-haves (user wanted all options, refused to trim):**
- Four recipe import paths: manual form, URL scrape, raw-text parse, photo/OCR
- Full cook mode: serving scaling, step-by-step, wake lock, step timers, ingredient tick-off
- Both a maintained pantry list AND a quick ad-hoc "type what I have" box
- A cook log that records dates and surfaces "made 3x this month"

**Prioritisation:** The story map carries a MoSCoW priority per story, framed explicitly as **build order, not a cut list** — nothing is being dropped. Musts are the 10 stories that close the capture → decide → cook loop (001, 006, 007, 009, 010, 012, 014, 018, 019, 023). The expensive importers (text parse 003, OCR 004) are Coulds because URL scraping (002) covers most real capture. Suggested first build: 001 → 006 → 007 → 012 → 014.

**How to apply:** When scoping or sequencing work, protect the pantry-match and recency features — they are the differentiator. Don't reintroduce meal planning or sharing stories unless the user asks. Original brief suggested "Manage Recipes / Plan Meals / Share Recipes"; the interview overrode two of those three.
