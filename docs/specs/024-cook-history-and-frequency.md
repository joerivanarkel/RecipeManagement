# Feature Specification: Cook History and Frequency

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 024 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Cook It (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Story 017 shows a quick glance at the most recent cook date, but it can't answer "how often do I actually make this?" The cook log (Story 023) has the raw data, but nothing today surfaces the full history or aggregate frequency in one place.

### 1.2 Goal

Provide a dedicated cook history view for a recipe, listing every recorded cook date (most recent first) and summarizing frequency (e.g., total cooks, cooks this month, cooks this week), accessible from the recipe detail page (Story 007).

### 1.3 Non-Goals

- Editing or deleting individual cook log entries (the log remains append-only per Story 023)
- Cross-recipe analytics (e.g., "your most-cooked recipe overall")
- Exporting cook history

---

## 2. User Stories

### US-001: View Full Cook History

**As a** home cook,
**I want** to see my cook history and how often I've made something ("made 3x this month"),
**so that** I understand my actual cooking patterns for this recipe, not just the most recent date.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall provide a "Cook History" section or page accessible from the recipe detail page (Story 007) | Must | US-001 |
| FR-002 | The system shall list all cook log entries (Story 023) for the recipe, most recent first | Must | US-001 |
| FR-003 | The system shall display each entry's date and servings prepared | Should | US-001 |
| FR-004 | The system shall display aggregate statistics: total times cooked, times cooked this calendar month, times cooked this calendar week | Should | US-001 |
| FR-005 | The system shall display a message such as "Never cooked — start cooking to begin tracking" when no cook log entries exist | Must | US-001 |
| FR-006 | The system shall support scrolling or pagination for recipes with a long cook history (50+ entries) | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: View History with Multiple Entries
*(FR-001, FR-002, FR-003)*

```gherkin
Given a recipe has cook log entries on 2026-10-20, 2026-10-10, 2026-09-30
When the user opens the "Cook History" section from the recipe detail page
Then entries display most recent first:
  | date | servings |
  | 2026-10-20 | 2 |
  | 2026-10-10 | 4 |
  | 2026-09-30 | 2 |
```

### SC-002: Aggregate Statistics Displayed
*(FR-004)*

```gherkin
Given a recipe has been cooked 10 times total, 3 times this month, 1 time this week
When the user views the cook history
Then it displays: "Cooked 10 times total · 3 this month · 1 this week"
```

### SC-003: Never Cooked
*(FR-005)*

```gherkin
Given a recipe has no cook log entries
When the user opens "Cook History"
Then the page displays: "Never cooked — start cooking to begin tracking"
And no statistics or list are shown
```

### SC-004: Long History Scrolls
*(FR-006)*

```gherkin
Given a recipe has 75 cook log entries
When the user opens "Cook History"
Then the list is scrollable
And all 75 entries are reachable without performance issues
```

### SC-005: This-Month Count Resets at Month Boundary
*(FR-004)*

```gherkin
Given a recipe was cooked once on the last day of last month and once on the first day of this month
When the user views cook history on any day of this month
Then "this month" count reflects only the entry from the current calendar month (i.e., 1)
```

### SC-006: This-Week Count Resets at Week Boundary
*(FR-004)*

```gherkin
Given a recipe was cooked once last week and once this week
When the user views cook history
Then "this week" count reflects only the entry within the current calendar week (i.e., 1)
```

### SC-007: Single Cook Entry
*(FR-002, FR-004)*

```gherkin
Given a recipe has exactly one cook log entry, from today
When the user views cook history
Then the list shows that single entry
And statistics show: "Cooked 1 time total · 1 this month · 1 this week"
```

---

## 5. Domain Model

### 5.1 Entities

No new entities. This story is a read/aggregation layer over:

#### CookLogEntry
_(Reused from Story 023)_

### 5.2 Relationships

Unchanged from Story 023: a Recipe has many CookLogEntries.

### 5.3 Value Objects

#### CookHistorySummary

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| entries | array of CookLogEntry | ordered, most recent first | Full list of cook events for the recipe |
| totalCount | integer | derived, >= 0 | Total number of cook log entries |
| countThisMonth | integer | derived, >= 0 | Entries falling within the current calendar month |
| countThisWeek | integer | derived, >= 0 | Entries falling within the current calendar week |

### 5.4 Domain Rules and Invariants

- **Read-only view**: This story introduces no new writes; it only aggregates and displays data already captured by Story 023.
- **Calendar-based boundaries**: "This month" and "this week" use calendar month/week boundaries (not a rolling 30-day/7-day window), for predictable, explainable counts.
- **Most-recent-first ordering**: The entry list is always sorted descending by `cookedAt`.
- **Statistics derive from the full entry list**: No separate denormalized counters are required; totals are computed from CookLogEntry data at query time (or cached/derived as an architecture decision).

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Cook history loads and aggregates in < 300ms for recipes with up to 500 entries | Should feel responsive even for frequently-made recipes tracked over years |
| NFR-002 | Correctness | Month/week boundary calculations respect the user's local calendar, not UTC-only boundaries, to avoid off-by-one-day confusion | Avoids showing incorrect "this week" counts near timezone edges |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Recipe is cooked at 11:59 PM on the last day of the month | Counted in that month, not the next, per local calendar boundary |
| EC-002 | Recipe has entries spanning multiple years | Full history still lists all entries; "this month"/"this week" only ever reflect the current period |
| EC-003 | Recipe deleted (Story 010) but cook log preserved | Cook history view is only reachable via the recipe detail page, so a deleted recipe has no accessible history view (data exists but is not orphan-browsable in MVP) |
| EC-004 | Two cook entries on the exact same day | Both appear as separate list entries, ordered by exact timestamp (most recent first) |
| EC-005 | User has cooked a recipe in a future-dated entry due to a clock error (extreme edge case) | Entry still displays; aggregation counts it if it falls within calculated boundaries, no special validation added here (Story 023 owns entry creation) |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | History loads and aggregates in < 300ms for up to 500 entries |
| SC-003 | Month/week counts are accurate against calendar boundaries |
| SC-004 | "Never cooked" state displays correctly for untracked recipes |
| SC-005 | Long histories (50+ entries) remain scrollable and performant |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 023 (cook log is the sole data source)
- Accessible from Story 007 (recipe detail page)
- Complements Story 017 (which shows only the most-recent date; this story shows the full picture)

### 9.2 Constraints

- Single-user, personal use
- Read-only; no new write operations
- No cross-recipe aggregation (per-recipe view only)

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Aggregation query patterns over CookLogEntry |
| 8. Cross-Cutting Concepts | Calendar boundary calculation (month/week), timezone handling |
| 10. Quality Requirements | Performance of aggregation for high-frequency recipes |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should "this week" start on Sunday or Monday, and should this be configurable? | Product | Open | Recommend following the user's locale default (typically Monday in most non-US locales); fixed for MVP |
| 2 | Should cook history be a separate page or an expandable section on the recipe detail page? | UX | Open | Recommend an expandable section for MVP simplicity; a dedicated page can follow if content grows |

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
