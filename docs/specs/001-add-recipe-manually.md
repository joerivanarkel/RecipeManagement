# Feature Specification: Add a Recipe by Hand

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 001 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Capture Recipes (Story Map) |
| Arc42 reference | 3, 5, 8, 10 |

### 1.1 Problem Statement

A home cook has recipes scattered across cookbooks, printed pages, and handwritten notes. Copying them into a digital form is tedious and error-prone — most existing recipe apps require meticulous typing or parsing, treating entry as a chore rather than the first step toward a working collection.

### 1.2 Goal

Enable a cook to quickly add a recipe by hand from a physical source. The recipe must capture title, ingredients (with quantity, unit, item, and optional notes), free-text steps, servings, and an optional photo. Entry should be fast and forgiving, with flexible fields (only ingredient item is required). The structured ingredient model unlocks downstream features: fridge-based matching (Story 014) and serving-size scaling (Story 018).

### 1.3 Non-Goals

- Parsing or importing from external sources (URLs, OCR, text blobs — those are separate stories)
- Multi-language recipe support
- Nutritional information or allergen tracking
- Sharing or collaborative editing
- Auto-generating shopping lists from ingredients

---

## 2. User Stories

### US-001: Add a Recipe by Hand

**As a** home cook,
**I want** to quickly add a recipe from a physical source (cookbook, clipping, note) by entering its title, ingredients (with flexible structure), steps, servings, and an optional photo,
**so that** I can build a digital collection without tedious typing and unlock downstream features like fridge-based suggestions and scaling.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a form for entering a new recipe | Must | US-001 |
| FR-002 | The system shall accept and store a recipe title (required field, max 200 chars) | Must | US-001 |
| FR-003 | The system shall accept and store a default servings count (required field, numeric, > 0) | Must | US-001 |
| FR-004 | The system shall accept and store zero or more ingredients, each with: quantity (optional), unit (optional), item (required), note (optional) | Must | US-001 |
| FR-005 | The system shall accept and store zero or more steps as free-text lines (max 1000 chars each) | Must | US-001 |
| FR-006 | The system shall accept and store an optional photo (image file, supported formats: JPG, PNG, GIF; max 5MB) | Should | US-001 |
| FR-007 | The system shall validate that a recipe has at least a title, servings, and one ingredient before saving | Must | US-001 |
| FR-008 | The system shall persist a new recipe to durable storage atomically (all-or-nothing) | Must | US-001 |
| FR-009 | The system shall provide feedback (success message) when a recipe is saved | Should | US-001 |
| FR-010 | The system shall allow the user to add/remove ingredient rows on the fly while filling the form | Should | US-001 |
| FR-011 | The system shall allow the user to add/remove step rows on the fly while filling the form | Should | US-001 |
| FR-012 | The system shall warn the user if they attempt to navigate away with unsaved form data | Should | US-001 |
| FR-013 | The system shall allow recipe save to succeed even if photo upload fails (photo is optional) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Add a Recipe (Happy Path)
*(FR-001, FR-002, FR-003, FR-004, FR-005, FR-008, FR-009)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | title | Garlic Chicken Stir-Fry |
  | servings | 4 |
  | ingredient 1 (item) | chicken breast |
  | ingredient 1 (quantity) | 2 |
  | ingredient 1 (unit) | pieces |
  | ingredient 2 (item) | garlic |
  | ingredient 2 (quantity) | 3 |
  | ingredient 2 (unit) | cloves |
  | ingredient 2 (note) | minced |
  | step 1 | Heat oil in a wok over high heat |
  | step 2 | Add chicken and cook until golden |
  | step 3 | Add garlic and cook for 1 minute |
And the user clicks "Save Recipe"
Then the recipe is persisted to storage
And a success message appears
And the user is returned to the recipe list
```

### SC-002: Add a Recipe Without Optional Fields
*(FR-004, FR-006, FR-010, FR-011)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | title | Simple Pasta |
  | servings | 2 |
  | ingredient 1 (item) | pasta |
  | ingredient 1 (quantity) | 500 |
  | ingredient 1 (unit) | grams |
  | ingredient 2 (item) | salt |
And the user does NOT upload a photo
And the user does NOT fill in ingredient notes
And the user clicks "Save Recipe"
Then the recipe is persisted with no photo
And the recipe is persisted with empty notes
And a success message appears
```

### SC-003: Reject Incomplete Recipe (Missing Ingredients)
*(FR-007)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | title | My Recipe |
  | servings | 4 |
And the user clicks "Save Recipe" without entering any ingredients
Then the form displays a validation error: "At least one ingredient is required"
And the recipe is NOT persisted
```

### SC-004: Reject Missing Title
*(FR-007)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | servings | 2 |
  | ingredient 1 (item) | eggs |
And the user clicks "Save Recipe" without entering a title
Then the form displays a validation error: "Title is required"
And the recipe is NOT persisted
```

### SC-005: Reject Invalid Servings (Non-Numeric)
*(FR-003, FR-007)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | title | Soup |
  | servings | abc |
And the user clicks "Save Recipe"
Then the form displays a validation error: "Servings must be a valid number"
And the recipe is NOT persisted
```

### SC-006: Reject Invalid Servings (Zero or Negative)
*(FR-003, FR-007)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | title | Soup |
  | servings | 0 |
And the user clicks "Save Recipe"
Then the form displays a validation error: "Servings must be greater than 0"
And the recipe is NOT persisted
```

### SC-007: Add/Remove Ingredient Rows
*(FR-010)*

```gherkin
Given the user is on the "Add Recipe" page with 2 ingredient rows visible
When the user clicks "Add Ingredient"
Then a new blank ingredient row appears
When the user clicks "Remove" on the first row
Then that row is deleted and the remaining ingredients shift up
```

### SC-008: Add/Remove Step Rows
*(FR-011)*

```gherkin
Given the user is on the "Add Recipe" page with 3 step rows visible
When the user clicks "Add Step"
Then a new blank step row appears at the end
When the user clicks "Remove" on the second step
Then that step is deleted and the remaining steps shift up
```

### SC-009: Ingredient With Quantity Only (No Unit)
*(FR-004)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters an ingredient:
  | field | value |
  | item | salt |
  | quantity | 1 |
  | unit | [empty] |
  | note | to taste |
And the user clicks "Save Recipe" with valid title and servings
Then the recipe is persisted with the ingredient exactly as entered
And the quantity and note are preserved
```

### SC-010: Ingredient With Item Only (No Quantity or Unit)
*(FR-004)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters an ingredient:
  | field | value |
  | item | lettuce |
  | quantity | [empty] |
  | unit | [empty] |
  | note | [empty] |
And the user clicks "Save Recipe" with valid title and servings
Then the recipe is persisted with the ingredient as-is
```

### SC-011: Reject Ingredient With No Item
*(FR-004, FR-007)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters an ingredient:
  | field | value |
  | item | [empty] |
  | quantity | 2 |
  | unit | cups |
And the user clicks "Save Recipe"
Then the form displays a validation error: "Every ingredient must have an item"
And the recipe is NOT persisted
```

### SC-012: Reject Whitespace-Only Step
*(FR-005)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters:
  | field | value |
  | title | Recipe |
  | servings | 2 |
  | ingredient 1 (item) | flour |
  | step 1 |    [whitespace only] |
And the user clicks "Save Recipe"
Then the form either trims whitespace or displays an error: "Step cannot be empty"
And the recipe is NOT persisted (or step is auto-trimmed and removed)
```

### SC-013: Reject Title Exceeding Max Length
*(FR-002)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters a title longer than 200 characters
And the user clicks "Save Recipe"
Then the form displays a validation error: "Title is too long (max 200 characters)"
And the recipe is NOT persisted
```

### SC-014: Reject Photo File Too Large
*(FR-006)*

```gherkin
Given the user is on the "Add Recipe" page
When the user selects a photo file larger than 5MB
Then the form displays an error: "File too large (max 5MB)"
And the file is NOT uploaded
```

### SC-015: Reject Invalid Photo Format
*(FR-006)*

```gherkin
Given the user is on the "Add Recipe" page
When the user selects a non-image file (e.g., .txt, .pdf)
Then the form displays an error: "Invalid file format. Supported: JPG, PNG, GIF"
And the file is NOT uploaded
```

### SC-016: Recipe Saves Even If Photo Upload Fails
*(FR-013)*

```gherkin
Given the user is on the "Add Recipe" page
When the user uploads a valid photo
And the photo upload fails due to storage error
And the user clicks "Save Recipe" with valid title, servings, and ingredients
Then the recipe is persisted successfully
And the photoId is null or empty
And the user sees a warning: "Recipe saved, but photo upload failed. You can add a photo later."
```

### SC-017: Warn on Navigation Without Save
*(FR-012)*

```gherkin
Given the user is on the "Add Recipe" page
When the user enters data into the form
And the user clicks the back button or navigates away
Then the browser displays a confirmation dialog: "You have unsaved changes. Leave anyway?"
When the user selects "Leave"
Then the unsaved data is discarded
When the user selects "Cancel"
Then the user remains on the form
```

### SC-018: Network Failure During Save
*(FR-008)*

```gherkin
Given the user is on the "Add Recipe" page with valid data
When the user clicks "Save Recipe"
And the network connection fails before the save completes
Then the form displays an error: "Failed to save recipe. Please try again."
And the recipe is NOT persisted
And the form retains the user's data for re-submission
```

---

## 5. Domain Model

### 5.1 Entities

#### Recipe

Represents a single recipe that a cook has added to their collection. This is the core aggregate root for the manual entry feature.

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Unique identifier for the recipe |
| title | string | required, max 200 chars | Recipe name (e.g., "Garlic Chicken Stir-Fry") |
| servings | integer | required, > 0 | Default serving size; used for scaling (Story 018) |
| ingredients | array of Ingredient | at least one required | List of ingredient entries |
| steps | array of Step | optional | List of cooking instructions |
| photoId | UUID | optional, FK to Photo | Reference to an optional recipe photo |
| createdAt | datetime | generated, immutable | When the recipe was added |
| updatedAt | datetime | generated | When the recipe was last edited |

#### Ingredient

Represents a single ingredient within a recipe. Structured to unlock fridge matching (Story 014) and scaling (Story 018).

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Unique identifier within the recipe |
| recipeId | UUID | required, FK to Recipe | Parent recipe |
| quantity | decimal | optional | Numeric quantity (e.g., 2, 0.5, 3.5) |
| unit | string | optional, max 50 chars | Unit of measurement (e.g., "cloves", "cup", "grams") |
| item | string | required, max 200 chars | The ingredient itself (e.g., "garlic", "chicken breast") |
| note | string | optional, max 300 chars | Optional preparation note (e.g., "minced", "to taste") |
| displayOrder | integer | optional | Sort order within the recipe (allows reordering) |

#### Step

Represents a single cooking step. Kept simple — free text for now. Extensible for future metadata (duration, temperature, etc. in Story 021).

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Unique identifier within the recipe |
| recipeId | UUID | required, FK to Recipe | Parent recipe |
| text | string | required, max 1000 chars | The instruction text |
| displayOrder | integer | optional | Sort order within the recipe |

#### Photo

Represents an optional photo associated with a recipe.

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Unique identifier |
| recipeId | UUID | required, FK to Recipe | Parent recipe |
| filename | string | required | Original filename (for reference) |
| mimeType | string | required | Content type (e.g., "image/jpeg") |
| size | integer | optional | File size in bytes |
| storedAt | string | required | Reference to storage location (path, URL, or key) |
| uploadedAt | datetime | generated | When the photo was uploaded |

### 5.2 Relationships

- A **Recipe** has many **Ingredients** (one-to-many). Every ingredient belongs to exactly one recipe.
- A **Recipe** has many **Steps** (one-to-many). Every step belongs to exactly one recipe.
- A **Recipe** has zero or one **Photo** (one-to-one, optional). A photo belongs to exactly one recipe.

### 5.3 Value Objects

No value objects needed for this story — the domain is straightforward.

### 5.4 Domain Rules and Invariants

- **Valid recipe**: A recipe must have a title, a servings count > 0, and at least one ingredient before it can be persisted.
- **Item is required**: Every ingredient must have an item; quantity, unit, and note are all optional.
- **Ingredient constraints**:
  - If `quantity` is provided, it must be a positive number.
  - If `unit` is provided without `quantity`, it is semantically valid (e.g., "a pinch of salt" might be stored as quantity=empty, unit=empty, item="salt", note="a pinch").
- **Display order**: Both Ingredient and Step have `displayOrder` to preserve the order the user entered them. No business logic depends on order yet, but it's preserved for UX.
- **Photo is optional**: A recipe can exist without a photo. The presence of `photoId` is not validated.
- **Immutable creation**: `createdAt` is immutable; only `updatedAt` changes on edits (relevant for Story 010 and future audit trails).
- **Whitespace handling**: Step text and ingredient items are trimmed of leading/trailing whitespace. Empty steps are not persisted.

---

## 6. Non-Functional Requirements

These are specific to Story 001. Project-wide NFRs (error handling, logging, auth) live in arc42 Section 10 and will be referenced, not duplicated.

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Recipe save completes in < 500ms at p95 | Form submission shouldn't feel sluggish; user is copying from a source |
| NFR-002 | Data Integrity | Recipe is atomically persisted (all-or-nothing) | Partial saves corrupt the domain model and cascade to downstream stories |
| NFR-003 | Accessibility | Form fields are keyboard-navigable; labels associated with inputs | User may be copying while holding a cookbook; keyboard-only workflow must work |
| NFR-004 | Storage | Photos are validated before upload (format, size limit) | Prevent storage bloat and format mismatches; reasonable limit ~5MB |
| NFR-005 | Resilience | If photo upload fails, the recipe itself still saves | Photo is optional; a failed upload shouldn't block the recipe |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User enters very long title (> 200 chars) | Form rejects with error; title field shows remaining character count |
| EC-002 | User enters 0 or negative servings | Form rejects with error: "Servings must be > 0" |
| EC-003 | User enters non-numeric servings | Form rejects with error: "Servings must be a valid number" |
| EC-004 | User adds 50+ ingredients | Form allows it; UI may paginate or scroll. No hard limit enforced (future NFR if needed) |
| EC-005 | User uploads a 50MB photo | Photo upload rejected with error: "File too large (max 5MB)" |
| EC-006 | User uploads a non-image file (e.g., .txt) | Upload rejected with error: "Invalid file format. Supported: JPG, PNG, GIF" |
| EC-007 | Network fails during recipe save | Form displays error: "Failed to save recipe. Please try again." Recipe is not persisted. |
| EC-008 | User navigates away without saving | Browser shows unsaved-changes confirmation dialog (see SC-017) |
| EC-009 | User tries to add an ingredient with only a note, no item | Form rejects on save with error: "Every ingredient must have an item" |
| EC-010 | User adds an ingredient with quantity but no unit | Allowed; saves as-is (e.g., "2" with no unit is valid) |
| EC-011 | User adds a step with only whitespace (e.g., "   ") | Step is treated as empty; form warns or auto-trims |
| EC-012 | User pastes very long text into a step field (> 1000 chars) | Form either truncates or shows error: "Step text is too long (max 1000 chars)" |
| EC-013 | Photo upload succeeds but storage fails silently | Recipe saves anyway with no photo reference (see SC-016) |
| EC-014 | Duplicate recipe title | No restriction; user can have multiple recipes with the same title (titles are not unique identifiers) |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-018) pass in CI |
| SC-002 | Form validates all required fields and prevents incomplete recipes from being saved |
| SC-003 | Recipe persists to durable storage and survives a page reload |
| SC-004 | Photo upload works for JPG/PNG/GIF files up to 5MB; larger or invalid files are rejected with clear errors |
| SC-005 | Recipe save completes in < 500ms at p95 |
| SC-006 | Form is fully keyboard-navigable and accessible (WCAG 2.1 AA minimum) |
| SC-007 | Recipe domain model is correctly persisted with all fields intact (tested via SC-001) |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Story 006 (Browse recipes) depends on this story — recipes must exist before they can be browsed.
- Story 010 (Edit/delete recipe) will reuse this domain model.
- Story 014 (Fridge matching) depends on the structured ingredient model established here.
- Story 018 (Scaling) depends on the structured ingredient model.

### 9.2 Constraints

- Single-user, personal use only (no multi-user locking required).
- Photo storage backend is decided separately (arc42 Section 5); this spec is agnostic to file storage mechanism.
- Recipe title and ingredient item are human-entered, not normalized or de-duplicated.
- Atomic persistence is required (all-or-nothing saves); implement with database transactions or equivalent.

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 3. System Scope and Context | Defines system boundary (recipe management for a single cook) |
| 5. Building Block View | Specifies where recipes are persisted and how photos are stored |
| 8. Cross-Cutting Concepts | Error handling, validation, and logging patterns |
| 10. Quality Requirements | Project-wide performance, accessibility, and data-integrity expectations |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Where are photos physically stored (local file, S3, database BLOB)? | Architecture | Open | Decide in arc42 Section 5; implement in Photo storage layer |
| 2 | Should the form auto-save drafts, or only save on explicit "Save" click? | Product | Open | Defer to Story 001 implementation or user feedback post-MVP |
| 3 | Should recipe title be case-insensitive unique (e.g., "Pasta" and "pasta" treated as duplicates)? | Product | Open | Defer; no uniqueness constraint in Story 001 |

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
