# Feature Specification: Step-by-Step Cook Mode

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 019 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Cook It (Story Map) |
| Arc42 reference | 3, 5, 8, 10 |

### 1.1 Problem Statement

When cooking, a cook has wet hands and can't navigate a complex UI. They need the recipe instructions to fill the screen, one step at a time, with simple navigation so they can focus on cooking, not scrolling.

### 1.2 Goal

Display a full-screen cook mode where the cook sees one recipe step at a time, with large, readable text. Navigation (next/previous) via buttons and/or swipe. Display scaled ingredients at the top for reference. When finished, record the recipe as cooked (Story 023).

### 1.3 Non-Goals

- Ingredient checkoff (Story 022)
- Timer extraction or launching (Story 021)
- Screen wake lock (Story 020)
- Cooking notes or modifications
- Multi-recipe cooking workflows

---

## 2. User Stories

### US-001: Enter Cook Mode

**As a** home cook,
**I want** to start cooking a recipe from the detail page and enter a full-screen cook mode,
**so that** I can follow the steps hands-free with wet hands.

### US-002: Navigate Steps

**As a** home cook,
**I want** to see one step at a time and navigate forward and backward,
**so that** I can follow the recipe at my own pace without scrolling.

### US-003: View Scaled Ingredients

**As a** home cook,
**I want** to see the scaled ingredient list in cook mode,
**so that** I can reference what I need without leaving cook mode.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall enter cook mode when the user clicks "Start Cooking" from recipe detail (Story 007) | Must | US-001 |
| FR-002 | The system shall pass the recipe ID and current servings to cook mode | Must | US-001 |
| FR-003 | The system shall display cook mode in full-screen layout optimized for the kitchen (large text, minimal UI clutter) | Must | US-001 |
| FR-004 | The system shall display one recipe step at a time, centered and readable | Must | US-002 |
| FR-005 | The system shall display current step number (e.g., "Step 3 of 8") | Should | US-002 |
| FR-006 | The system shall provide "Previous" and "Next" buttons to navigate steps | Must | US-002 |
| FR-007 | The system shall support swipe navigation (left/right) to move between steps | Should | US-002 |
| FR-008 | The system shall display scaled ingredients (based on servings passed from detail view) at the top of the screen | Must | US-003 |
| FR-009 | The system shall make ingredient text readable at a glance (not a full detailed list, but a reference) | Should | US-003 |
| FR-010 | The system shall allow the user to exit cook mode and return to recipe detail | Should | US-001 |
| FR-011 | The system shall display a "Finished Cooking" button that marks the recipe as cooked (Story 023) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Enter Cook Mode
*(FR-001, FR-002, FR-003)*

```gherkin
Given the user is on recipe detail (Story 007)
When the user clicks "Start Cooking"
Then cook mode launches in full-screen
And the recipe ID and scaled servings are passed to cook mode
And the first step is displayed
```

### SC-002: Display One Step at a Time
*(FR-004, FR-005)*

```gherkin
Given the user is in cook mode for a recipe with 5 steps
When cook mode loads
Then the screen displays:
  - Step number: "Step 1 of 5"
  - Step text in large, readable font
  - Nothing else (no sidebar, minimal chrome)
```

### SC-003: Navigate to Next Step
*(FR-006, FR-007)*

```gherkin
Given the user is viewing Step 1
When the user clicks "Next" button
Then Step 2 is displayed
And step number updates to "Step 2 of 5"
When the user swipes right on mobile
Then Step 2 is also displayed
```

### SC-004: Navigate to Previous Step
*(FR-006, FR-007)*

```gherkin
Given the user is viewing Step 3
When the user clicks "Previous" button
Then Step 2 is displayed
When the user swipes left on mobile
Then Step 2 is also displayed
```

### SC-005: Last Step Navigation
*(FR-006)*

```gherkin
Given the user is viewing the last step (Step 5 of 5)
When the user clicks "Next"
Then the page does not advance (or wraps to first step, depending on design)
And "Finished Cooking" button is visible
```

### SC-006: Display Scaled Ingredients
*(FR-008, FR-009)*

```gherkin
Given the user is in cook mode for a recipe scaled to 4 servings
When cook mode loads
Then the top of the screen shows a reference list of scaled ingredients:
  | Scaled Ingredients |
  | 4 cups flour |
  | 2 eggs |
  | salt to taste |
  | [etc.] |
And the ingredient list is collapsible or scrollable if long
```

### SC-007: Exit Cook Mode
*(FR-010)*

```gherkin
Given the user is in cook mode
When the user clicks "Exit" or "Back"
Then cook mode closes
And the user returns to the recipe detail view
And no progress is lost (user can re-enter cook mode at any step)
```

### SC-008: Finish Cooking
*(FR-011)*

```gherkin
Given the user is in cook mode
When the user clicks "Finished Cooking"
Then a confirmation dialog appears: "Mark this recipe as cooked?"
When the user confirms
Then the recipe is recorded as cooked (Story 023)
And cook mode closes
And the user is returned to the recipe list or recipe detail
```

### SC-009: Swipe Navigation on Mobile
*(FR-007)*

```gherkin
Given the user is on a mobile device in cook mode
When the user swipes left (toward next step)
Then the current step slides out and the next step slides in
When the user swipes right (toward previous step)
Then the current step slides out and the previous step slides in
```

### SC-010: Very Long Step Text
*(FR-004)*

```gherkin
Given a recipe has a step with very long text (e.g., 1000 chars)
When the user is in cook mode viewing that step
Then the text is readable (wraps appropriately)
And the user can scroll within the step area if needed
Or the text is truncated with indication there's more
```

### SC-011: No Steps in Recipe
*(FR-004)*

```gherkin
Given a recipe has no steps (only ingredients)
When the user clicks "Start Cooking"
Then cook mode displays: "No steps for this recipe"
And a button appears: "View ingredients only" or "Exit"
```

### SC-012: Recipe Not Found
*(FR-001)*

```gherkin
Given the user tries to enter cook mode for a recipe that was deleted
When cook mode attempts to load
Then an error displays: "Recipe not found"
And a button allows the user to return to the recipe list
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
| servings | integer | required | Original servings |
| ingredients | array | at least one | Ingredients with quantity, unit, item, note |
| steps | array | optional | Cooking steps |

#### Step
_(Reused from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Step identifier |
| recipeId | UUID | FK | Parent recipe |
| text | string | required, max 1000 chars | Instruction text |
| displayOrder | integer | optional | Sort order |

#### CookSession
_(New for cook mode tracking)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Unique session ID |
| recipeId | UUID | FK | Recipe being cooked |
| servings | integer | required | Servings for this session |
| currentStepIndex | integer | optional | Current step (0-based); used to resume if needed |
| startedAt | datetime | generated | When cook mode started |
| completedAt | datetime | optional | When user clicked "Finished Cooking" |

### 5.2 Relationships

- A **Recipe** has zero or more **Steps** (one-to-many).
- A **CookSession** references one **Recipe** (many-to-one).

### 5.3 Value Objects

No new value objects needed for this story.

### 5.4 Domain Rules and Invariants

- **Cook mode is read-only**: Steps are not edited in cook mode; edits happen in Story 010.
- **Servings are fixed for the session**: The servings passed to cook mode do not change during cooking; scaling is done in detail view (Story 007).
- **Steps are immutable during cooking**: Step order and text do not change once cook mode starts.
- **Navigation bounds**: User cannot navigate before step 1 or after the last step (buttons disable or wrap, depending on design).

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Cook mode loads in < 500ms | User should enter cook flow quickly |
| NFR-002 | Responsiveness | Step navigation (next/previous, swipe) responds in < 100ms | Interaction should feel instant with wet hands |
| NFR-003 | Accessibility | Cook mode is touch-optimized; buttons are large and easily tapped | User has wet hands and may not be precise |
| NFR-004 | Readability | Step text is at least 18pt font, high contrast | User is reading from across kitchen |
| NFR-005 | Responsiveness | Cook mode is full-screen on mobile and tablet | Kitchen use is primarily mobile |
| NFR-006 | Reliability | Cook session state persists; user can exit and re-enter at the same step | User may be interrupted mid-recipe |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User navigates past the last step | "Next" button is disabled or wraps to first step; "Finished Cooking" is visible |
| EC-002 | User navigates before the first step | "Previous" button is disabled; user cannot go before step 1 |
| EC-003 | Recipe is deleted while user is in cook mode | Error message: "Recipe no longer available"; button to exit |
| EC-004 | Recipe has 0 steps | Cook mode displays: "No steps for this recipe"; offer to view ingredients or exit |
| EC-005 | Step text is very long (> 1000 chars) | Text wraps and is scrollable within the step area |
| EC-006 | Recipe has 1 step | "Step 1 of 1" displays; "Next" is disabled; "Finished Cooking" is visible after first step |
| EC-007 | User swipes rapidly between steps | Navigation is debounced; only one step change per gesture |
| EC-008 | Scaled ingredients list is very long (50+ items) | Ingredients are displayed in a scrollable section at the top |
| EC-009 | User exits cook mode without finishing | Cook session is not marked as cooked; user can re-enter at the same step later |
| EC-010 | Device is rotated (portrait ↔ landscape) | Cook mode reflows; current step is maintained; text remains readable |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-012) pass in CI |
| SC-002 | Cook mode loads in < 500ms |
| SC-003 | Step navigation (next/previous, swipe) responds in < 100ms |
| SC-004 | Step text is readable at 18pt+ font on mobile screens |
| SC-005 | Cook mode is full-screen on mobile and tablet |
| SC-006 | Scaled ingredients are displayed and accessible |
| SC-007 | "Finished Cooking" marks the recipe as cooked (Story 023) |
| SC-008 | Cook session state persists; user can exit and re-enter |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (recipes with steps must exist)
- Depends on Story 007 (entry point is "Start Cooking" button on detail page)
- Depends on Story 018 (scaling logic; passes scaled ingredients to cook mode)
- Feeds into Story 020 (screen wake lock is handled separately)
- Feeds into Story 021 (timer extraction from steps is handled separately)
- Feeds into Story 022 (ingredient checkoff is handled separately)
- Feeds into Story 023 (marks recipe as cooked when "Finished Cooking" is clicked)

### 9.2 Constraints

- Cook mode is display-only; no edits or modifications to the recipe
- Single-user, personal use (no multi-user cooking)
- Steps are plain text; no structured metadata (duration, temperature) in Story 019
- No timer or duration extraction in Story 019 (Story 021 adds that)
- No ingredient checkoff in Story 019 (Story 022 adds that)
- No screen wake lock in Story 019 (Story 020 adds that)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 3. System Scope and Context | User interactions during cooking |
| 5. Building Block View | Session state and recipe data retrieval |
| 8. Cross-Cutting Concepts | Responsive layout, touch handling, navigation patterns |
| 10. Quality Requirements | Readability, performance, accessibility for kitchen use |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should cook mode remember the current step if the user exits and re-enters? | UX | Open | Recommend yes; resume at the same step for better UX |
| 2 | On last step, should "Next" button wrap to first step or be disabled? | UX | Open | Recommend disabled; "Finished Cooking" is the next action |
| 3 | Should ingredient list be collapsible to give more screen space to step text? | UX | Open | Recommend collapsible; user can expand if needed for reference |

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
