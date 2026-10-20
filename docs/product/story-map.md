# User Story Map: RecipeManagement

## Goal
A personal cookbook that gets recipes in effortlessly, answers "what's for dinner tonight?" based on what's actually in the kitchen and what you haven't cooked lately, and then walks you through cooking it with your phone on the counter.

## Primary User
A single home cook (personal use, not multi-user or social) who keeps losing recipes across bookmarks, screenshots and cookbooks, and who stalls at 6pm deciding what to make. They want their whole collection in one place, a fast answer to "what can I make right now", and a hands-free guide while cooking.

## Scope Notes
- **In:** capturing recipes, organizing them, deciding what to cook tonight, cooking along, remembering what was cooked.
- **Out (for now):** weekly meal planning / Sunday shopping-list workflows, sharing, accounts, multi-user.
- The "what's for dinner" decision is driven by two things: **what's in the fridge** and **what I've made too recently**.

## Priority Key (MoSCoW)
Every story here is getting built. MoSCoW is **build order, not a cut list**.

| Priority    | Meaning                                                                             |
| ----------- | ----------------------------------------------------------------------------------- |
| **Must**    | Without this the core loop (capture → decide → cook) doesn't close. Build these first |
| **Should**  | Turns a working app into one worth using daily. Build second                          |
| **Could**   | Delight and polish. Real features, just safe to land later                            |
| **Won't**   | Deliberately out of scope this round — see "Won't Have" below                         |

## Story Map

### Capture Recipes
| #   | Story                                                                                                        | Priority |
| --- | ------------------------------------------------------------------------------------------------------------ | -------- |
| 001 | As a cook, I can add a recipe by hand with a title, ingredients, steps, servings and a photo                   | Must     |
| 002 | As a cook, I can paste a recipe URL and have the title, ingredients and steps scraped in automatically         | Should   |
| 003 | As a cook, I can paste a blob of raw recipe text and have it parsed into structured ingredients and steps      | Could    |
| 004 | As a cook, I can photograph a cookbook page and have the text read and turned into a recipe                    | Could    |
| 005 | As a cook, I can review and fix anything the importer got wrong before the recipe is saved                     | Should   |

### Build the Collection
| #   | Story                                                                                                        | Priority |
| --- | ------------------------------------------------------------------------------------------------------------ | -------- |
| 006 | As a cook, I can see all my recipes as a browsable grid of cards with photos                                   | Must     |
| 007 | As a cook, I can open a recipe and see the full ingredients, steps, time and servings on one page              | Must     |
| 008 | As a cook, I can tag a recipe (cuisine, meal type, vegetarian, quick) so I can narrow things down later        | Should   |
| 009 | As a cook, I can search my collection by recipe name or by an ingredient it contains                           | Should     |
| 010 | As a cook, I can edit or delete a recipe when it changes or I no longer want it                                | Should     |
| 011 | As a cook, I can favourite a recipe so my reliable go-tos stay easy to find                                    | Could    |

### Decide What to Cook Tonight
| #   | Story                                                                                                        | Priority |
| --- | ------------------------------------------------------------------------------------------------------------ | -------- |
| 012 | As a cook, I can keep a pantry list of what I currently have in the fridge, freezer and cupboards              | Must     |
| 013 | As a cook, I can quickly type a few ingredients ("chicken, cabbage, rice") without updating my whole pantry    | Should   |
| 014 | As a cook, I can see recipes ranked by how much of them I can already make, with the missing items called out  | Should     |
| 015 | As a cook, I can filter suggestions by tag so I can ask for something vegetarian or something Asian            | Could    |
| 016 | As a cook, I can hit "surprise me" and get a suggestion that skips anything I've cooked recently               | Should   |
| 017 | As a cook, I can see at a glance when a recipe was last cooked so I don't repeat myself without noticing       | Should   |

### Cook It
| #   | Story                                                                                                        | Priority |
| --- | ------------------------------------------------------------------------------------------------------------ | -------- |
| 018 | As a cook, I can scale a recipe's servings and have every ingredient quantity recalculate                      | Must     |
| 019 | As a cook, I can enter a step-by-step cook mode showing one large step at a time, tapping to advance           | Must     |
| 020 | As a cook, I can keep the screen awake while I'm in cook mode so my phone doesn't sleep mid-recipe             | Should   |
| 021 | As a cook, I can start a timer directly from a step that mentions a duration ("simmer 20 min")                 | Should   |
| 022 | As a cook, I can tick off ingredients as I add them so I don't lose my place                                   | Should   |
| 023 | As a cook, I can mark a recipe as cooked when I finish, recording the date in my cook log                      | Should     |
| 024 | As a cook, I can see my cook history and how often I've made something ("made 3x this month")                  | Could    |

## Priority Summary

### Must (10) — the loop has to close
`001` `006` `007` `009` `010` `012` `014` `018` `019` `023`

One way in (manual form), a collection you can browse, search and correct, a pantry, the what-can-I-make ranking, and a cook mode that scales and records. Ship these and the app genuinely works end to end.

### Should (8) — what makes it a daily habit
`002` `005` `008` `013` `016` `017` `020` `021` `022`

URL import is the single biggest effort-saver for getting recipes in. The recency features (`016`, `017`) are what stop the collection going stale. The cook-mode trio (`020` `021` `022`) is what makes the counter experience not annoying.

### Could (6) — polish
`003` `004` `011` `015` `024`

Text parsing and OCR are the expensive importers — real wins, but `002` covers most real-world capture. `024` is the payoff on the cook log you're already writing in `023`.

### Won't Have (this round)
Weekly meal planning, shopping-list generation, recipe sharing, user accounts and multi-user support. Ruled out in the interview — this is a personal tool.

## Dependency Notes
- `014` needs `012` (pantry) and `001` (recipes exist) first — it's the heart of the app but not the first thing you can build
- `016` and `017` both need `023` (cook log) landed first
- `015` needs `008` (tags)
- `005` only matters once an importer exists (`002`, `003` or `004`)
- `020` `021` `022` all hang off `019` (cook mode)

Suggested first build: `001` → `006` → `007` → `012` → `014`. That gets you to the interesting part fastest.
