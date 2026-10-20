# Feature Specification: See Last-Cooked Date at a Glance

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 017 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Decide What to Cook Tonight (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Story 023 records when a recipe is cooked, but that data is only useful if it's visible where the cook is actually scanning recipes — the browse grid and detail page. Without a glance-able indicator, the cook has no passive reminder that they're repeating themselves.

### 1.2 Goal

Display a lightweight, consistently formatted last-cooked indicator (e.g., "Cooked 3 days ago" or "Never cooked") on both the browse grid cards (Story 006) and the recipe detail page (Story 007), sourced from the cook log (Story 023).

### 1.3 Non-Goals

- Full cook history list or detailed frequency breakdown (Story 024)
- Any new write operations — this story is display-only, reading existing cook log data
- Custom reminder notifications ("you haven't cooked X in a while")

---

## 2. User Stories

### US-001: See Last-Cooked Date While Browsing

**As a** home cook,
**I want** to see at a glance when a recipe was last cooked,
**so that** I don't repeat myself without noticing while scanning my collection.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a last-cooked indicator on each recipe card in the browse grid (Story 006) | Should | US-001 |
| FR-002 | The system shall display a last-cooked indicator on the recipe detail page (Story 007) | Should | US-001 |
| FR-003 | The system shall format the last-cooked date as a relative, human-readable string (e.g., "today", "3 days ago", "2 months ago") | Must | US-001 |
| FR-004 | The system shall display "Never cooked" for recipes with no cook log entries | Must | US-001 |
| FR-005 | The system shall use a single, shared relative-time formatting rule across grid and detail views for consistency | Must | US-001 |
| FR-006 | The system shall update the last-cooked indicator immediately after a cook event is recorded (Story 023) without requiring a page reload | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Display Last-Cooked on Grid Card
*(FR-001, FR-003)*

```gherkin
Given a recipe was last cooked 3 days ago
When the user views the browse grid (Story 006)
Then that recipe's card displays: "Cooked 3 days ago"
```

### SC-002: Display Last-Cooked on Detail Page
*(FR-002, FR-003)*

```gherkin
Given a recipe was last cooked yesterday
When the user views the recipe detail page (Story 007)
Then the page displays: "Last cooked: yesterday"
```

### SC-003: Never Cooked
*(FR-004)*

```gherkin
Given a recipe has no cook log entries
When the user views it on the grid or detail page
Then it displays: "Never cooked"
```

### SC-004: Consistent Formatting Across Views
*(FR-005)*

```gherkin
Given a recipe was cooked 10 days ago
When the user views the grid card
Then it shows "Cooked 10 days ago"
When the user opens the detail page
Then it shows the same relative phrasing (e.g., "Last cooked: 10 days ago")
```

### SC-005: Today's Cook Reflects Immediately
*(FR-006)*

```gherkin
Given the user finishes cooking a recipe via "Finished Cooking" (Story 019/023)
When the user returns to the browse grid
Then that recipe's card now shows "Cooked today"
without requiring a manual page refresh
```

### SC-006: Long Time Since Last Cooked
*(FR-003)*

```gherkin
Given a recipe was last cooked 8 months ago
When the user views the recipe
Then it displays an appropriately scaled relative time (e.g., "Cooked 8 months ago")
```

### SC-007: Cooked Multiple Times, Shows Most Recent Only
*(FR-003)*

```gherkin
Given a recipe has cook log entries from 3 days ago and 30 days ago
When the user views the last-cooked indicator
Then it displays based on the most recent entry only: "Cooked 3 days ago"
```

---

## 5. Domain Model

### 5.1 Entities

No new entities. This story is purely a read/display layer over:

#### CookLogEntry
_(Reused from Story 023)_ — queried for the most recent `cookedAt` per recipe.

### 5.2 Relationships

Unchanged from Story 023: a Recipe has many CookLogEntries; `lastCookedAt` is the max `cookedAt` among them.

### 5.3 Value Objects

#### RelativeTimeLabel

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| rawTimestamp | datetime | optional | The underlying `lastCookedAt`, or null if never cooked |
| displayText | string | derived | The human-readable relative label (e.g., "3 days ago", "Never cooked") |

### 5.4 Domain Rules and Invariants

- **Most recent cook only**: The displayed indicator always reflects the single most recent `cookedAt` entry, not the full history (Story 024 covers full history).
- **Single formatting source of truth**: Grid and detail views use the same relative-time formatting function to avoid inconsistent phrasing.
- **"Never cooked" is explicit**: A recipe with zero cook log entries is never shown with a blank or missing indicator — it always explicitly states "Never cooked."

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Last-cooked lookup adds no more than 50ms to grid or detail page load | Should not noticeably slow down Story 006/007 rendering |
| NFR-002 | Usability | Relative time labels are legible at small card sizes (e.g., abbreviations only where they remain clear) | Grid cards have limited space |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Recipe cooked less than a minute ago | Displays "just now" or "today" rather than a confusing sub-minute value |
| EC-002 | Recipe cooked exactly 7 days ago (week boundary) | Displays "7 days ago" or "1 week ago", per the shared formatting rule, consistently |
| EC-003 | Clock/timezone differences between client and server | Relative time is computed consistently using a single source of truth for "now" (document which — client or server) |
| EC-004 | Recipe deleted (Story 010) but had cook history | No indicator shown since the recipe itself no longer exists in the grid/detail views |
| EC-005 | Very large collection with many recipes needing last-cooked lookups | Lookups are batched/efficient, not one query per card |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | Last-cooked lookup adds < 50ms to page load |
| SC-003 | Formatting is consistent between grid and detail views |
| SC-004 | "Never cooked" displays correctly for recipes with no history |
| SC-005 | Indicator updates immediately after a new cook event (Story 023) |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 023 (cook log must exist to derive last-cooked date)
- Displayed in Story 006 (grid) and Story 007 (detail)
- Related to Story 016 ("surprise me" also uses recency data, but for exclusion logic rather than display)

### 9.2 Constraints

- Single-user, personal use
- Display-only; introduces no new write operations
- Most-recent-only display; full history is Story 024's responsibility

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Efficient batched lookup of last-cooked dates for grid rendering |
| 8. Cross-Cutting Concepts | Shared relative-time formatting utility |
| 10. Quality Requirements | Performance impact on existing grid/detail load times |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should relative time be computed client-side (using local clock) or server-side? | Architecture | Open | Recommend client-side computation from a stored UTC timestamp, for simplicity and timezone correctness |

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
