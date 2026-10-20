# Feature Specification: Keep a Pantry List

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 012 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Decide What to Cook Tonight (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

To answer "what can I cook right now?", the app needs to know what ingredients the cook has on hand. Without a pantry, Story 014 (recipe matching) cannot work — it's the foundation of the decision-making loop.

### 1.2 Goal

Allow the cook to maintain a persistent list of pantry items (fridge, freezer, cupboards) with quantities. Items are stored with category and quantity (e.g., "2 lbs chicken", "6 eggs") so that Story 014 can match them against recipe ingredients.

### 1.3 Non-Goals

- Consume/destock tracking (mark items as used when cooking)
- Expiration dates or freshness tracking
- Auto-suggestions based on recipes being cooked
- Grocery list generation
- Barcode scanning or recipe-to-pantry auto-import

---

## 2. User Stories

### US-001: Add Pantry Items

**As a** home cook,
**I want** to add items to my pantry with a category (fridge, freezer, cupboards) and quantity,
**so that** the app knows what I have on hand.

### US-002: View Pantry

**As a** home cook,
**I want** to see my pantry organized by category (fridge, freezer, cupboards),
**so that** I can remember what I have and manage my inventory.

### US-003: Remove Pantry Items

**As a** home cook,
**I want** to delete items from my pantry when I run out,
**so that** the app's suggestions (Story 014) stay accurate.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a pantry view with items organized by category (fridge, freezer, cupboards) | Must | US-002 |
| FR-002 | The system shall allow the user to add pantry items with: category, item name, and quantity | Must | US-001 |
| FR-003 | The system shall accept and store pantry item quantity as a string (e.g., "2 lbs flour", "6 eggs", "1/2 cup milk") | Must | US-001 |
| FR-004 | The system shall allow the user to delete a pantry item | Must | US-003 |
| FR-005 | The system shall persist pantry items to durable storage | Must | US-001 |
| FR-006 | The system shall display the pantry list with item name and quantity clearly visible | Should | US-002 |
| FR-007 | The system shall allow the user to edit a pantry item's quantity | Should | US-001 |
| FR-008 | The system shall prevent duplicate items in the same category (optional: warn on add, or allow with quantities summed) | Could | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Add Pantry Item
*(FR-002, FR-003, FR-005)*

```gherkin
Given the user is on the pantry page
When the user selects category "fridge"
And enters item "chicken" with quantity "2 lbs"
And clicks "Add to Pantry"
Then the item is stored
And the pantry list updates to show:
  | category | item | quantity |
  | fridge | chicken | 2 lbs |
```

### SC-002: View Pantry Organized by Category
*(FR-001, FR-006)*

```gherkin
Given the user has pantry items:
  | category | item | quantity |
  | fridge | chicken | 2 lbs |
  | fridge | milk | 1 liter |
  | freezer | beef | 1 kg |
  | cupboards | flour | 3 lbs |
When the user views the pantry
Then items are grouped by category:
  - **Fridge**: chicken (2 lbs), milk (1 liter)
  - **Freezer**: beef (1 kg)
  - **Cupboards**: flour (3 lbs)
```

### SC-003: Remove Pantry Item
*(FR-004, FR-005)*

```gherkin
Given the pantry contains:
  | item | quantity |
  | eggs | 6 |
  | milk | 1 liter |
When the user clicks "Delete" next to eggs
Then the item is removed from storage
And the pantry list updates
And only milk remains
```

### SC-004: Edit Pantry Item Quantity
*(FR-007)*

```gherkin
Given the pantry contains:
  | item | quantity |
  | flour | 3 lbs |
When the user clicks "Edit" next to flour
And changes quantity to "1.5 lbs"
And clicks "Save"
Then the pantry updates to show "1.5 lbs"
```

### SC-005: Empty Pantry
*(FR-001)*

```gherkin
Given the user has no pantry items
When the user views the pantry
Then a message displays: "Your pantry is empty. Add items to get recipe suggestions."
And a button appears: "Add your first item"
```

### SC-006: Add Item with Flexible Quantity Format
*(FR-003)*

```gherkin
Given the user is adding a pantry item
When the user enters quantities in various formats:
  | format | example |
  | quantity + unit | 2 cups |
  | fractional | 1/2 cup |
  | decimal | 1.5 lbs |
  | range | 2-3 cloves |
  | descriptive | a bunch, a pinch, to taste |
Then all formats are accepted and stored as-is
```

### SC-007: Pantry Persists Across Sessions
*(FR-005)*

```gherkin
Given the user adds items to their pantry
When the user closes the app and returns later
Then the pantry items are still there, unchanged
```

### SC-008: Quick Add Multiple Items
*(FR-002, FR-006)*

```gherkin
Given the user is on the pantry page
When the user adds multiple items in quick succession:
  - Add "chicken" (fridge)
  - Add "rice" (cupboards)
  - Add "ice cream" (freezer)
Then all three items appear in the pantry, organized by category
And the user doesn't have to refresh or reload
```

### SC-009: Category Selection
*(FR-002)*

```gherkin
Given the user is adding a pantry item
When the user selects a category
Then the options are: Fridge, Freezer, Cupboards
And no other categories are available
```

### SC-010: Duplicate Item Handling
*(FR-008)*

```gherkin
Given the pantry already contains "chicken, 2 lbs" in fridge
When the user tries to add "chicken, 1 lb" to fridge
Then the system either:
  - Warns: "Chicken already in fridge. Proceed anyway?" (allows duplicates)
  - Or suggests: "Update existing item instead?"
[Note: This is a design choice; for MVP, recommend allowing duplicates or simple replacement]
```

---

## 5. Domain Model

### 5.1 Entities

#### PantryItem

Represents a single item in the cook's pantry.

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Unique pantry item identifier |
| category | enum | required, one of: FRIDGE, FREEZER, CUPBOARDS | Where the item is stored |
| itemName | string | required, max 100 chars | Name of the ingredient (e.g., "chicken", "flour") |
| quantity | string | required, max 100 chars | Human-readable quantity (e.g., "2 lbs", "6 eggs", "1/2 cup") |
| addedAt | datetime | generated | When the item was added to pantry |
| updatedAt | datetime | generated | When the quantity was last changed |

### 5.2 Relationships

- A **User** has many **PantryItems** (implied in single-user context; all pantry items belong to the one cook).

### 5.3 Value Objects

No value objects needed for this story.

### 5.4 Domain Rules and Invariants

- **Category is required**: Every pantry item must be in one of three categories (Fridge, Freezer, Cupboards).
- **Item name is required**: Cannot add an item without specifying what it is.
- **Quantity is required**: The app needs to know "how much" to match against recipes in Story 014.
- **Quantity is flexible format**: Quantities are stored as free text, not parsed (e.g., "2 lbs", "a bunch", "to taste").
- **Display order**: Items within a category are displayed in order added (or can be manually reordered; deferred to future).
- **No consumption tracking in MVP**: Items are added and deleted manually; "consume" tracking is out of scope for Story 012.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Pantry view loads in < 200ms | User should see pantry instantly |
| NFR-002 | Performance | Add/delete/edit operations complete in < 100ms | Operations should feel responsive |
| NFR-003 | Accessibility | Pantry form is keyboard-navigable; category dropdown is accessible | User may be in kitchen with limited mouse access |
| NFR-004 | Reliability | Pantry data survives app restarts | Pantry is the source of truth for Story 014 matching |
| NFR-005 | Responsiveness | Pantry UI is readable on mobile and desktop | Users manage pantry from various locations |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User adds item with empty name | Form rejects; displays error: "Item name is required" |
| EC-002 | User adds item with empty quantity | Form rejects; displays error: "Quantity is required" |
| EC-003 | User adds item with very long name (> 100 chars) | Form rejects or truncates with warning |
| EC-004 | User adds item with very long quantity (> 100 chars) | Form rejects or truncates with warning |
| EC-005 | User deletes an item; app is closed before save completes | Item is deleted (optimistic update); sync on next load |
| EC-006 | User adds duplicate item to same category | Allow (design choice for MVP); can be merged later or user manages manually |
| EC-007 | Pantry has 100+ items | UI scrolls; all items are accessible; no performance degradation |
| EC-008 | Network fails during add/delete | Operation queued and retried when connection returns (or error shown, depending on architecture) |
| EC-009 | User edits quantity to empty string | Form rejects; quantity must remain valid |
| EC-010 | Category dropdown not available | Fallback to text entry or default category (Cupboards) |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-010) pass in CI |
| SC-002 | Pantry view loads in < 200ms |
| SC-003 | Add/delete/edit operations complete in < 100ms |
| SC-004 | Pantry items persist across app restarts |
| SC-005 | Items are displayed organized by category (Fridge, Freezer, Cupboards) |
| SC-006 | Empty pantry state displays appropriate message |
| SC-007 | Pantry data is available to Story 014 for recipe matching |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Foundation for Story 014 (recipe matching depends on pantry data)
- Optional quick-add from Story 013 (type ingredients without updating pantry)
- No dependency on Stories 001-011 (pantry is independent)

### 9.2 Constraints

- Single-user, personal use (no multi-user pantries)
- Quantities are free-text, not normalized (no parsing or validation of format)
- No consumption/destock tracking in Story 012 (future feature)
- No recipe-to-pantry auto-updates or sync

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Pantry data storage and retrieval |
| 8. Cross-Cutting Concepts | Error handling, form validation patterns |
| 10. Quality Requirements | Performance and accessibility standards |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should duplicate items be allowed, or should quantity be merged? | Product | Open | Recommend allowing duplicates for MVP; user can manually manage |
| 2 | Should pantry items be reorderable (drag-and-drop)? | UX | Open | Not in Story 012; defer to future if needed |
| 3 | Should categories be editable/customizable, or fixed (Fridge/Freezer/Cupboards)? | Product | Open | Recommend fixed for MVP; simplicity wins |

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
