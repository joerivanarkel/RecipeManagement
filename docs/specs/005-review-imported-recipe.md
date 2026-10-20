# Feature Specification: Review and Fix Imported Recipe

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 005 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Capture Recipes (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

None of the importers (Story 002 URL scrape, Story 003 text parse, Story 004 photo OCR) can guarantee perfect extraction. Without a review step, incorrect or incomplete drafts would either fail to save or silently corrupt the recipe collection. The cook needs a chance to see and fix whatever the importer got wrong before it becomes a real recipe.

### 1.2 Goal

Present any ImportedRecipeDraft (from Story 002, 003, or 004) in the same form used for manual entry (Story 001), pre-populated with the parsed values. The user corrects any field, and the same validation rules apply. On save, the draft becomes a real Recipe; on cancel, the draft is discarded.

### 1.3 Non-Goals

- Re-running the importer with different settings
- Side-by-side diffing against the original source (beyond showing the source photo for Story 004)
- Partial saves (draft is either confirmed in full or discarded)

---

## 2. User Stories

### US-001: Review and Correct Imported Recipe

**As a** home cook,
**I want** to review and fix anything an importer got wrong before the recipe is saved,
**so that** my collection stays accurate even when automatic extraction isn't perfect.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display the recipe entry form (Story 001) pre-populated with the draft's parsed values | Must | US-001 |
| FR-002 | The system shall allow editing of every field: title, servings, ingredients (quantity/unit/item/note), steps, and photo | Must | US-001 |
| FR-003 | The system shall apply the same validation rules as Story 001 (title required, servings required > 0, at least one ingredient with a non-empty item) | Must | US-001 |
| FR-004 | The system shall display the original source alongside the form for reference: the source URL (Story 002), the raw pasted text (Story 003), or the source photo (Story 004) | Should | US-001 |
| FR-005 | The system shall indicate which fields the importer was unable to populate (e.g., visually flag blank required fields) | Should | US-001 |
| FR-006 | The system shall persist the recipe as a standard Recipe entity upon save, identical in structure to a manually entered recipe (Story 001) | Must | US-001 |
| FR-007 | The system shall discard the draft if the user cancels without saving | Must | US-001 |
| FR-008 | The system shall display a low-confidence warning banner when the source was OCR-extracted with low confidence (Story 004) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Review and Save a Well-Parsed Draft
*(FR-001, FR-002, FR-003, FR-006)*

```gherkin
Given a draft was created from a URL import (Story 002) with all fields populated correctly
When the user opens the review screen
Then the form displays title, servings, ingredients, and steps matching the draft
When the user clicks "Save Recipe" without changes
Then a new Recipe is persisted identical to the draft's content
And the draft is discarded
```

### SC-002: Correct a Misparsed Field
*(FR-002, FR-003)*

```gherkin
Given a draft has an ingredient misparsed as: item="2 cloves garlic, minced", quantity=[empty]
When the user is on the review screen
Then the user edits the fields to: quantity="2", unit="cloves", item="garlic", note="minced"
And clicks "Save Recipe"
Then the recipe is persisted with the corrected ingredient structure
```

### SC-003: Fill In a Blank Required Field
*(FR-003, FR-005)*

```gherkin
Given a draft has no title (importer could not determine one)
When the user is on the review screen
Then the title field is empty and visually flagged
When the user types a title and clicks "Save Recipe"
Then the recipe is saved successfully
```

### SC-004: Reject Save with Missing Required Field
*(FR-003)*

```gherkin
Given a draft is missing servings
When the user clicks "Save Recipe" without entering servings
Then the form displays: "Servings is required"
And the recipe is NOT persisted
```

### SC-005: Cancel Discards the Draft
*(FR-007)*

```gherkin
Given the user is reviewing an imported draft
When the user clicks "Cancel" or navigates away
Then a confirmation appears: "Discard this import?"
When the user confirms
Then the draft is discarded
And no recipe is persisted
```

### SC-006: View Source Photo During Review
*(FR-004)*

```gherkin
Given a draft was created from a photo import (Story 004)
When the user opens the review screen
Then the original photo is displayed alongside the form fields
So the user can cross-check extracted text against the source image
```

### SC-007: View Source URL During Review
*(FR-004)*

```gherkin
Given a draft was created from a URL import (Story 002)
When the user opens the review screen
Then the source URL is displayed (e.g., as a link) for reference
```

### SC-008: Low-Confidence OCR Warning
*(FR-008)*

```gherkin
Given a draft was created from a photo with low OCR confidence (Story 004)
When the user opens the review screen
Then a warning banner displays: "This text may not be accurate — please review carefully"
```

### SC-009: Remove an Incorrectly Extracted Ingredient
*(FR-002)*

```gherkin
Given a draft has an ingredient line that is actually a step, misclassified as an ingredient
When the user is on the review screen
Then the user deletes that ingredient row
And optionally adds the corrected text as a step instead
And clicks "Save Recipe"
Then the recipe is persisted without the erroneous ingredient
```

### SC-010: Add a Photo Not Extracted by the Importer
*(FR-002)*

```gherkin
Given a text-based import (Story 003) produced a draft with no photo
When the user is on the review screen
Then the user uploads a photo manually
And clicks "Save Recipe"
Then the recipe is persisted with the manually added photo
```

---

## 5. Domain Model

### 5.1 Entities

This story introduces no new entities. It operates on:

#### ImportedRecipeDraft
_(From Story 002/003/004)_ — read as input, discarded on cancel or confirm.

#### Recipe, Ingredient, Step, Photo
_(From Story 001)_ — created on save, using the same structure and validation as manual entry.

### 5.2 Relationships

- A single **ImportedRecipeDraft** produces at most one **Recipe** (on save) or zero (on cancel/discard).
- The resulting **Recipe** has the same relationships to **Ingredient**, **Step**, and **Photo** as described in Story 001.

### 5.3 Value Objects

No new value objects.

### 5.4 Domain Rules and Invariants

- **Validation parity with Story 001**: No separate validation path exists for imported recipes; they must satisfy the exact same rules as manual entry.
- **All-or-nothing confirmation**: A draft either becomes a complete, valid Recipe or is discarded entirely — there is no partially-saved import.
- **Draft is ephemeral**: Once confirmed or discarded, the ImportedRecipeDraft is deleted; it has no long-term existence in the system.
- **Source reference is read-only context**: The source URL, raw text, or photo displayed during review is for the user's reference only and is not itself persisted as part of the final Recipe (except the photo, if the user chooses to keep it as the Recipe's photo).

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Review form loads pre-populated in < 300ms after import completes | Should feel like a continuous flow from import to review |
| NFR-002 | Usability | Blank/missing fields are visually distinguishable from correctly parsed ones | Helps the user focus corrections where the importer struggled |
| NFR-003 | Data Integrity | Save is atomic, matching Story 001's atomic persistence guarantee | Prevents partially-saved imported recipes |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User abandons the review screen without explicit cancel (closes tab/app) | Draft is discarded; nothing is persisted |
| EC-002 | Draft has duplicate ingredient lines from a parsing error | All lines are shown as separate rows; user deletes duplicates manually |
| EC-003 | Draft's extracted title is actually part of the ingredient list | User edits title and removes it from ingredients; same correction flow as any other field |
| EC-004 | Import source becomes unavailable between import and review (e.g., URL goes down) | Does not affect review; draft data was already captured and does not require re-fetching |
| EC-005 | User saves an imported recipe with zero steps (importer found none) | Allowed — steps are optional per Story 001's rules |
| EC-006 | Multiple imports are started before the first is reviewed | Each import produces its own independent draft; no collision |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-010) pass in CI |
| SC-002 | Review form reuses Story 001's validation without duplication or divergence |
| SC-003 | Saved imported recipes are indistinguishable in structure from manually entered recipes |
| SC-004 | Cancelling an import always discards the draft with no residual data |
| SC-005 | Source reference (URL/text/photo) is visible during review for all three import types |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on at least one importer existing to produce a draft: Story 002 (URL), Story 003 (text), or Story 004 (photo)
- Reuses the exact form and validation logic from Story 001

### 9.2 Constraints

- Single-user, personal use
- No partial saves; a draft is confirmed in full or discarded in full
- This story has no standalone entry point — it is only reachable from an active import flow

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Shared form component between Story 001 and Story 005 |
| 8. Cross-Cutting Concepts | Validation reuse, draft lifecycle management |
| 10. Quality Requirements | Data integrity for atomic save/discard |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should drafts be persisted temporarily (so a crash doesn't lose an in-progress review), or held only in memory? | Architecture | Open | Recommend short-lived persistence (e.g., local storage) to survive accidental navigation |
| 2 | Should there be a way to re-run the importer from the review screen if corrections are extensive? | UX | Open | Defer to future; user can cancel and re-import as a workaround for now |

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
