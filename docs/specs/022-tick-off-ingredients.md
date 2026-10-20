# Feature Specification: Tick Off Ingredients in Cook Mode

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 022 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Cook It (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

When assembling ingredients at the start of cooking (mise en place), it's easy to lose track of which ones have already been added, especially when interrupted mid-recipe. Story 019's scaled ingredient reference list is currently read-only, with no way to mark progress.

### 1.2 Goal

Allow the cook to tap each ingredient in cook mode's reference list to mark it as added/used, with a clear visual change (e.g., strikethrough), so they always know where they left off.

### 1.3 Non-Goals

- Persisting checked state across different cook sessions (resets each time cook mode is entered fresh)
- Checking off ingredients from the recipe detail page (Story 007) outside of cook mode
- Automatic checking based on recipe step progress (manual tap only)

---

## 2. User Stories

### US-001: Track Ingredient Progress While Cooking

**As a** home cook,
**I want** to tick off ingredients as I add them,
**so that** I don't lose my place if I'm interrupted while cooking.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a checkbox or tappable state next to each ingredient in cook mode's reference list (Story 019) | Must | US-001 |
| FR-002 | The system shall toggle an ingredient's checked state when tapped | Must | US-001 |
| FR-003 | The system shall visually distinguish checked ingredients (e.g., strikethrough text, dimmed color) | Must | US-001 |
| FR-004 | The system shall maintain checked state for the duration of the current cook session, regardless of which step is being viewed | Must | US-001 |
| FR-005 | The system shall provide a "Reset all" action to uncheck every ingredient | Should | US-001 |
| FR-006 | The system shall preserve each ingredient's checked state when servings are scaled (Story 007/019), since scaling only changes displayed quantity, not which ingredient it is | Must | US-001 |
| FR-007 | The system shall reset all checked states when a new cook session begins (e.g., re-entering cook mode after exiting) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Check Off an Ingredient
*(FR-001, FR-002, FR-003)*

```gherkin
Given cook mode displays a reference list including "2 cups flour"
When the user taps the checkbox next to "2 cups flour"
Then the ingredient is marked checked
And displays with a strikethrough or dimmed style
```

### SC-002: Uncheck an Ingredient
*(FR-002, FR-003)*

```gherkin
Given "2 cups flour" is currently checked
When the user taps it again
Then it reverts to its normal, unchecked appearance
```

### SC-003: Checked State Persists Across Step Navigation
*(FR-004)*

```gherkin
Given the user checks off "chicken" while viewing Step 1
When the user navigates to Step 3
Then "chicken" still appears checked in the reference list
```

### SC-004: Reset All Checked Ingredients
*(FR-005)*

```gherkin
Given 3 of 5 ingredients are checked
When the user taps "Reset All"
Then all 5 ingredients return to unchecked state
```

### SC-005: Checked State Survives Scaling
*(FR-006)*

```gherkin
Given the user checked "2 cups flour" (at 2 servings)
When the user scales the recipe to 4 servings (Story 007/019)
Then the ingredient now displays as "4 cups flour"
And it remains checked (scaling changes quantity text, not identity)
```

### SC-006: Fresh Cook Session Resets State
*(FR-007)*

```gherkin
Given the user checked off all ingredients and exited cook mode
When the user starts cooking the same recipe again later (a new cook session)
Then all ingredients start unchecked
```

### SC-007: All Ingredients Checked
*(FR-002, FR-003)*

```gherkin
Given the user has checked every ingredient in the reference list
Then all are displayed with the checked style
And no special "all done" message is required (simple visual state is sufficient)
```

---

## 5. Domain Model

### 5.1 Entities

No new persisted entities. Checked state is ephemeral, scoped to the active cook session.

#### CookSession
_(Extended from Story 019)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| checkedIngredientIds | array of UUID | optional, default empty | IDs of Ingredients (Story 001) currently marked as checked for this session |

### 5.2 Relationships

- A **CookSession** (Story 019) tracks checked state referencing **Ingredient** IDs (Story 001) by identity, independent of displayed/scaled quantity.

### 5.3 Value Objects

No new value objects.

### 5.4 Domain Rules and Invariants

- **Checked state is keyed by ingredient identity, not displayed text**: Since scaling changes the displayed quantity string but not the underlying Ingredient's `id`, checked state survives scaling (FR-006).
- **Checked state is session-scoped**: It is not persisted to the Recipe or Ingredient entities themselves; it lives only within the current CookSession and is discarded when the session ends.
- **No partial reset**: "Reset All" clears every checked ingredient at once; there's no per-item "reset" distinct from simply unchecking it.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Responsiveness | Toggling a checkbox updates the display in < 50ms | Should feel instant during active cooking |
| NFR-002 | Usability | Checkboxes/tap targets are large enough for quick, imprecise taps (minimum 44x44px) | User may be cooking with messy or wet hands |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User checks an ingredient, then edits the recipe (Story 010) to remove that ingredient mid-session | Checked state for the removed ingredient becomes irrelevant; no error, simply no longer displayed |
| EC-002 | User exits and re-enters cook mode without closing the app (quick back-and-forth) | Per FR-007, state resets on a genuinely new session; exact boundary (e.g., "Exit" button vs. tab switch) should be clarified during implementation |
| EC-003 | Ingredient list is very long (30+ items) | All remain individually checkable; list scrolls within the reference area |
| EC-004 | User rapidly taps the same ingredient many times | Final state reflects the last tap (even/odd toggle count), no race condition |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | Toggling updates the display in < 50ms |
| SC-003 | Checked state persists correctly across step navigation and servings scaling |
| SC-004 | Checked state resets correctly on a new cook session |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends entirely on Story 019 (cook mode); the ingredient reference list it extends is defined there
- Interacts with Story 007/019's scaling logic (checked state must be scaling-safe)

### 9.2 Constraints

- Single-user, personal use
- No persistence beyond the active cook session
- Checked state is purely a UI/UX convenience, not part of the Recipe or Ingredient domain model

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Cook session state extension |
| 8. Cross-Cutting Concepts | Session-scoped state management patterns |
| 10. Quality Requirements | Responsiveness of toggle interaction |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | What exact boundary defines "a new cook session" for reset purposes — clicking "Start Cooking" again, or just re-rendering the page? | Architecture | Open | Recommend tying it to a fresh CookSession creation (i.e., clicking "Start Cooking" from detail view again) |

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
