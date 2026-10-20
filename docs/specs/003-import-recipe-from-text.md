# Feature Specification: Import Recipe from Pasted Text

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 003 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Capture Recipes (Story Map) |
| Arc42 reference | 3, 5, 8, 10 |

### 1.1 Problem Statement

A cook often has a recipe as a raw blob of text — copied from a document, email, or note app — without any structured markup to parse. They need a way to paste that text and have it segmented into ingredients and steps automatically, rather than typing each into separate fields.

### 1.2 Goal

Let the cook paste an unstructured block of recipe text into a single field. The system segments the text into likely title, ingredient lines, and steps using heuristics, producing a draft that is handed to Story 005 for review and correction before saving.

### 1.3 Non-Goals

- Parsing structured markup from URLs (Story 002)
- OCR or image-based text extraction (Story 004)
- Multi-language parsing accuracy guarantees
- Perfect segmentation — this is best-effort, corrected by the user in Story 005

---

## 2. User Stories

### US-001: Import Recipe via Pasted Text

**As a** home cook,
**I want** to paste a blob of raw recipe text and have it parsed into structured ingredients and steps,
**so that** I can save recipes from notes or documents without manually splitting each line into form fields.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a text area that accepts a single pasted block of recipe text | Must | US-001 |
| FR-002 | The system shall segment the pasted text into candidate title, ingredient lines, and step lines using heuristics (e.g., short lines with quantities/units = ingredients; longer sentences or numbered lines = steps) | Must | US-001 |
| FR-003 | The system shall attempt to identify the first non-ingredient, non-step line as the title | Should | US-001 |
| FR-004 | The system shall populate a draft (ImportedRecipeDraft) with the segmented title, ingredient lines, and steps | Must | US-001 |
| FR-005 | The system shall hand the parsed draft to the review screen (Story 005) rather than saving directly | Must | US-001 |
| FR-006 | The system shall handle text with no clear segmentation by placing all content into steps, leaving ingredients empty for manual entry | Should | US-001 |
| FR-007 | The system shall preserve line order within each segmented section | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Well-Formatted Text Segments Correctly
*(FR-001, FR-002, FR-003, FR-004, FR-005)*

```gherkin
Given the user pastes:
  """
  Garlic Butter Shrimp

  1 lb shrimp, peeled
  3 cloves garlic, minced
  2 tbsp butter

  Melt butter in a pan over medium heat.
  Add garlic and cook until fragrant.
  Add shrimp and cook until pink.
  """
When the user clicks "Import"
Then a draft is created with:
  - title: "Garlic Butter Shrimp"
  - ingredient lines: ["1 lb shrimp, peeled", "3 cloves garlic, minced", "2 tbsp butter"]
  - steps: ["Melt butter in a pan over medium heat.", "Add garlic and cook until fragrant.", "Add shrimp and cook until pink."]
And the user is taken to the review screen (Story 005)
```

### SC-002: No Clear Segmentation
*(FR-006)*

```gherkin
Given the user pastes a single unstructured paragraph with no line breaks or obvious ingredient markers
When the user clicks "Import"
Then a draft is created with the full text placed into a single step
And ingredient lines are left empty
And the user is taken to the review screen (Story 005) to manually separate ingredients
```

### SC-003: Numbered Steps Are Recognized
*(FR-002)*

```gherkin
Given the pasted text includes steps formatted as:
  """
  1. Preheat oven to 350F.
  2. Mix dry ingredients.
  3. Bake for 20 minutes.
  """
When the user clicks "Import"
Then the numbering is stripped and each line becomes a separate step
```

### SC-004: Ingredient Lines with Mixed Formatting
*(FR-002)*

```gherkin
Given the pasted text includes ingredient lines with bullets, dashes, and plain lines:
  """
  - 2 cups flour
  * 1 cup sugar
  3 eggs
  """
When the user clicks "Import"
Then bullets and dashes are stripped
And all three lines are recognized as ingredient lines
```

### SC-005: Empty Paste
*(FR-001)*

```gherkin
Given the user clicks "Import" with an empty text area
Then the system displays: "Paste some recipe text first"
And no draft is created
```

### SC-006: Title Not Clearly Identifiable
*(FR-003)*

```gherkin
Given the pasted text starts directly with an ingredient line (no title line)
When the user clicks "Import"
Then the draft's title is left blank
And the user fills it in during review (Story 005)
```

### SC-007: Very Long Text Block
*(FR-002)*

```gherkin
Given the user pastes a 5000-character block of text
When the user clicks "Import"
Then segmentation completes without error
And the draft is created with best-effort segmentation
```

---

## 5. Domain Model

### 5.1 Entities

#### ImportedRecipeDraft
_(Reused from Story 002, with `sourceType = TEXT`)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Draft identifier |
| sourceType | enum | TEXT for this story | Origin of the draft |
| rawText | string | required | The original pasted text, retained for reference |
| title | string | optional | Segmented title |
| rawIngredientLines | array of string | optional | Segmented ingredient lines |
| rawSteps | array of string | optional | Segmented step lines |
| importStatus | enum | one of: PENDING, PARSED, FAILED | Current state |
| createdAt | datetime | generated | When the import was attempted |

### 5.2 Relationships

- An **ImportedRecipeDraft** is converted into a **Recipe** upon confirmation in Story 005.

### 5.3 Value Objects

No new value objects.

### 5.4 Domain Rules and Invariants

- **Segmentation is best-effort**: No heuristic is guaranteed to be perfectly accurate; Story 005 always allows correction.
- **Line order preserved**: Ingredient and step order reflects the original text order.
- **Original text retained**: The raw pasted text is stored on the draft so the user can refer back to it if segmentation is poor.
- **Numbering/bullets stripped**: List markers (numbers, dashes, bullets) are removed from segmented lines.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Text segmentation completes in < 1 second for typical recipe-length text (under 5000 chars) | Should feel instant since it's client-side/local processing |
| NFR-002 | Resilience | Segmentation never throws an error that blocks draft creation; worst case is poor segmentation, not failure | User should always be able to proceed to review |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Text has no line breaks at all (single long line) | Treated as a single step; ingredients left empty |
| EC-002 | Text mixes ingredients and steps on the same line | Line is placed in whichever section the heuristic favors; corrected in Story 005 |
| EC-003 | Text contains only ingredients, no steps | Draft has ingredients populated, steps empty |
| EC-004 | Text contains only steps, no ingredients | Draft has steps populated, ingredients empty |
| EC-005 | Text has non-Latin characters or emoji | Segmentation proceeds on line structure, not language-specific parsing |
| EC-006 | Pasted text includes unrelated content (e.g., ads, navigation text copied from a webpage) | Included as-is in the best matching section; user cleans up in review |
| EC-007 | Text has excessive blank lines between sections | Blank lines are used as section boundaries; excessive ones are collapsed |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | Segmentation completes in < 1 second for typical input |
| SC-003 | Well-formatted text (clear title/ingredients/steps sections) segments with high accuracy |
| SC-004 | Poorly-formatted text still produces a usable draft for manual correction, never an error |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Feeds into Story 005 (review and fix importer output before saving)
- Shares the ImportedRecipeDraft model with Story 002 and Story 004

### 9.2 Constraints

- Single-user, personal use
- Heuristic-based parsing only; no machine-learning/NLP model assumed unless the architecture team decides otherwise
- English-language heuristics are the baseline; other languages are best-effort

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Text segmentation pipeline, draft storage |
| 8. Cross-Cutting Concepts | Error handling, shared draft model across import stories |
| 10. Quality Requirements | Performance of client-side segmentation |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should segmentation run client-side (instant, simple heuristics) or server-side (more powerful, slower)? | Architecture | Open | Recommend client-side for speed; revisit if accuracy is insufficient |
| 2 | Should the user be able to manually mark line ranges as "ingredients" vs "steps" before import, instead of only after? | UX | Open | Defer to Story 005; pre-marking adds complexity without clear benefit yet |

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
