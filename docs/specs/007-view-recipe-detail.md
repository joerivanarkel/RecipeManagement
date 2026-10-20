# Feature Specification: View Recipe Detail

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 007 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Build the Collection (Story Map) |
| Arc42 reference | 3, 5, 8, 10 |

### 1.1 Problem Statement

A cook has clicked on a recipe from the grid (Story 006) and needs to see its full content — ingredients and steps — to decide whether to cook it or edit it. The detail view is the hub for cooking (Story 019), editing (Story 010), and scaling (Story 018).

### 1.2 Goal

Display a full recipe with title, photo, ingredients (prose list), steps (prose), and servings. Provide a servings slider that recalculates ingredient quantities live. Include buttons to start cooking, edit, or delete the recipe.

### 1.3 Non-Goals

- Cook time or prep time fields (not captured in Story 001)
- Nutritional information
- Recipe notes or comments
- Sharing or exporting

---

## 2. User Stories

### US-001: View Recipe Details

**As a** home cook,
**I want** to see the full ingredients, steps, and servings for a recipe,
**so that** I can decide whether to cook it or edit it.

### US-002: Scale Recipe Servings

**As a** home cook,
**I want** to adjust the serving size on the detail page and see ingredients recalculate instantly,
**so that** I can adapt the recipe to my needs without opening an edit form.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a recipe detail page when accessed by recipe ID | Must | US-001 |
| FR-002 | The system shall display recipe title prominently at the top | Must | US-001 |
| FR-003 | The system shall display the recipe photo, or a placeholder if none exists | Should | US-001 |
| FR-004 | The system shall display all ingredients as a prose list formatted as: `[quantity] [unit] [item], [note]` with graceful handling of empty fields | Must | US-001 |
| FR-005 | The system shall display all steps as a prose paragraph list (one step per paragraph) | Must | US-001 |
| FR-006 | The system shall display the current servings size with a slider to adjust from 1 to a reasonable maximum (e.g., 20) | Must | US-002 |
| FR-007 | The system shall recalculate ingredient quantities based on the selected servings in real-time | Must | US-002 |
| FR-008 | The system shall NOT scale ingredients that have no quantity (e.g., "salt to taste" remains unchanged) | Must | US-002 |
| FR-009 | The system shall display "Start Cooking" button that launches step-by-step cook mode (Story 019) | Should | US-001 |
| FR-010 | The system shall display "Edit Recipe" button that opens the recipe edit form (Story 010) | Should | US-001 |
| FR-011 | The system shall display "Delete Recipe" button that prompts for confirmation (Story 010) | Should | US-001 |
| FR-012 | The system shall preserve the original recipe servings; scaling is display-only and not persisted | Must | US-002 |
| FR-013 | The system shall handle scaling edge cases: quantities with ranges (e.g., "2-3"), decimal quantities, and fractional units (e.g., "1/2 cup") | Should | US-002 |

---

## 4. Acceptance Scenarios

### SC-001: View Recipe Detail (Happy Path)
*(FR-001, FR-002, FR-003, FR-004, FR-005, FR-006)*

```gherkin
Given the user clicks on a recipe from the grid (Story 006)
When the detail page loads
Then the recipe title is displayed prominently
And the recipe photo is displayed (or placeholder if none)
And all ingredients are listed in prose format (e.g., "2 cups flour, sifted")
And all steps are displayed as numbered paragraphs
And the current servings size is shown (e.g., "Servings: 4")
And a slider is displayed to adjust servings
```

### SC-002: Scale Servings Up
*(FR-006, FR-007, FR-012)*

```gherkin
Given the user is viewing a recipe with:
  | ingredient | |
  | 2 cups flour | |
  | 1/2 cup milk | |
  | salt to taste | |
When the user adjusts the servings slider from 2 to 4
Then the ingredients recalculate:
  | ingredient | |
  | 4 cups flour | |
  | 1 cup milk | |
  | salt to taste | [unchanged, no quantity] |
And the page refreshes immediately without a save
And the original recipe data remains unchanged
```

### SC-003: Scale Servings Down
*(FR-006, FR-007)*

```gherkin
Given the user is viewing a recipe with servings set to 4
And ingredients showing 4 cups flour, 2 eggs, 1 tsp salt to taste
When the user adjusts the servings slider from 4 to 1
Then the ingredients recalculate:
  | ingredient | |
  | 1 cup flour | |
  | 0.5 eggs | [or "1/2 egg"] |
  | salt to taste | [unchanged] |
And the display is mathematically correct
```

### SC-004: Ingredient With Only Quantity (No Unit)
*(FR-004, FR-007)*

```gherkin
Given a recipe has an ingredient: quantity=2, unit=[empty], item=chicken breasts, note=[empty]
When the user views the detail page at default servings (2)
Then the ingredient displays as: "2 chicken breasts"
When the user scales to 4 servings
Then it displays as: "4 chicken breasts"
```

### SC-005: Ingredient With Only Item (No Quantity or Unit)
*(FR-004, FR-007, FR-008)*

```gherkin
Given a recipe has an ingredient: quantity=[empty], unit=[empty], item=lettuce, note=[empty]
When the user views the detail page
Then the ingredient displays as: "lettuce"
When the user scales servings
Then it remains: "lettuce" [no scaling applied]
```

### SC-006: Start Cooking Button
*(FR-009)*

```gherkin
Given the user is viewing a recipe detail page
When the user clicks "Start Cooking"
Then the app transitions to step-by-step cook mode (Story 019)
And the recipe ID and current servings are passed to cook mode
```

### SC-007: Edit Recipe Button
*(FR-010)*

```gherkin
Given the user is viewing a recipe detail page
When the user clicks "Edit Recipe"
Then the app opens the recipe edit form (Story 010)
And the form is pre-populated with the current recipe data
```

### SC-008: Delete Recipe Button
*(FR-011)*

```gherkin
Given the user is viewing a recipe detail page
When the user clicks "Delete Recipe"
Then a confirmation dialog appears: "Are you sure? This cannot be undone."
When the user clicks "Confirm"
Then the recipe is deleted
And the user is redirected to the recipe list (Story 006)
```

### SC-009: Recipe With No Photo
*(FR-003)*

```gherkin
Given a recipe has no photo
When the user views the detail page
Then a placeholder image displays
And the recipe content is fully readable
```

### SC-010: Fractional Quantities Scale Correctly
*(FR-013)*

```gherkin
Given a recipe has ingredients:
  | quantity | unit | item |
  | 1/2 | cup | sugar |
  | 1.5 | tsp | baking powder |
When the user scales from 2 to 4 servings
Then ingredients recalculate:
  | quantity | unit | item |
  | 1 | cup | sugar |
  | 3 | tsp | baking powder |
```

### SC-011: Range Quantities Scale Correctly
*(FR-013)*

```gherkin
Given a recipe has an ingredient: quantity="2-3", unit="cloves", item="garlic"
When the user scales from 2 to 4 servings
Then the ingredient displays as: "4-6 cloves garlic" [both bounds scaled]
```

### SC-012: Scaling Does Not Persist
*(FR-012)*

```gherkin
Given the user scales a recipe to 6 servings on the detail page
When the user navigates away and returns to the recipe
Then the recipe is back to its original servings
```

### SC-013: Recipe Not Found
*(FR-001)*

```gherkin
Given the user tries to access a recipe detail with an invalid or deleted recipe ID
When the page attempts to load
Then the app displays: "Recipe not found"
And a link appears to return to the recipe list (Story 006)
```

### SC-014: Zero or Negative Servings Slider
*(FR-006)*

```gherkin
Given the user is viewing a recipe detail
When the user attempts to set servings to 0 or a negative number via the slider
Then the slider does not allow values < 1
And servings remains at the previous valid value
```

---

## 5. Domain Model

### 5.1 Entities

#### Recipe
_(Reused from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Unique recipe identifier |
| title | string | required, max 200 chars | Recipe name |
| servings | integer | required, > 0 | Default/original serving size |
| ingredients | array of Ingredient | at least one | Ingredients with quantity, unit, item, note |
| steps | array of Step | optional | Cooking steps |
| photoId | UUID | optional | Reference to recipe photo |
| createdAt | datetime | generated | When recipe was added |
| updatedAt | datetime | generated | When recipe was last edited |

#### Ingredient
_(Reused from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Ingredient identifier |
| recipeId | UUID | FK | Parent recipe |
| quantity | decimal | optional | Numeric quantity (can be null or range string like "2-3") |
| unit | string | optional, max 50 chars | Unit of measurement (e.g., "cups", "cloves") |
| item | string | required, max 200 chars | The ingredient itself |
| note | string | optional, max 300 chars | Preparation note (e.g., "to taste", "minced") |
| displayOrder | integer | optional | Sort order |

#### Step
_(Reused from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Step identifier |
| recipeId | UUID | FK | Parent recipe |
| text | string | required, max 1000 chars | Instruction text |
| displayOrder | integer | optional | Sort order |

#### Photo
_(Reused from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Photo identifier |
| recipeId | UUID | FK | Parent recipe |
| filename | string | required | Original filename |
| mimeType | string | required | Content type |
| storedAt | string | required | Storage reference |

### 5.2 Relationships

- A **Recipe** has zero or one **Photo** (one-to-one, optional).
- A **Recipe** has one or more **Ingredients** (one-to-many).
- A **Recipe** has zero or more **Steps** (one-to-many).

### 5.3 Value Objects

#### ScaledQuantity

A quantity that can scale: either a decimal or a range (e.g., "2-3"). Used during scaling calculations.

| Attribute | Type | Constraints |
| --- | --- | --- |
| original | string | "2" or "2-3" or "1/2" |
| scaleFactor | decimal | servingsRequested / originalServings |
| scaled | string | Result of scaling the original |

### 5.4 Domain Rules and Invariants

- **Original servings immutable**: The recipe's `servings` field is never changed by scaling; only the display is affected.
- **Quantity scaling rule**: If `quantity` is null or empty, the ingredient is not scaled (e.g., "salt to taste" remains unchanged).
- **Range scaling**: Quantities like "2-3" are scaled as both bounds (e.g., "2-3" × 2 = "4-6").
- **Fractional arithmetic**: Quantities like "1/2" or "0.5" are scaled mathematically and can result in decimal or fractional displays (e.g., "1.5" or "3/2").
- **Display-only scaling**: Scaling is a view concern, not persisted to the domain model.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Recipe detail page loads in < 500ms | User should see recipe immediately after clicking from grid |
| NFR-002 | Responsiveness | Servings slider recalculates quantities in < 100ms | Live interaction should feel instant |
| NFR-003 | Accessibility | Page is keyboard-navigable; buttons and slider are operable via keyboard | User may be in kitchen with limited mouse access |
| NFR-004 | Responsiveness | Detail page is readable on mobile, tablet, desktop (responsive layout) | Users access recipes from various devices |
| NFR-005 | Reliability | Scaling calculations are mathematically accurate for decimals, fractions, and ranges | Incorrect scaled quantities could ruin the dish |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Recipe ID does not exist | Display "Recipe not found" with link back to list |
| EC-002 | Recipe is deleted while user is viewing it | Display "Recipe no longer available" |
| EC-003 | Servings slider set to 0 or negative | Slider prevents values < 1; stays at previous valid value |
| EC-004 | Servings slider set to very large number (e.g., 1000) | UI limits to reasonable max (e.g., 20); ingredients scale correctly |
| EC-005 | Ingredient quantity is "2-3" (range) | Scales both bounds (e.g., "2-3" × 2 = "4-6") |
| EC-006 | Ingredient quantity is fractional ("1/2 cup") | Scales as decimal (0.5 × 2 = 1) and displays appropriately |
| EC-007 | Ingredient quantity has no decimal places ("2 cups") | Scales as integer and displays without decimals (2 × 2 = 4) |
| EC-008 | Ingredient quantity results in very small decimal (e.g., "0.1") | Rounds to reasonable precision or displays as fraction (e.g., "1/10") |
| EC-009 | User scales, then navigates away, then returns | Recipe is back to original servings |
| EC-010 | Photo fails to load | Placeholder displays; content is fully readable |
| EC-011 | Recipe has 0 steps | Page displays ingredients and servings only; steps area is empty |
| EC-012 | Recipe has extremely long ingredient note (300+ chars) | Note wraps or truncates gracefully |
| EC-013 | Recipe has 50+ ingredients | Page scrolls; all ingredients are accessible |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-014) pass in CI |
| SC-002 | Recipe detail page loads in < 500ms for any recipe |
| SC-003 | Servings slider recalculates quantities in < 100ms |
| SC-004 | Scaling calculations are mathematically correct for integers, decimals, fractions, and ranges |
| SC-005 | Scaled view does not persist; recipe data is unchanged |
| SC-006 | "Start Cooking", "Edit", and "Delete" buttons navigate/function correctly |
| SC-007 | Page is readable and functional on mobile, tablet, and desktop |
| SC-008 | Invalid recipe ID displays appropriate error message |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (recipes must exist)
- Depends on Story 006 (users navigate here from the grid)
- Feeds into Story 010 (edit button opens edit form)
- Feeds into Story 019 (cook button starts cook mode, passing servings)
- Feeds into Story 018 (scaling logic lives here, but also in cook mode for Story 019)

### 9.2 Constraints

- Single-user, personal use (no multi-user access control)
- Scaling is display-only; original recipe data is immutable
- No recipe-level cook time or prep time fields (not captured in Story 001)
- Photo storage backend is decided separately (arc42 Section 5)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 3. System Scope and Context | User interactions with recipe data |
| 5. Building Block View | Photo storage and retrieval; recipe data fetching |
| 8. Cross-Cutting Concepts | Error handling, responsive layout patterns |
| 10. Quality Requirements | Performance (< 500ms load, < 100ms scaling), accessibility |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | How should fractional results display? (e.g., "0.33 cup" or "1/3 cup"?) | UX/Math | Open | Recommend sensible rounding and fraction display (e.g., 1/3, 1/2, 2/3) |
| 2 | Should the servings slider have preset "common" values (e.g., 1, 2, 4) or continuous? | UX | Open | Recommend continuous slider; users can pick any value |
| 3 | Max servings on slider — is 20 reasonable, or different? | Product | Open | Recommend 20 as practical limit; can be configured |

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
