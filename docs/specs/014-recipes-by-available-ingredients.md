# Feature Specification: Recipes Ranked by Available Ingredients

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 014 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Decide What to Cook Tonight (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

At 6pm, a cook asks "what can I make right now?" The app needs to match their pantry against all recipes and show which recipes they can already make, ranked by completeness. Without this, the pantry (Story 012) is just a list; with it, the app answers the core question.

### 1.2 Goal

Display recipes ranked by how many of their ingredients are in the pantry. For each recipe, show which ingredients are available and which are missing. The cook can quickly scan and pick a recipe they can actually make.

### 1.3 Non-Goals

- Filtering or sorting (Story 015 adds tag filtering; Story 016 adds recency)
- Auto-shopping list generation
- Fuzzy ingredient matching or substitutions
- Quantity matching (e.g., "do I have *enough* flour for this recipe")

---

## 2. User Stories

### US-001: Find Recipes by Available Ingredients

**As a** home cook,
**I want** to see all my recipes ranked by how many ingredients I already have in my pantry,
**so that** I can quickly decide what to cook tonight based on what's actually available.

### US-002: View Missing Ingredients

**As a** home cook,
**I want** to see which ingredients are missing for each recipe,
**so that** I know what I'd need to buy or skip to make it.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a page that matches pantry items against all recipes and ranks them by availability | Must | US-001 |
| FR-002 | The system shall rank recipes by percentage of ingredients available (e.g., "4 of 5 ingredients available") | Must | US-001 |
| FR-003 | The system shall match pantry items to recipe ingredients by item name (case-insensitive, exact name match) | Must | US-001 |
| FR-004 | The system shall ignore quantity when matching (if pantry has "chicken", it matches any recipe ingredient "chicken" regardless of quantity) | Must | US-001 |
| FR-005 | The system shall display recipes with 100% available ingredients first, then decreasing availability | Must | US-001 |
| FR-006 | The system shall show for each recipe: title, photo, availability ratio (e.g., "4/5"), and available/missing ingredients list | Must | US-002 |
| FR-007 | The system shall make recipes clickable, opening the detail view (Story 007) | Must | US-001 |
| FR-008 | The system shall ignore ingredients with "to taste" or similar open-ended quantities (treat as always available) | Should | US-001 |
| FR-009 | The system shall refresh the results if the pantry is edited (Story 012) without requiring a page reload | Should | US-001 |
| FR-010 | The system shall handle empty pantry or no matching recipes gracefully (show appropriate message) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Match Pantry to Recipes (Happy Path)
*(FR-001, FR-002, FR-003, FR-005)*

```gherkin
Given the pantry contains:
  | item |
  | chicken |
  | rice |
  | garlic |
And the recipe collection contains:
  | recipe | ingredients |
  | Chicken Fried Rice | chicken, rice, garlic, soy sauce, eggs |
  | Garlic Bread | garlic, bread, butter |
  | Pasta Carbonara | pasta, eggs, bacon, parmesan |
When the user views the "What Can I Make" page
Then recipes are ranked:
  1. Chicken Fried Rice: 3/5 available (chicken, rice, garlic)
  2. Garlic Bread: 1/3 available (garlic)
  3. Pasta Carbonara: 0/5 available
```

### SC-002: Show Available and Missing Ingredients
*(FR-006)*

```gherkin
Given the user is viewing Chicken Fried Rice (3/5 available)
When the recipe card is displayed
Then it shows:
  - Recipe title: "Chicken Fried Rice"
  - Availability: "3 of 5 ingredients available"
  - Available ingredients: chicken, rice, garlic
  - Missing ingredients: soy sauce, eggs
```

### SC-003: Ignore Quantity When Matching
*(FR-004)*

```gherkin
Given the pantry contains:
  | item |
  | flour |
  | 2 eggs |
And a recipe requires:
  | ingredient | quantity |
  | flour | 2 cups |
  | eggs | 12 |
When matching ingredients
Then flour and eggs both match (quantity is ignored)
And the recipe shows 2/2 available
```

### SC-004: Treat Open-Ended Quantities as Always Available
*(FR-008)*

```gherkin
Given a recipe ingredient: "salt to taste"
When matching against the pantry
Then the ingredient is treated as always available
And it counts toward the availability ratio (e.g., 4/5 available instead of 3/5)
```

### SC-005: Recipe With 100% Available Ingredients First
*(FR-005)*

```gherkin
Given recipes:
  | recipe | available |
  | Pasta with Olive Oil | 5/5 |
  | Chicken Fried Rice | 3/5 |
  | Garlic Bread | 1/3 |
When the user views the results
Then recipes are sorted: Pasta with Olive Oil, Chicken Fried Rice, Garlic Bread
```

### SC-006: No Matching Recipes
*(FR-010)*

```gherkin
Given the pantry is empty
When the user views the "What Can I Make" page
Then the page displays: "Your pantry is empty. Add items to see recipe suggestions."
And a link appears: "Go to Pantry"
```

### SC-007: Click Recipe to View Detail
*(FR-007)*

```gherkin
Given the user is viewing the "What Can I Make" page
When the user clicks on a recipe
Then the recipe detail page (Story 007) opens with that recipe
```

### SC-008: Empty Recipe Collection
*(FR-010)*

```gherkin
Given the user has no recipes
When the user views the "What Can I Make" page
Then the page displays: "You have no recipes yet. Add some recipes to get started."
```

### SC-009: Pantry Updated, Results Refresh
*(FR-009)*

```gherkin
Given the user is on the "What Can I Make" page
When the user adds "soy sauce" to their pantry
Then the page automatically updates
And Chicken Fried Rice now shows 4/5 instead of 3/5
And recipes are re-ranked
```

### SC-010: Case-Insensitive Ingredient Matching
*(FR-003)*

```gherkin
Given the pantry contains:
  | item |
  | Chicken |
  | GARLIC |
And a recipe requires:
  | ingredient |
  | chicken |
  | garlic |
When matching
Then both ingredients match
And the recipe shows correct availability
```

---

## 5. Domain Model

### 5.1 Entities

#### Recipe
_(Reused from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Recipe identifier |
| title | string | required | Recipe name |
| ingredients | array | at least one | Ingredients with item names |

#### PantryItem
_(Reused from Story 012)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Pantry item identifier |
| itemName | string | required | Name of ingredient |
| quantity | string | required | Human-readable quantity |
| category | enum | one of: FRIDGE, FREEZER, CUPBOARDS | Storage location |

#### RecipeAvailability
_(New value object for results)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| recipeId | UUID | required | Recipe identifier |
| totalIngredients | integer | required | Total ingredient count |
| availableCount | integer | required, >= 0 | Ingredients available in pantry |
| availableIngredients | array | required | List of available ingredient items |
| missingIngredients | array | required | List of missing ingredient items |
| percentageAvailable | decimal | 0-100 | Availability as percentage |

### 5.2 Relationships

- A **Recipe** is matched against **PantryItems** to produce a **RecipeAvailability** result.

### 5.3 Value Objects

#### IngredientMatch

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| recipeIngredient | string | required | Ingredient name from recipe |
| pantryItem | PantryItem | optional | Matching pantry item, if found |
| isAvailable | boolean | required | True if pantry item exists |

### 5.4 Domain Rules and Invariants

- **Matching is item-name only**: Ingredient matching is case-insensitive exact string match on item name; quantity is ignored.
- **Open-ended quantities count as available**: Ingredients with "to taste", "as needed", etc. are treated as always available.
- **Ranking by availability**: Recipes are sorted by `availableCount / totalIngredients` descending (100% first).
- **Missing ingredient list is complete**: If an ingredient is not in the pantry, it appears in missing ingredients.
- **Ingredient names are normalized for matching**: Item names are trimmed and case-normalized before comparison.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Results load and display in < 500ms for 100 recipes and 50 pantry items | User should see results instantly |
| NFR-002 | Performance | Matching algorithm is efficient (O(n×m) acceptable for small collections) | Real-time match-and-rank must be responsive |
| NFR-003 | Responsiveness | Pantry updates trigger results refresh without full page reload (< 100ms) | Live feedback when pantry changes |
| NFR-004 | Correctness | Ingredient matching is deterministic (same pantry/recipes = same results) | User must trust the ranking |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Ingredient name has different casing ("Chicken" vs "chicken") | Match succeeds (case-insensitive) |
| EC-002 | Ingredient name has leading/trailing whitespace | Trimmed before matching |
| EC-003 | Recipe has no ingredients | Recipe displays as 0/0 available (or skipped) |
| EC-004 | Pantry is empty | All recipes show 0/X available; page shows "empty pantry" message |
| EC-005 | Recipe ingredient is "salt to taste" | Treated as always available; counts toward availability ratio |
| EC-006 | Recipe requires "2-3 cloves garlic", pantry has "garlic" | Match succeeds (quantity ignored) |
| EC-007 | Multiple pantry items with same name (e.g., "chicken" twice) | User can have duplicates; matching counts either as available (exists) |
| EC-008 | Pantry item quantity is very long or malformed | Quantity is stored as-is; does not affect matching |
| EC-009 | 1000+ recipes and 100+ pantry items | Results load in < 500ms; pagination/lazy loading if needed |
| EC-010 | Pantry changes while results page is open | Results refresh automatically (Story 009 dependency) |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-010) pass in CI |
| SC-002 | Results load in < 500ms for typical collections (100 recipes, 50 pantry items) |
| SC-003 | Ingredient matching is case-insensitive and quantity-independent |
| SC-004 | Recipes are correctly ranked by availability (100% first, decreasing) |
| SC-005 | Available and missing ingredients are clearly displayed |
| SC-006 | Page handles empty pantry, empty recipe collection, and no matches gracefully |
| SC-007 | Results refresh when pantry is updated (Story 012) |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (recipes must exist with ingredients)
- Depends on Story 012 (pantry items must exist)
- Feeds into Story 015 (filtering by tag)
- Feeds into Story 016 (filtering by recency)
- Feeds into Story 007 (recipes are clickable and open detail view)

### 9.2 Constraints

- Matching is item-name only; no fuzzy matching or synonym support
- Quantity-based matching is out of scope (Story 014 does not check "do I have *enough*")
- Single-user, personal use
- No shopping list generation or buying recommendations

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Query and matching algorithms for recipes and pantry |
| 8. Cross-Cutting Concepts | Caching and performance for real-time matching |
| 10. Quality Requirements | Performance targets and correctness guarantees |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should partial matches (e.g., "chicken breast" vs "chicken") be treated as matches? | Product | Open | Recommend exact item-name match for MVP; fuzzy match later if needed |
| 2 | Should the results page have a title/description, or just display the ranked list? | UX | Open | Recommend a header like "What You Can Cook" or "Based on Your Pantry" |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero in this spec)
  - [x] Open questions are assigned and have a resolution path
-->
