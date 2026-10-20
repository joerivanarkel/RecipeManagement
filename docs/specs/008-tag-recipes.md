# Feature Specification: Tag a Recipe

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 008 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Build the Collection (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

As a collection grows, recipes need a way to be categorized beyond title and ingredients so the cook can narrow things down later (e.g., Story 015's tag filtering during "what can I cook" decisions). Without tags, every recipe is an undifferentiated entry in the grid.

### 1.2 Goal

Let the cook add free-text tags (e.g., "vegetarian", "quick", "Asian") to a recipe, with autocomplete suggestions drawn from previously used tags. Tags are displayed on the browse grid (Story 006) and used for filtering (Story 015).

### 1.3 Non-Goals

- Hierarchical or nested tag categories
- Tag merging/renaming across the whole collection
- Mandatory or pre-defined tag taxonomy
- Auto-tagging based on ingredient analysis

---

## 2. User Stories

### US-001: Tag a Recipe

**As a** home cook,
**I want** to tag a recipe (e.g., cuisine, meal type, vegetarian, quick),
**so that** I can narrow down my collection later when deciding what to cook.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall allow adding one or more tags to a recipe from the edit form (Story 010) | Must | US-001 |
| FR-002 | The system shall accept a tag as free text, max 50 characters | Must | US-001 |
| FR-003 | The system shall suggest existing tags (autocomplete) drawn from tags already used across the collection, as the user types | Should | US-001 |
| FR-004 | The system shall prevent duplicate tags on the same recipe (case-insensitive) | Must | US-001 |
| FR-005 | The system shall allow removing a tag from a recipe | Must | US-001 |
| FR-006 | The system shall display tags as visually distinct badges on the recipe detail (Story 007) and browse grid (Story 006) | Must | US-001 |
| FR-007 | The system shall normalize tag display casing consistently (e.g., lowercase storage, capitalized display) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Add a New Tag
*(FR-001, FR-002, FR-006)*

```gherkin
Given the user is editing a recipe
When the user types "vegetarian" into the tag input and confirms
Then the tag "vegetarian" is added to the recipe
And it displays as a badge on save
```

### SC-002: Autocomplete Suggests Existing Tags
*(FR-003)*

```gherkin
Given the collection already has tags "vegetarian", "vegan", "quick" used on other recipes
When the user types "veg" into the tag input
Then suggestions "vegetarian" and "vegan" appear
And the user can select one instead of typing the full tag
```

### SC-003: Prevent Duplicate Tag on Same Recipe
*(FR-004)*

```gherkin
Given a recipe already has the tag "Quick"
When the user tries to add the tag "quick" (different casing)
Then the system does not add a duplicate
And optionally shows: "Already tagged as Quick"
```

### SC-004: Remove a Tag
*(FR-005)*

```gherkin
Given a recipe has tags "vegetarian" and "quick"
When the user removes the "quick" tag
And saves the recipe
Then only "vegetarian" remains on the recipe
```

### SC-005: Tags Display on Recipe Detail and Grid
*(FR-006)*

```gherkin
Given a recipe has tags "vegetarian", "quick", "asian"
When the user views the recipe detail page (Story 007)
Then all three tags display as badges
When the user views the browse grid (Story 006)
Then the same tags display on the recipe's card
```

### SC-006: Tag with Maximum Length
*(FR-002)*

```gherkin
Given the user enters a tag of exactly 50 characters
When the user confirms the tag
Then it is accepted and saved
When the user attempts a tag of 51+ characters
Then the system rejects it with: "Tag is too long (max 50 characters)"
```

### SC-007: Recipe with No Tags
*(FR-006)*

```gherkin
Given a recipe has no tags
When the user views the recipe detail or grid card
Then no tag area/badges are shown (no empty placeholder)
```

### SC-008: Case-Insensitive Tag Display Consistency
*(FR-007)*

```gherkin
Given one recipe was tagged "Quick" and another "quick"
When both are displayed across the app
Then both display with consistent casing (e.g., "Quick")
And both are treated as the same tag for filtering purposes (Story 015)
```

---

## 5. Domain Model

### 5.1 Entities

#### Tag

Represents a single tag applied to a recipe.

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Tag identifier |
| recipeId | UUID | required, FK to Recipe | Recipe this tag belongs to |
| name | string | required, max 50 chars, stored lowercase | Tag text (e.g., "vegetarian", "quick") |

### 5.2 Relationships

- A **Recipe** has zero or more **Tags** (one-to-many).
- Tag autocomplete suggestions are derived from the distinct set of `name` values across all Tags in the collection (no separate global tag registry entity required).

### 5.3 Value Objects

No new value objects.

### 5.4 Domain Rules and Invariants

- **Tag names are stored normalized (lowercase)**: Comparison and deduplication are case-insensitive; display formatting (e.g., capitalization) is a presentation concern.
- **No duplicate tags per recipe**: A recipe cannot have the same tag name (case-insensitively) more than once.
- **Tags are recipe-scoped**: Deleting a recipe deletes its tags; there is no shared tag entity that persists independently across recipes.
- **No predefined taxonomy**: Any string up to 50 characters is a valid tag; there is no fixed list of allowed tags.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Autocomplete suggestions appear in < 150ms as the user types | Should feel responsive, not laggy |
| NFR-002 | Performance | Adding/removing a tag persists in < 200ms | Tag edits should feel instant within the edit form |
| NFR-003 | Usability | Tag badges are visually distinct from other recipe metadata (e.g., colored pills) | Supports quick scanning on the grid |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User enters a tag with only whitespace | Rejected; whitespace-only tags are not valid |
| EC-002 | User enters a tag with special characters (e.g., "spicy!") | Accepted as-is; no character restriction beyond length |
| EC-003 | Recipe has 20+ tags | All tags are stored; display truncation (e.g., "+N more") is handled by Story 006's grid card rules |
| EC-004 | Two different recipes use the exact same tag name | Each recipe stores its own Tag row; autocomplete treats them as the same suggestion |
| EC-005 | User deletes a recipe that was the only one using a particular tag | The tag disappears from autocomplete suggestions since no recipe references it anymore |
| EC-006 | User tries to add a tag while editing, then cancels the edit without saving | No tag changes are persisted |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-008) pass in CI |
| SC-002 | Tags can be added, removed, and display correctly across detail and grid views |
| SC-003 | Duplicate tags (case-insensitive) are prevented on the same recipe |
| SC-004 | Autocomplete suggests existing tags in < 150ms |
| SC-005 | Tag data is available to Story 015 for filtering |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 001 (recipes must exist to tag)
- Edited via Story 010's edit form
- Displayed in Story 006 (grid) and Story 007 (detail)
- Feeds into Story 015 (filter suggestions by tag)

### 9.2 Constraints

- Single-user, personal use (no shared/global tag governance)
- No predefined tag taxonomy or validation beyond length and duplicate checks
- Tags are recipe-scoped, not a standalone manageable entity

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Tag storage and autocomplete query patterns |
| 8. Cross-Cutting Concepts | Case-insensitive comparison and normalization patterns |
| 10. Quality Requirements | Autocomplete responsiveness |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should there be a maximum number of tags per recipe? | Product | Open | Recommend no hard limit for MVP; revisit if UI clutter becomes an issue |
| 2 | Should common tag categories (cuisine, meal type, diet) be visually grouped, or all tags treated equally? | UX | Open | Recommend flat/equal treatment for MVP; grouping is a future enhancement |

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
