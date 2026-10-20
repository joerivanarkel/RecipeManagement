# Feature Specification: Mark Recipe as Cooked

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 023 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Cook It (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

After cooking a recipe, the app needs to remember when and how often the cook has made it. Without this, Story 016 (recency filtering) and Story 017 (cook frequency display) have no data, and the decision-making loop (Story 014 + 016) is incomplete.

### 1.2 Goal

Record when a recipe is cooked by storing a timestamp in a cook log. Display cook frequency (e.g., "made 3x this month") and last-cooked date on recipe cards and detail pages. Enable Story 016 (surprise me / skip recent) and Story 017 (show when last cooked).

### 1.3 Non-Goals

- Detailed cooking notes or modifications
- Photo documentation of cooked dishes
- Sharing cook history
- Batch cook operations (one recipe at a time)

---

## 2. User Stories

### US-001: Record Cook Event

**As a** home cook,
**I want** the app to record that I just cooked a recipe (when I click "Finished Cooking" in cook mode),
**so that** the app learns my patterns and can suggest recipes I haven't made recently.

### US-002: View Cook History

**As a** home cook,
**I want** to see how often I've cooked a recipe and when I last made it,
**so that** I can avoid repetition and keep my meals varied.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall create a cook log entry when user clicks "Finished Cooking" in cook mode (Story 019) | Must | US-001 |
| FR-002 | The system shall store cook log entry with: recipe ID, date/time cooked, and servings prepared | Must | US-001 |
| FR-003 | The system shall persist cook log to durable storage | Must | US-001 |
| FR-004 | The system shall calculate and display cook frequency (e.g., "made 3x this month", "last cooked 5 days ago") | Should | US-002 |
| FR-005 | The system shall display last-cooked date on recipe detail page (Story 007) | Should | US-002 |
| FR-006 | The system shall display cook frequency on recipe cards in browse grid (Story 006) or detail view | Could | US-002 |
| FR-007 | The system shall allow user to manually log a past cook event (optional; for recipes cooked before app existed) | Could | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Record Cook in Cook Mode
*(FR-001, FR-002, FR-003)*

```gherkin
Given the user is in cook mode (Story 019)
When the user clicks "Finished Cooking"
Then a confirmation dialog appears: "Mark this recipe as cooked?"
When the user confirms
Then a cook log entry is created:
  | field | value |
  | recipeId | [recipe ID] |
  | cookedAt | [current timestamp] |
  | servings | [scaled servings from cook mode] |
And cook mode closes
And the user is returned to the recipe list or detail page
```

### SC-002: Display Last-Cooked Date
*(FR-005)*

```gherkin
Given a recipe has been cooked on 2026-10-15
When the user views the recipe detail page
Then it displays: "Last cooked: 5 days ago" (or "2026-10-15")
```

### SC-003: Display Cook Frequency
*(FR-004)*

```gherkin
Given a recipe has been cooked:
  | date |
  | 2026-10-20 |
  | 2026-10-10 |
  | 2026-09-30 |
When the user views the recipe detail page
Then it displays: "Cooked 3 times"
And optionally: "Cooked 2x this month"
```

### SC-004: First Cook Entry
*(FR-001, FR-003)*

```gherkin
Given a recipe has never been cooked
When the user finishes cooking it for the first time
Then a cook log entry is created
And the recipe now shows: "Last cooked: today"
```

### SC-005: Multiple Cook Sessions
*(FR-002, FR-003)*

```gherkin
Given a recipe has been cooked 5 times with various servings:
  | date | servings |
  | 2026-10-20 | 2 |
  | 2026-10-10 | 4 |
  | 2026-09-30 | 2 |
When the user views cook history
Then all entries are recorded with correct dates and servings
And frequency displays: "Cooked 3 times"
```

### SC-006: Same Recipe Cooked Multiple Times Same Day
*(FR-001)*

```gherkin
Given a recipe is cooked twice on 2026-10-20 (morning and evening)
When the user finishes cooking both times
Then both cook log entries are created with different timestamps
And frequency display shows accurate count
```

### SC-007: Cook Frequency Over Time
*(FR-004)*

```gherkin
Given a recipe has been cooked on various dates over 3 months
When the user views the recipe
Then the app displays:
  - Last cooked: [relative time, e.g., "5 days ago"]
  - Frequency: [count, e.g., "Cooked 10 times"]
  - Optional: Time-based frequency [e.g., "3x this month", "1x last week"]
```

---

## 5. Domain Model

### 5.1 Entities

#### CookLogEntry

Represents a single instance of cooking a recipe.

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK, generated | Unique cook log entry ID |
| recipeId | UUID | FK, required | Recipe that was cooked |
| cookedAt | datetime | required, immutable | When the recipe was cooked |
| servings | integer | required, > 0 | How many servings were prepared |

#### Recipe
_(Extended from Story 001)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | PK | Recipe identifier |
| title | string | required | Recipe name |
| cookLog | array | CookLogEntry[] | All cook events for this recipe |
| lastCookedAt | datetime | optional, derived | Timestamp of most recent cook |
| totalCooks | integer | derived | Count of cook log entries |

### 5.2 Relationships

- A **Recipe** has many **CookLogEntries** (one-to-many).
- Each **CookLogEntry** references exactly one **Recipe**.

### 5.3 Value Objects

#### CookFrequency

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| totalCount | integer | >= 0 | Total times cooked |
| lastCookedAt | datetime | optional | Timestamp of last cook |
| lastCookedRelative | string | e.g., "5 days ago", "today" | Human-readable relative time |
| frequencyThisMonth | integer | optional | Cooks in current month |
| frequencyThisWeek | integer | optional | Cooks in current week |

### 5.4 Domain Rules and Invariants

- **Cook log is append-only**: Entries are never edited or deleted (immutable audit trail).
- **Timestamp is immutable**: Once recorded, the `cookedAt` timestamp cannot change.
- **Last-cooked is derived**: Calculated from cook log, not stored separately.
- **Frequency is calculated**: Totals and time-based frequencies are computed on-demand from cook log.
- **Servings are recorded**: Each cook event captures the servings prepared (from Story 019 cook mode).

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Cook log entry is created in < 100ms | Finish cooking shouldn't feel slow |
| NFR-002 | Correctness | Cook log entries are persisted atomically | Audit trail must be reliable |
| NFR-003 | Accessibility | Frequency display is human-readable ("5 days ago", not "432000 seconds") | User should understand patterns at a glance |
| NFR-004 | Reliability | Cook log persists across app restarts | History is durable and always available |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User clicks "Finished Cooking" but network fails | Cook entry is queued or stored locally; synced on next connection |
| EC-002 | Recipe is deleted; cook log still references it | Cook log entry remains (no referential deletion); history is preserved |
| EC-003 | Same recipe cooked at 11:59 PM then 12:01 AM next day | Both entries recorded with correct dates; frequency displays accurately |
| EC-004 | Cook log has 1000+ entries for a single recipe | Display is efficient; aggregations are fast |
| EC-005 | User tries to record cook for recipe that no longer exists | Error displayed; cook log entry is not created (or linked to archived recipe) |
| EC-006 | Servings passed from cook mode is invalid (0 or negative) | Use original recipe servings as fallback; log entry still created |
| EC-007 | Cook frequency calculation includes today vs. excludes today | Clarify behavior: "3x this month" includes/excludes current day? (recommend inclusive) |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | Cook log entry is created in < 100ms |
| SC-003 | Cook log persists across app restarts |
| SC-004 | Cook frequency is accurately calculated from cook log |
| SC-005 | Last-cooked date and frequency display on recipe detail and/or browse grid |
| SC-006 | Cook log is append-only; entries are never modified or deleted |
| SC-007 | Relative time display is human-readable ("5 days ago", "today", "last week", etc.) |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 019 (cook mode triggers cook log creation via "Finished Cooking")
- Feeds into Story 016 (recency filter uses cook log to skip recently made recipes)
- Feeds into Story 017 (cook frequency and last-cooked date display)
- Optional: Story 007 can display last-cooked and frequency on recipe detail

### 9.2 Constraints

- Single-user, personal use (no multi-user cook tracking)
- Cook log is immutable (no edits or deletes; only append)
- Timestamps are server-time or client-time (clarify in architecture)
- No bulk cook operations (one recipe at a time)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Cook log storage and retrieval; audit trail patterns |
| 8. Cross-Cutting Concepts | Timestamp handling, immutability, data durability |
| 10. Quality Requirements | Performance and reliability for cook tracking |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should cook log entries be editable or deletable if user made a mistake? | Product | Open | Recommend append-only (no edits); user can't delete entries |
| 2 | Should cook frequency reset monthly, or show all-time totals? | Product | Open | Recommend both (e.g., "10 times total, 3 this month") |
| 3 | If recipe is deleted, should associated cook log be deleted too? | Data | Open | Recommend keeping cook log (historical audit trail); soft-delete recipe |

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
