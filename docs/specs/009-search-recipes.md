# Feature Specification: Search Recipes by Name or Ingredient

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 009 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Build the Collection (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

As the recipe collection grows beyond what fits comfortably in a single scroll (Story 006), the cook needs a fast way to jump to a specific recipe they already know they have — either by name or by an ingredient it contains.

### 1.2 Goal

Provide a search box on the browse grid that filters recipes in real time by matching the query against recipe titles and ingredient item names, case-insensitively. Results update live as the user types.

### 1.3 Non-Goals

- Fuzzy/typo-tolerant search (exact substring match only for MVP)
- Tag-based filtering (Story 015)
- Full-text search of steps
- Search ranking/relevance scoring beyond simple match presence

---

## 2. User Stories

### US-001: Search Recipe Collection

**As a** home cook,
**I want** to search my collection by recipe name or by an ingredient it contains,
**so that** I can quickly find a specific recipe without scrolling through the whole grid.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a search input above the recipe grid (Story 006) | Must | US-001 |
| FR-002 | The system shall filter the grid to recipes whose title contains the search query (case-insensitive substring match) | Must | US-001 |
| FR-003 | The system shall filter the grid to recipes with at least one ingredient whose item name contains the search query (case-insensitive substring match) | Must | US-001 |
| FR-004 | The system shall combine title and ingredient matches with OR logic (a recipe matches if either condition is true) | Must | US-001 |
| FR-005 | The system shall update results live as the user types, without requiring a submit action | Should | US-001 |
| FR-006 | The system shall display a "No recipes found" message when the query matches nothing | Should | US-001 |
| FR-007 | The system shall restore the full, unfiltered grid when the search query is cleared | Must | US-001 |
| FR-008 | The system shall not count a recipe twice if it matches both title and ingredient conditions | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Search by Recipe Title
*(FR-001, FR-002, FR-005)*

```gherkin
Given the collection has recipes "Garlic Chicken Stir-Fry", "Garlic Bread", "Pasta Carbonara"
When the user types "garlic" into the search box
Then the grid shows "Garlic Chicken Stir-Fry" and "Garlic Bread"
And "Pasta Carbonara" is hidden
```

### SC-002: Search by Ingredient
*(FR-003, FR-005)*

```gherkin
Given "Pasta Carbonara" has an ingredient "bacon" and no other recipe does
When the user types "bacon" into the search box
Then only "Pasta Carbonara" is shown
```

### SC-003: Query Matches Both Title and Ingredient
*(FR-004, FR-008)*

```gherkin
Given "Chicken Soup" has title match for "chicken" and also has "chicken" as an ingredient
When the user types "chicken"
Then "Chicken Soup" appears exactly once in the results
```

### SC-004: Case-Insensitive Matching
*(FR-002, FR-003)*

```gherkin
Given a recipe titled "Garlic Bread"
When the user types "GARLIC" or "garlic" or "GaRlIc"
Then "Garlic Bread" appears in all cases
```

### SC-005: No Matches Found
*(FR-006)*

```gherkin
Given no recipe title or ingredient contains "pineapple"
When the user types "pineapple"
Then the grid displays: "No recipes found for 'pineapple'"
```

### SC-006: Clearing Search Restores Full Grid
*(FR-007)*

```gherkin
Given the user has searched "garlic" and sees 2 filtered results
When the user clears the search box
Then all recipes in the collection are displayed again
```

### SC-007: Partial Word Match
*(FR-002, FR-003)*

```gherkin
Given a recipe titled "Chicken Fried Rice"
When the user types "fri"
Then "Chicken Fried Rice" matches (substring "fri" found within "Fried")
```

### SC-008: Search Updates Live Without Submit
*(FR-005)*

```gherkin
Given the user is on the browse grid with search focused
When the user types each character of "pasta" one at a time
Then the grid filters progressively after each keystroke
And no "Search" button click is required
```

### SC-009: Empty Search Query
*(FR-007)*

```gherkin
Given the search box is empty
When the user views the browse grid
Then all recipes are shown (equivalent to Story 006's default behavior)
```

---

## 5. Domain Model

### 5.1 Entities

No new entities. This story performs read-only queries against:

#### Recipe
_(Reused from Story 001)_ — matched on `title`.

#### Ingredient
_(Reused from Story 001)_ — matched on `item`.

### 5.2 Relationships

- Unchanged from Story 001/006: a Recipe has one or more Ingredients.

### 5.3 Value Objects

#### SearchQuery

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| text | string | optional, max 200 chars | The raw search text entered by the user |
| normalizedText | string | derived | Trimmed, lowercased version used for matching |

### 5.4 Domain Rules and Invariants

- **Match is case-insensitive substring search**: Both title and ingredient matching use the same normalization (trim + lowercase) before comparison.
- **OR semantics across title and ingredient**: A recipe is included in results if it matches on title, ingredient, or both.
- **No duplicate results**: A recipe matching on multiple ingredients or on both title and ingredient appears exactly once in results.
- **Empty query means no filtering**: An empty or whitespace-only query is equivalent to "show all recipes."

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Search results update in < 200ms per keystroke for collections up to 500 recipes | Live-filter search must feel instant |
| NFR-002 | Usability | Search input is debounced to avoid excessive re-filtering on rapid typing | Prevents jank on fast typists |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Query contains special regex characters (e.g., "." or "*") | Treated as literal characters in substring match, not regex |
| EC-002 | Query is only whitespace | Treated as empty; full grid shown |
| EC-003 | Query matches an ingredient note but not the item itself (e.g., searching "minced") | Does not match (FR-003 matches item name only, not notes) — documented as a known limitation |
| EC-004 | Very large collection (1000+ recipes) | Search remains responsive via efficient client-side or indexed server-side filtering |
| EC-005 | Query has leading/trailing whitespace | Trimmed before matching |
| EC-006 | User searches while grid is still loading (Story 006 lazy load) | Search applies to loaded recipes; results update as more load, if applicable |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-009) pass in CI |
| SC-002 | Search results update in < 200ms for typical collection sizes |
| SC-003 | Title and ingredient matches are combined correctly with no duplicates |
| SC-004 | Case-insensitive substring matching works correctly across all test cases |
| SC-005 | Clearing the search restores the full grid |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (recipes and ingredients must exist)
- Integrates with Story 006 (search box lives above/within the browse grid)

### 9.2 Constraints

- Single-user, personal use
- Exact substring match only; no fuzzy matching, stemming, or typo tolerance in MVP
- Matches ingredient item name only, not notes or quantities

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Query/filter implementation (client-side vs. server-side) |
| 8. Cross-Cutting Concepts | Debouncing, normalization patterns |
| 10. Quality Requirements | Search responsiveness target |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should search also match ingredient notes (e.g., "minced")? | Product | Open | Recommend item-name-only for MVP; expand later if needed |
| 2 | Should search be client-side (fast, simple) or server-side (scales better for very large collections)? | Architecture | Open | Recommend client-side for MVP given expected personal-collection scale |

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
