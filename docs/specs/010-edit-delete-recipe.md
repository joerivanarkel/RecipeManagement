# Feature Specification: Edit or Delete Recipe

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 010 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Build the Collection (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Recipes change. Users make mistakes when entering them, discover better versions, or want to adjust quantities. Without edit capability, recipes are locked-in and frustrating. Without delete, unwanted recipes clutter the collection.

### 1.2 Goal

Allow users to edit all recipe fields (title, ingredients, steps, servings, photo) and delete recipes. Edits update the recipe immediately; deletes remove it from the collection. Both operations confirm intent to prevent accidents.

### 1.3 Non-Goals

- Recipe versioning or history
- Undo/redo for edits
- Bulk edit or batch delete
- Copy/duplicate recipe

---

## 2. User Stories

### US-001: Edit Recipe

**As a** home cook,
**I want** to edit a recipe's title, ingredients, steps, servings, or photo,
**so that** I can fix mistakes or adapt recipes to my preferences.

### US-002: Delete Recipe

**As a** home cook,
**I want** to delete a recipe I no longer want,
**so that** it doesn't clutter my collection.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display an "Edit Recipe" button on the recipe detail page (Story 007) | Must | US-001 |
| FR-002 | The system shall open an edit form pre-populated with all current recipe data | Must | US-001 |
| FR-003 | The system shall allow editing of: title, ingredients (all fields), steps, servings, and photo | Must | US-001 |
| FR-004 | The system shall validate edits using the same rules as Story 001 (required fields, max lengths, at least one ingredient) | Must | US-001 |
| FR-005 | The system shall persist edits to durable storage atomically (all-or-nothing) | Must | US-001 |
| FR-006 | The system shall update `updatedAt` timestamp when recipe is edited | Should | US-001 |
| FR-007 | The system shall warn user if they navigate away with unsaved changes | Should | US-001 |
| FR-008 | The system shall display a "Delete Recipe" button on the recipe detail page | Must | US-002 |
| FR-009 | The system shall require confirmation before deleting (dialog: "Are you sure? This cannot be undone.") | Must | US-002 |
| FR-010 | The system shall delete the recipe and all associated data (ingredients, steps, photo reference) | Must | US-002 |
| FR-011 | The system shall preserve cook log entries even if recipe is deleted (soft-delete or data audit trail) | Should | US-002 |
| FR-012 | The system shall redirect user to recipe list after successful delete | Should | US-002 |

---

## 4. Acceptance Scenarios

### SC-001: Edit Recipe Title
*(FR-001, FR-002, FR-003, FR-004, FR-005)*

```gherkin
Given the user is on a recipe detail page
When the user clicks "Edit Recipe"
Then the edit form opens with current data:
  | field | value |
  | title | Garlic Chicken Stir-Fry |
  | [ingredients] | [all current ingredients] |
  | [steps] | [all current steps] |
  | servings | 4 |
When the user changes title to "Garlic Chicken with Rice"
And clicks "Save Recipe"
Then the recipe is updated
And detail page shows new title
And `updatedAt` timestamp is changed
```

### SC-002: Edit Ingredients
*(FR-003, FR-004)*

```gherkin
Given the user is editing a recipe
When the user modifies ingredients:
  - Remove "soy sauce"
  - Change "2 cups chicken" to "3 cups chicken"
  - Add new ingredient: "ginger, 1 tbsp"
And clicks "Save Recipe"
Then the recipe ingredients are updated
And validation passes (at least one ingredient remains)
```

### SC-003: Edit Steps
*(FR-003)*

```gherkin
Given the user is editing a recipe
When the user modifies a step:
  - Changes "Simmer 5 minutes" to "Simmer 8 minutes"
And clicks "Save Recipe"
Then the recipe steps are updated
```

### SC-004: Edit Servings
*(FR-003)*

```gherkin
Given a recipe with servings=4
When the user edits servings to 6
And clicks "Save Recipe"
Then the recipe is updated
And future scaling (Story 007) uses 6 as the base
```

### SC-005: Edit Photo
*(FR-003)*

```gherkin
Given a recipe with an existing photo
When the user clicks "Change Photo"
And uploads a new image
And clicks "Save Recipe"
Then the old photo is replaced
And the new photo displays on the recipe
```

### SC-006: Remove Photo
*(FR-003)*

```gherkin
Given a recipe with a photo
When the user clicks "Remove Photo"
And clicks "Save Recipe"
Then the photo is removed
And the recipe displays placeholder on browse grid (Story 006)
```

### SC-007: Reject Invalid Edit
*(FR-004)*

```gherkin
Given the user is editing a recipe
When the user removes all ingredients
And clicks "Save Recipe"
Then the form displays: "At least one ingredient is required"
And the recipe is NOT updated
```

### SC-008: Warn on Unsaved Changes
*(FR-007)*

```gherkin
Given the user is editing a recipe with unsaved changes
When the user clicks "Back" or navigates away
Then a dialog appears: "You have unsaved changes. Discard?"
When the user confirms "Discard"
Then the edits are lost and user returns to detail page
```

### SC-009: Delete Recipe (Happy Path)
*(FR-008, FR-009, FR-010, FR-012)*

```gherkin
Given the user is on a recipe detail page
When the user clicks "Delete Recipe"
Then a confirmation dialog appears: "Are you sure? This cannot be undone."
When the user clicks "Confirm Delete"
Then the recipe is deleted
And all associated ingredients and steps are deleted
And the user is redirected to the recipe list (Story 006)
```

### SC-010: Cancel Delete
*(FR-009)*

```gherkin
Given the user is on a recipe detail page
When the user clicks "Delete Recipe"
And a confirmation dialog appears
When the user clicks "Cancel"
Then the dialog closes
And the recipe remains unchanged
```

### SC-011: Cook Log Preserved After Delete
*(FR-011)*

```gherkin
Given a recipe has been cooked 5 times (Story 023)
When the user deletes the recipe
Then the recipe is removed from the collection
And the cook log entries are preserved (for historical tracking)
```

### SC-012: Edit Multiple Fields
*(FR-003, FR-005)*

```gherkin
Given the user is editing a recipe
When the user changes:
  - Title
  - Servings
  - Ingredients
  - Steps
  - Photo
And clicks "Save Recipe"
Then all changes are persisted atomically
And if any change fails, the entire edit fails
```

---

## 5. Domain Model

### 5.1 Entities

#### Recipe
_(Extended from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Recipe identifier |
| title | string | required, max 200 chars | Recipe name |
| servings | integer | required, > 0 | Serving size |
| ingredients | array | at least one | Ingredients with quantity, unit, item, note |
| steps | array | optional | Cooking steps |
| photoId | UUID | optional | Reference to recipe photo |
| createdAt | datetime | generated, immutable | When recipe was created |
| updatedAt | datetime | generated | When recipe was last edited |
| isDeleted | boolean | optional | Soft-delete flag (if using soft deletes) |

### 5.2 Relationships

- A **Recipe** has many **Ingredients** (one-to-many).
- A **Recipe** has many **Steps** (one-to-many).
- A **Recipe** has zero or one **Photo** (one-to-one, optional).
- A **Recipe** has many **CookLogEntries** (one-to-many; preserved on delete).

### 5.3 Value Objects

No new value objects needed for this story.

### 5.4 Domain Rules and Invariants

- **Edit form mirrors creation form**: Story 010 edit uses the same validation as Story 001 add.
- **Atomic update**: All recipe fields (ingredients, steps, photo) are updated together or not at all.
- **`updatedAt` is required**: Every edit updates this timestamp.
- **Cook log is preserved**: Deleting a recipe does not delete its cook log (Story 023 data survives).
- **Soft or hard delete**: Clarify if delete is hard (data gone) or soft (marked isDeleted but queryable).

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Edit form loads in < 200ms | Pre-population should be fast |
| NFR-002 | Performance | Recipe update persists in < 500ms | Edits should feel responsive |
| NFR-003 | Data Integrity | Edit is atomic (all-or-nothing) | Partial edits corrupt the recipe |
| NFR-004 | Reliability | `updatedAt` timestamp is always accurate | Audit trail must reflect actual edit time |
| NFR-005 | Accessibility | Edit form is keyboard-navigable (same as Story 001) | User may have accessibility needs |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User edits recipe but network fails mid-save | Rollback; display error; user retries |
| EC-002 | User deletes recipe while another browser tab has the detail page open | Deleted recipe shows error when refreshed; redirect to list |
| EC-003 | User edits ingredient to have no item (empty) | Form rejects; ingredient must have item |
| EC-004 | User edits title to exceed 200 chars | Form rejects or truncates with warning |
| EC-005 | User removes all ingredients | Form rejects; at least one ingredient required |
| EC-006 | User edits recipe; another user (future feature) is viewing same recipe | [Out of scope for single-user MVP] |
| EC-007 | Photo upload fails during edit | Recipe updates without photo; error shown for photo |
| EC-008 | User attempts to delete recipe that was already deleted | Error: "Recipe not found"; redirect to list |
| EC-009 | Edit form has unsaved changes; browser tab crashes | On recovery, unsaved changes are lost; user must re-edit |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-012) pass in CI |
| SC-002 | Edit form loads in < 200ms |
| SC-003 | Recipe update persists in < 500ms |
| SC-004 | All recipe fields are editable (title, ingredients, steps, servings, photo) |
| SC-005 | Edit validation matches Story 001 (required fields, max lengths, at least one ingredient) |
| SC-006 | Edit is atomic (all-or-nothing) |
| SC-007 | Delete requires confirmation |
| SC-008 | Deleted recipe is removed from collection |
| SC-009 | Cook log is preserved when recipe is deleted |
| SC-010 | User is warned of unsaved changes |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (edit form mirrors add form; uses same domain model)
- Depends on Story 007 (edit/delete buttons on recipe detail page)
- Feeds into Story 006 (deleted recipes no longer appear in browse grid)
- Related to Story 023 (cook log is preserved on delete)

### 9.2 Constraints

- Single-user, personal use (no multi-user conflict handling)
- Edit form mirrors Story 001 validation (reuse logic)
- No recipe versioning or history (edits overwrite previous version)
- No undo/redo
- Delete is confirmed but not reversible (no soft-delete user recovery)
- Photo deletion follows Story 001 patterns (optional, can be cleared)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Recipe update and delete patterns; data persistence |
| 8. Cross-Cutting Concepts | Validation (reuse from Story 001), atomic updates, timestamps |
| 10. Quality Requirements | Performance and data integrity during edit/delete |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should delete be hard (data gone) or soft (marked isDeleted)? | Architecture | Open | Recommend hard delete with cook log preservation via FK constraints |
| 2 | Should edit preserve original `createdAt` or update it? | Product | Open | Recommend preserve `createdAt`, update only `updatedAt` |
| 3 | If user edits a recipe after it's been cooked, should cook log reflect old or new recipe? | Product | Open | Recommend cook log reflects recipe state at time of cooking (no retro-changes) |

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
