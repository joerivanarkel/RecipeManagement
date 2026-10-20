# Feature Specification: Import Recipe from URL

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 002 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Capture Recipes (Story Map) |
| Arc42 reference | 3, 5, 8, 10 |

### 1.1 Problem Statement

Many recipes a cook wants to save live on websites and blogs. Re-typing them by hand (Story 001) is slow and defeats the purpose of fast capture. The cook needs a way to paste a link and have the structured data extracted automatically.

### 1.2 Goal

Allow the cook to paste a recipe URL, fetch the page, and extract a draft recipe (title, ingredients, steps, servings, photo) using structured markup where available and heuristics otherwise. The draft is handed to Story 005 for review and correction before saving.

### 1.3 Non-Goals

- Parsing non-recipe pages or general web content
- Multi-language recipe parsing
- Importing multiple recipes from a single page (first/primary recipe only)
- Automatically saving without user review (always routes through Story 005)
- Nutritional data extraction

---

## 2. User Stories

### US-001: Import Recipe via URL

**As a** home cook,
**I want** to paste a recipe URL and have the title, ingredients, and steps scraped in automatically,
**so that** I don't have to manually retype recipes I find online.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall accept a URL as input and validate it is well-formed | Must | US-001 |
| FR-002 | The system shall fetch the page content from the provided URL | Must | US-001 |
| FR-003 | The system shall extract recipe data preferentially from structured markup (e.g., schema.org/Recipe, JSON-LD) when present | Must | US-001 |
| FR-004 | The system shall fall back to heuristic text extraction when structured markup is absent or incomplete | Should | US-001 |
| FR-005 | The system shall populate a draft with: title, ingredients (best-effort split into quantity/unit/item/note), steps, servings, and photo URL | Must | US-001 |
| FR-006 | The system shall display a loading indicator while fetching and parsing | Should | US-001 |
| FR-007 | The system shall hand the parsed draft to the review screen (Story 005) rather than saving directly | Must | US-001 |
| FR-008 | The system shall display a clear error if the URL is unreachable, invalid, or contains no recognizable recipe data | Must | US-001 |
| FR-009 | The system shall time out a fetch attempt after a reasonable duration and report failure | Should | US-001 |
| FR-010 | The system shall offer a fallback link to "Add Manually" (Story 001) when import fails | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Successful Import from Structured Markup
*(FR-001, FR-002, FR-003, FR-005, FR-007)*

```gherkin
Given the user pastes a URL to a recipe page with schema.org/Recipe markup
When the user clicks "Import"
Then the page is fetched and parsed
And a draft is created with title, ingredients, steps, servings, and photo populated
And the user is taken to the review screen (Story 005)
```

### SC-002: Successful Import via Heuristic Fallback
*(FR-004, FR-005)*

```gherkin
Given the user pastes a URL to a recipe page with no structured markup
When the user clicks "Import"
Then the system extracts recipe-like content using heuristics (ingredient-like lines, numbered steps)
And a draft is created, possibly with lower confidence/completeness
And the user is taken to the review screen (Story 005)
```

### SC-003: Invalid URL Format
*(FR-001, FR-008)*

```gherkin
Given the user enters "not a url" into the import field
When the user clicks "Import"
Then the system displays: "Please enter a valid URL"
And no fetch is attempted
```

### SC-004: URL Unreachable
*(FR-002, FR-008)*

```gherkin
Given the user pastes a URL that returns a 404 or connection error
When the user clicks "Import"
Then the system displays: "Couldn't reach that page. Check the link and try again."
And offers a link to "Add Manually" (Story 001)
```

### SC-005: Page Has No Recognizable Recipe Content
*(FR-003, FR-004, FR-008)*

```gherkin
Given the user pastes a URL to a non-recipe page (e.g., a news article)
When the user clicks "Import"
Then the system displays: "We couldn't find a recipe on that page."
And offers a link to "Add Manually" (Story 001)
```

### SC-006: Fetch Times Out
*(FR-009)*

```gherkin
Given the user pastes a URL to a page that is slow or unresponsive
When the fetch exceeds the timeout threshold
Then the system cancels the request
And displays: "That page took too long to load. Try again or add manually."
```

### SC-007: Partial Extraction
*(FR-005)*

```gherkin
Given a recipe page has title and ingredients in structured markup but no servings field
When the import completes
Then the draft has title and ingredients populated
And servings is left blank for the user to fill in during review (Story 005)
```

### SC-008: Loading Indicator Displayed
*(FR-006)*

```gherkin
Given the user clicks "Import" with a valid URL
When the fetch and parse are in progress
Then a loading indicator is displayed
And the import button is disabled until the operation completes
```

---

## 5. Domain Model

### 5.1 Entities

#### ImportedRecipeDraft

Represents an in-progress, unconfirmed recipe extracted from an external source. Not a Recipe until confirmed in Story 005.

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Draft identifier |
| sourceType | enum | one of: URL, TEXT, PHOTO | Where this draft came from |
| sourceUrl | string | optional, max 2000 chars | Original URL (for URL imports) |
| title | string | optional | Extracted title |
| rawIngredientLines | array of string | optional | Best-effort split ingredient lines |
| rawSteps | array of string | optional | Extracted step text |
| servings | integer | optional | Extracted servings, if found |
| photoUrl | string | optional | URL of extracted image |
| importStatus | enum | one of: PENDING, PARSED, FAILED | Current state of the import |
| createdAt | datetime | generated | When the import was attempted |

### 5.2 Relationships

- An **ImportedRecipeDraft** is converted into a **Recipe** (Story 001's entity) upon confirmation in Story 005. Prior to confirmation, it has no relationship to a persisted Recipe.

### 5.3 Value Objects

No value objects needed beyond the draft itself.

### 5.4 Domain Rules and Invariants

- **Draft is not a Recipe**: An ImportedRecipeDraft is never directly persisted as a queryable Recipe; it must pass through Story 005's review and validation.
- **Best-effort extraction**: Fields that cannot be confidently extracted are left blank rather than guessed incorrectly.
- **Primary recipe only**: If a page contains multiple recipes, only the first/primary one is extracted.
- **Source URL retained**: The originating URL is kept with the draft for user reference during review.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Import (fetch + parse) completes in < 5 seconds for typical recipe pages | User shouldn't wait long for a paste-and-go action |
| NFR-002 | Reliability | Fetch requests time out after 10 seconds | Prevents the UI from hanging indefinitely |
| NFR-003 | Security | Fetched page content is parsed as data only; no embedded scripts are executed | Prevents XSS/code execution from untrusted pages |
| NFR-004 | Resilience | Partial extraction failures do not block the overall import; missing fields are left blank | One missing field shouldn't fail the whole import |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | URL redirects multiple times before resolving | Follow a reasonable number of redirects (e.g., max 5), then fail if unresolved |
| EC-002 | Page requires login/paywall | Import fails with "Couldn't find a recipe"; suggest manual entry |
| EC-003 | Page blocks automated requests (403) | Import fails with a clear message; suggest manual entry |
| EC-004 | Recipe page in a non-English language | Extraction proceeds if structured markup exists; heuristic fallback may be unreliable (flagged as low-confidence) |
| EC-005 | Page has multiple recipes (e.g., a roundup post) | Only the first recipe's structured data is extracted |
| EC-006 | Extracted ingredient lines mix instructions with ingredients | Lines go to rawIngredientLines as-is; user corrects in Story 005 |
| EC-007 | Image URL is broken or inaccessible | Draft proceeds without photo; user can add one manually in review |
| EC-008 | User pastes a URL twice in a row | Each import creates a new independent draft; no deduplication |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-008) pass in CI |
| SC-002 | Import completes in < 5 seconds for typical pages with structured markup |
| SC-003 | Structured-markup pages extract title, ingredients, steps, servings, and photo with high accuracy |
| SC-004 | Failed imports display a clear, actionable error message with a manual-entry fallback |
| SC-005 | No page script execution occurs during fetch/parse |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Feeds into Story 005 (review and fix importer output before saving)
- Reuses the Recipe domain model from Story 001 once confirmed

### 9.2 Constraints

- Single-user, personal use
- No server-side script execution on fetched pages (security boundary)
- Extraction quality depends on the structure of the source page; not all sites will parse perfectly

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 3. System Scope and Context | External HTTP fetch as a system boundary concern |
| 5. Building Block View | Fetch/parse pipeline, draft storage |
| 8. Cross-Cutting Concepts | Error handling, security (safe parsing, no script execution) |
| 10. Quality Requirements | Performance (import latency), resilience |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Which structured markup formats should be supported beyond schema.org/Recipe JSON-LD (e.g., microdata)? | Architecture | Open | Recommend JSON-LD first; add microdata if real-world sites require it |
| 2 | Should imports be retried automatically on transient network failure? | Architecture | Open | Recommend a single retry with backoff before surfacing failure |

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
