# Feature Specification: Quick Ingredient Entry for Matching

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 013 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Decide What to Cook Tonight (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Sometimes a cook doesn't want to maintain a full pantry (Story 012) — they just have a handful of things on the counter ("chicken, cabbage, rice") and want an instant answer to "what can I make with these?" Requiring a full pantry update for a quick, one-off check is too much friction.

### 1.2 Goal

Provide a lightweight text input on the "what can I cook" page (Story 014) where the cook types a short comma-separated list of ingredients. These are combined with the saved pantry for that session's matching calculation, without being persisted to the pantry.

### 1.3 Non-Goals

- Persisting quick-typed ingredients to the pantry (Story 012 remains the durable store)
- Autocomplete or validation of ingredient names in the quick-entry box
- Saving quick-entry lists for reuse across sessions

---

## 2. User Stories

### US-001: Quick-Type Ingredients for a One-Off Check

**As a** home cook,
**I want** to quickly type a few ingredients ("chicken, cabbage, rice") without updating my whole pantry,
**so that** I can get an instant answer to "what can I make" without the overhead of managing my full pantry list.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a free-text input on the "what can I cook" page (Story 014) for quickly entering ingredients | Must | US-001 |
| FR-002 | The system shall accept a comma-separated list of ingredient names in the quick-entry input | Must | US-001 |
| FR-003 | The system shall combine the quick-entry ingredients with the saved pantry (Story 012) for that session's matching calculation (union, not replacement) | Must | US-001 |
| FR-004 | The system shall NOT persist quick-entry ingredients to the pantry | Must | US-001 |
| FR-005 | The system shall update recipe matching results (Story 014) immediately as quick-entry ingredients are added or cleared | Should | US-001 |
| FR-006 | The system shall provide a way to clear the quick-entry list without affecting the saved pantry | Must | US-001 |
| FR-007 | The system shall treat quick-entry ingredient names the same way as pantry items for matching purposes (case-insensitive item-name match) | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Quick Entry Supplements Pantry Matching
*(FR-001, FR-002, FR-003, FR-005)*

```gherkin
Given the saved pantry contains "rice" only
And the recipe "Chicken Cabbage Stir-Fry" requires chicken, cabbage, rice
When the user types "chicken, cabbage" into the quick-entry box
Then the "what can I cook" results recalculate
And "Chicken Cabbage Stir-Fry" now shows 3/3 available
```

### SC-002: Quick Entry Does Not Persist to Pantry
*(FR-004)*

```gherkin
Given the user types "chicken, cabbage" into the quick-entry box
When the user navigates to the Pantry page (Story 012)
Then "chicken" and "cabbage" do NOT appear in the saved pantry list
```

### SC-003: Clear Quick Entry
*(FR-006)*

```gherkin
Given the user has typed "chicken, cabbage" into quick-entry
And matching results reflect those extra ingredients
When the user clicks "Clear"
Then the quick-entry input empties
And matching results revert to reflect only the saved pantry
```

### SC-004: Comma-Separated Parsing
*(FR-002)*

```gherkin
Given the user types "chicken,  cabbage ,rice"
When the input is parsed
Then it is split into three ingredient names: "chicken", "cabbage", "rice"
And each is trimmed of extra whitespace
```

### SC-005: Case-Insensitive Matching
*(FR-007)*

```gherkin
Given the user types "CHICKEN" into quick-entry
And a recipe requires "chicken"
When matching is recalculated
Then the recipe counts "chicken" as available
```

### SC-006: Quick Entry with No Saved Pantry
*(FR-003)*

```gherkin
Given the saved pantry is empty
When the user types "chicken, rice" into quick-entry
Then matching results are calculated using only the quick-entry ingredients
```

### SC-007: Duplicate Between Quick Entry and Pantry
*(FR-003)*

```gherkin
Given the saved pantry already contains "chicken"
When the user also types "chicken" into quick-entry
Then "chicken" is counted once for matching purposes (no double-counting or error)
```

### SC-008: Empty Quick Entry Has No Effect
*(FR-003)*

```gherkin
Given the quick-entry box is empty
When the user views "what can I cook" results
Then results reflect only the saved pantry, identical to Story 014's default behavior
```

---

## 5. Domain Model

### 5.1 Entities

No new persisted entities — quick-entry ingredients are ephemeral, UI/session-level state only.

#### QuickEntryIngredient
_(Transient value object, not persisted)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| itemName | string | required, trimmed | Ingredient name as typed by the user |

### 5.2 Relationships

- QuickEntryIngredients are combined (set union by normalized item name) with **PantryItems** (Story 012) only for the duration of a single matching calculation (Story 014); they are never written to storage.

### 5.3 Value Objects

See QuickEntryIngredient above.

### 5.4 Domain Rules and Invariants

- **Session-only, never persisted**: Quick-entry ingredients exist only in the current page's state and are discarded on navigation or refresh.
- **Union, not replacement**: Quick-entry ingredients are added to the pantry's ingredient set for matching; they do not hide or replace existing pantry items.
- **Same matching rules as pantry**: Quick-entry ingredients use the identical case-insensitive, item-name-only matching logic defined in Story 014.
- **No deduplication errors**: An ingredient present in both the pantry and quick-entry is simply available; no double-counting affects the availability ratio.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Matching results recalculate in < 200ms after quick-entry changes | Should feel as responsive as typing a search box |
| NFR-002 | Usability | Clearing quick-entry is a single, obvious action (one click/tap) | Encourages experimentation without fear of "messing up" the pantry |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User types a trailing comma ("chicken, cabbage,") | Trailing empty segment is ignored |
| EC-002 | User types only commas and whitespace | Treated as empty; no ingredients added |
| EC-003 | User types a very long list (50+ items) | All are parsed and used for matching; no artificial limit |
| EC-004 | User navigates away and returns to the page | Quick-entry list is cleared (not persisted across navigation) |
| EC-005 | User pastes a list with newlines instead of commas | Newlines are also treated as separators, in addition to commas |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-008) pass in CI |
| SC-002 | Quick-entry ingredients never appear in the saved pantry (Story 012) |
| SC-003 | Matching results update in < 200ms after quick-entry changes |
| SC-004 | Comma and whitespace parsing is robust across common input variations |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 012 (pantry) and Story 014 (matching/ranking) — this story only adds an alternate, ephemeral ingredient source into that calculation
- Lives entirely on the "what can I cook" page introduced by Story 014

### 9.2 Constraints

- Single-user, personal use
- No persistence of quick-entry data under any circumstance
- No validation against a known ingredient vocabulary (free text accepted as-is)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Client-side/session state handling for ephemeral input |
| 8. Cross-Cutting Concepts | Reuse of matching logic from Story 014 |
| 10. Quality Requirements | Responsiveness of live recalculation |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should quick-entry ingredients be remembered for the duration of the browser session (not just the page), in case the user navigates away and back? | UX | Open | Recommend clearing on navigation for MVP simplicity; revisit if users request persistence |

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
