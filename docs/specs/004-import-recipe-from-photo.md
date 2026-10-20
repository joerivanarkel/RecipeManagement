# Feature Specification: Import Recipe from Photographed Page

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 004 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Capture Recipes (Story Map) |
| Arc42 reference | 3, 5, 8, 10 |

### 1.1 Problem Statement

Many of a cook's recipes live only in physical cookbooks or handwritten cards. Typing these out (Story 001) is the slowest possible capture path. The cook needs to photograph a page and have the text read automatically, turning it into a recipe draft.

### 1.2 Goal

Let the cook take or upload a photo of a cookbook page or handwritten recipe. The system runs OCR to extract text, then reuses the text segmentation pipeline (Story 003) to produce a draft (title, ingredients, steps). The draft is handed to Story 005 for review and correction.

### 1.3 Non-Goals

- High-accuracy handwriting recognition (best-effort only; printed text is the primary target)
- Multi-page recipe stitching (one photo per import)
- Real-time camera-based live text recognition
- Image enhancement/cropping tools beyond basic capture

---

## 2. User Stories

### US-001: Import Recipe via Photo

**As a** home cook,
**I want** to photograph a cookbook page and have the text read and turned into a recipe draft,
**so that** I can digitize recipes that only exist on paper without retyping them.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall allow the user to capture a photo via device camera or upload an existing image | Must | US-001 |
| FR-002 | The system shall validate the uploaded/captured file is a supported image format (JPG, PNG) and within a reasonable size limit (e.g., 10MB) | Must | US-001 |
| FR-003 | The system shall run OCR on the image to extract raw text | Must | US-001 |
| FR-004 | The system shall report an OCR confidence indicator alongside the extracted text | Should | US-001 |
| FR-005 | The system shall pass the extracted text through the same segmentation pipeline used for pasted text (Story 003) to produce title/ingredients/steps | Must | US-001 |
| FR-006 | The system shall populate a draft (ImportedRecipeDraft) with the segmented content and retain the source photo for reference | Must | US-001 |
| FR-007 | The system shall hand the parsed draft to the review screen (Story 005) rather than saving directly | Must | US-001 |
| FR-008 | The system shall display a clear error when OCR fails to extract any usable text | Must | US-001 |
| FR-009 | The system shall display a loading indicator while OCR processing is in progress | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Successful OCR and Segmentation
*(FR-001, FR-003, FR-005, FR-006, FR-007)*

```gherkin
Given the user photographs a clearly printed cookbook page
When the user confirms the photo for import
Then OCR extracts the page text
And the text is segmented into title, ingredients, and steps
And a draft is created with the source photo attached
And the user is taken to the review screen (Story 005)
```

### SC-002: Low-Confidence OCR
*(FR-004, FR-008)*

```gherkin
Given the user photographs a handwritten or blurry recipe card
When OCR completes with low confidence
Then the draft is still created with whatever text was extracted
And a warning is shown: "This text may not be accurate — please review carefully"
```

### SC-003: OCR Finds No Text
*(FR-008)*

```gherkin
Given the user photographs a blank page or an image with no readable text
When OCR completes
Then the system displays: "We couldn't read any text from that photo. Try a clearer photo or add manually."
And offers a link to "Add Manually" (Story 001)
```

### SC-004: Unsupported File Format
*(FR-002)*

```gherkin
Given the user attempts to upload a .pdf or .txt file instead of an image
When the upload is attempted
Then the system rejects it with: "Please upload a JPG or PNG image"
```

### SC-005: File Too Large
*(FR-002)*

```gherkin
Given the user attempts to upload a 20MB image
When the upload is attempted
Then the system rejects it with: "Image is too large (max 10MB)"
```

### SC-006: Loading Indicator During OCR
*(FR-009)*

```gherkin
Given the user confirms a photo for import
When OCR processing is in progress
Then a loading indicator is displayed
And the import controls are disabled until processing completes
```

### SC-007: Source Photo Retained for Reference
*(FR-006)*

```gherkin
Given OCR has extracted text from a photo
When the user reaches the review screen (Story 005)
Then the original photo is displayed alongside the parsed fields
So the user can cross-check the extracted text against the source
```

### SC-008: Multi-Column Page Layout
*(FR-003)*

```gherkin
Given the photographed page has a two-column layout (common in cookbooks)
When OCR processes the image
Then text is extracted in a best-effort reading order
And may require more correction in Story 005 if columns are misread
```

---

## 5. Domain Model

### 5.1 Entities

#### ImportedRecipeDraft
_(Reused from Story 002/003, with `sourceType = PHOTO`)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Draft identifier |
| sourceType | enum | PHOTO for this story | Origin of the draft |
| sourcePhotoUrl | string | required for this source type | Reference to the captured/uploaded image |
| ocrRawText | string | optional | Raw text extracted by OCR |
| ocrConfidence | decimal | optional, 0-100 | Overall confidence score of the OCR pass |
| title | string | optional | Segmented title |
| rawIngredientLines | array of string | optional | Segmented ingredient lines |
| rawSteps | array of string | optional | Segmented step lines |
| importStatus | enum | one of: PENDING, PARSED, FAILED | Current state |
| createdAt | datetime | generated | When the import was attempted |

### 5.2 Relationships

- An **ImportedRecipeDraft** is converted into a **Recipe** upon confirmation in Story 005.
- The source photo may optionally become the Recipe's **Photo** (Story 001) if the user chooses to keep it, or be discarded if the user uploads a different photo during review.

### 5.3 Value Objects

No new value objects.

### 5.4 Domain Rules and Invariants

- **OCR output feeds text segmentation**: Once text is extracted, the same segmentation rules from Story 003 apply — no separate parsing logic is maintained for photo imports.
- **Low confidence does not block import**: A low OCR confidence score produces a warning, not a failure; the user always gets a chance to review and correct.
- **Source photo retained until confirmed/discarded**: The original image stays attached to the draft through review so the user can cross-reference it.
- **File validation occurs before OCR**: Format and size are checked before attempting OCR, to fail fast on obviously invalid input.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | OCR processing completes in < 10 seconds for a typical single-page photo | User is waiting live; should not feel broken |
| NFR-002 | Storage | Uploaded photos are validated for format and size before processing (max 10MB, JPG/PNG) | Prevents abuse and excessive storage/processing cost |
| NFR-003 | Resilience | OCR failures produce a clear error and a manual-entry fallback, never a silent failure | User must never be left without a path forward |
| NFR-004 | Accessibility | Camera capture flow works on mobile devices with standard camera permissions | Primary use case is photographing a physical cookbook in-kitchen |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Photo is blurry or out of focus | OCR attempts extraction; low confidence warning shown if text is partially unreadable |
| EC-002 | Photo is taken at an angle/skewed | OCR attempts extraction; accuracy may suffer, flagged via confidence score |
| EC-003 | Photo includes only part of the recipe (cropped) | Only the visible text is extracted; user completes the rest manually in review |
| EC-004 | Lighting is poor (dark or glare on page) | OCR attempts extraction; low confidence warning if text is unreadable |
| EC-005 | Handwritten recipe card | OCR attempts extraction with likely lower confidence; warning displayed prominently |
| EC-006 | User denies camera permission | System falls back to file upload option |
| EC-007 | Photo contains a recipe in a non-English language | OCR extracts text if the engine supports the language; otherwise flagged as low confidence |
| EC-008 | Photo upload fails due to network interruption | Clear error shown; user can retry |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-008) pass in CI |
| SC-002 | OCR completes in < 10 seconds for typical single-page photos |
| SC-003 | Clearly printed, well-lit pages produce high-confidence, accurate extraction |
| SC-004 | Low-confidence or failed OCR always surfaces a clear message and a manual-entry fallback |
| SC-005 | Source photo is retained and visible during review (Story 005) |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 003's text segmentation pipeline (reused after OCR extraction)
- Feeds into Story 005 (review and fix importer output before saving)
- Shares the ImportedRecipeDraft model with Story 002 and Story 003

### 9.2 Constraints

- Single-user, personal use
- OCR engine/provider choice is an architecture decision (on-device vs. cloud OCR service)
- No guarantee of accuracy for handwritten text; printed text is the primary target
- One photo per import (no multi-page stitching)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | OCR integration, photo storage, draft pipeline |
| 8. Cross-Cutting Concepts | Error handling, file validation, shared draft model |
| 10. Quality Requirements | OCR performance and reliability targets |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Which OCR provider/engine should be used (on-device vs. cloud API)? | Architecture | Open | Decide based on cost, privacy, and offline requirements |
| 2 | Should there be a minimum confidence threshold below which import is refused entirely? | Product | Open | Recommend no hard threshold; always show a warning instead of blocking |
| 3 | Should users be able to crop/rotate the photo before OCR runs? | UX | Open | Defer to future; out of scope for Story 004 MVP |

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
