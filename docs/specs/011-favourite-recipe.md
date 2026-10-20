# Feature Specification: Favourite a Recipe

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 011 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Build the Collection (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Some recipes are reliable go-tos the cook makes often; others are experiments or one-offs. As the collection grows, finding those go-to recipes among everything else becomes harder without a way to mark and surface them.

### 1.2 Goal

Let the cook mark any recipe as a favourite with a single tap/click, toggle it off just as easily, and optionally filter the browse grid to favourites only.

### 1.3 Non-Goals

- Multiple favourite "lists" or collections (single boolean favourite state only)
- Favourite ranking or ordering beyond simple flag
- Sharing favourites with others

---

## 2. User Stories

### US-001: Favourite a Recipe

**As a** home cook,
**I want** to favourite a recipe,
**so that** my reliable go-tos stay easy to find as my collection grows.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a favourite toggle (e.g., star/heart icon) on the recipe detail page (Story 007) | Must | US-001 |
| FR-002 | The system shall display a favourite toggle on each recipe card in the browse grid (Story 006) | Should | US-001 |
| FR-003 | The system shall persist the favourite state immediately when toggled, without requiring a separate save action | Must | US-001 |
| FR-004 | The system shall visually distinguish favourited recipes (e.g., filled vs. outline icon) | Must | US-001 |
| FR-005 | The system shall provide a "Favourites only" filter toggle on the browse grid | Should | US-001 |
| FR-006 | The system shall preserve favourite state across recipe edits (Story 010) | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Favourite a Recipe from Detail Page
*(FR-001, FR-003, FR-004)*

```gherkin
Given the user is viewing a recipe detail page with the favourite icon unfilled
When the user clicks the favourite icon
Then the recipe is marked as favourited
And the icon immediately changes to its filled/active state
And no separate save step is required
```

### SC-002: Unfavourite a Recipe
*(FR-003, FR-004)*

```gherkin
Given a recipe is currently favourited
When the user clicks the favourite icon again
Then the recipe is unmarked as favourited
And the icon reverts to its unfilled/inactive state
```

### SC-003: Favourite from Grid Card
*(FR-002, FR-003)*

```gherkin
Given the user is viewing the browse grid (Story 006)
When the user clicks the favourite icon on a recipe card
Then that recipe is favourited
And the card icon updates without navigating to the detail page
```

### SC-004: Filter to Favourites Only
*(FR-005)*

```gherkin
Given the collection has 10 recipes, 3 of which are favourited
When the user enables "Favourites only"
Then only the 3 favourited recipes are displayed in the grid
When the user disables the filter
Then all 10 recipes are displayed again
```

### SC-005: Favourite State Survives Edit
*(FR-006)*

```gherkin
Given a recipe is favourited
When the user edits the recipe's title (Story 010) and saves
Then the recipe remains favourited after the edit
```

### SC-006: Rapid Toggle
*(FR-003)*

```gherkin
Given a recipe is unfavourited
When the user clicks the favourite icon twice in quick succession
Then the final persisted state reflects the last toggle (favourited after odd clicks, unfavourited after even clicks)
```

### SC-007: No Favourites Yet
*(FR-005)*

```gherkin
Given no recipes are favourited
When the user enables "Favourites only"
Then the grid displays: "No favourites yet — tap the star on a recipe to add one"
```

---

## 5. Domain Model

### 5.1 Entities

#### Recipe
_(Extended from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| isFavorite | boolean | required, default false | Whether the cook has marked this recipe as a favourite |

_(All other Recipe attributes as defined in Story 001.)_

### 5.2 Relationships

No new relationships; `isFavorite` is a simple attribute on the existing Recipe entity.

### 5.3 Value Objects

No new value objects.

### 5.4 Domain Rules and Invariants

- **Favourite is a simple boolean flag**: No favourite lists, categories, or ranking — a recipe is either favourited or not.
- **Toggling is independent of editing**: Favouriting does not update the recipe's `updatedAt` timestamp (it is not considered a content edit) — this is a UX/semantic decision to keep "recently edited" meaningful.
- **Favourite state persists independently of other fields**: Editing or scaling a recipe does not affect its favourite state.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Favourite toggle persists in < 100ms | Should feel instantaneous, like a like/star button anywhere else |
| NFR-002 | Usability | Favourite icon is easily tappable on mobile (minimum 44x44px touch target) | Common interaction on grid cards during quick browsing |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Network fails while toggling favourite | Optimistic UI update with rollback on failure, or clear error with retry |
| EC-002 | User toggles favourite on a recipe that was just deleted elsewhere (edge timing) | Toggle fails gracefully; recipe no longer exists, error shown |
| EC-003 | User favourites, then immediately deletes the recipe | Recipe (and its favourite state) is removed entirely; no orphaned data |
| EC-004 | All recipes in the collection are favourited | "Favourites only" filter shows the entire collection; no special casing needed |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | Favourite toggle persists in < 100ms |
| SC-003 | Favourite state is visible and consistent across detail page and grid |
| SC-004 | "Favourites only" filter correctly narrows the grid |
| SC-005 | Favourite state survives recipe edits |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (recipes must exist)
- Displayed in Story 006 (grid) and Story 007 (detail)
- Favourite state preserved through Story 010 (edit)

### 9.2 Constraints

- Single-user, personal use (no shared/collaborative favourites)
- Single boolean flag; no multiple favourite lists or tiers

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Recipe attribute storage and update pattern |
| 8. Cross-Cutting Concepts | Optimistic UI update patterns |
| 10. Quality Requirements | Responsiveness of toggle interaction |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should favourited recipes float to the top of the default grid order, or only surface via the explicit filter? | UX | Open | Recommend explicit filter only for MVP; avoids surprising reordering |

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
