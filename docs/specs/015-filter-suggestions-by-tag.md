# Feature Specification: Filter Suggestions by Tag

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 015 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Decide What to Cook Tonight (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

The "what can I cook" ranking (Story 014) answers "what's available" but not "what am I in the mood for." A cook who wants something specifically vegetarian or Asian has no way to narrow the ranked list down to just those options.

### 1.2 Goal

Let the cook filter the Story 014 results by one or more tags (from Story 008), narrowing the ranked list to recipes matching the selected tags while preserving the availability ranking.

### 1.3 Non-Goals

- Tag management (creating/renaming tags — that's Story 008)
- Saved/default filter presets
- Filtering by anything other than tags (e.g., prep time) in this story

---

## 2. User Stories

### US-001: Filter "What Can I Cook" Results by Tag

**As a** home cook,
**I want** to filter suggestions by tag,
**so that** I can ask for something vegetarian or something Asian instead of scanning the whole ranked list.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display selectable tag filter chips above the Story 014 results, populated from tags in use across the collection | Must | US-001 |
| FR-002 | The system shall allow selecting one or more tags simultaneously | Must | US-001 |
| FR-003 | The system shall narrow results to recipes that have ALL selected tags (AND semantics) | Must | US-001 |
| FR-004 | The system shall preserve the availability ranking (Story 014) within the filtered results | Must | US-001 |
| FR-005 | The system shall update filtered results immediately as tag selections change | Should | US-001 |
| FR-006 | The system shall display a "No recipes match these tags" message when the filter excludes everything | Should | US-001 |
| FR-007 | The system shall allow clearing all tag filters to return to the full ranked list | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Filter by a Single Tag
*(FR-001, FR-002, FR-003, FR-004)*

```gherkin
Given the ranked results (Story 014) include recipes tagged "vegetarian" and others not
When the user selects the "vegetarian" tag filter
Then only recipes tagged "vegetarian" remain in the results
And they retain their relative availability ranking
```

### SC-002: Filter by Multiple Tags (AND Semantics)
*(FR-002, FR-003)*

```gherkin
Given recipes:
  | recipe | tags |
  | Veggie Stir-Fry | vegetarian, quick, asian |
  | Veggie Lasagna | vegetarian, italian |
  | Chicken Stir-Fry | quick, asian |
When the user selects both "vegetarian" and "asian" tag filters
Then only "Veggie Stir-Fry" appears (it has both tags)
```

### SC-003: No Matches for Selected Tags
*(FR-006)*

```gherkin
Given no recipe has both "vegan" and "dessert" tags
When the user selects both filters
Then the page displays: "No recipes match these tags"
```

### SC-004: Clear All Filters
*(FR-007)*

```gherkin
Given the user has selected "vegetarian" and "quick" filters
When the user clicks "Clear filters"
Then all tag selections are removed
And the full ranked list (Story 014) is shown again
```

### SC-005: Live Update on Filter Change
*(FR-005)*

```gherkin
Given the user has selected "vegetarian"
When the user additionally selects "quick"
Then the results immediately narrow further without a page reload
```

### SC-006: Tag Chips Reflect Collection Vocabulary
*(FR-001)*

```gherkin
Given the collection uses tags "vegetarian", "quick", "asian", "italian" across various recipes
When the user views the filter chips
Then exactly those four tags are available for selection
And no tags that aren't used by any recipe are shown
```

### SC-007: Deselect a Single Tag
*(FR-002, FR-005)*

```gherkin
Given the user has selected "vegetarian" and "quick"
When the user deselects "quick"
Then results update to show recipes matching "vegetarian" only
```

---

## 5. Domain Model

### 5.1 Entities

No new entities. This story queries existing:

#### Recipe and Tag
_(Reused from Story 001 and Story 008)_

#### RecipeAvailability
_(Reused from Story 014)_ — the ranked result set being filtered.

### 5.2 Relationships

- Unchanged: a Recipe has zero or more Tags (Story 008).

### 5.3 Value Objects

#### TagFilterSelection

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| selectedTags | array of string | optional | Currently selected tag names for filtering |

### 5.4 Domain Rules and Invariants

- **AND semantics across selected tags**: A recipe must have every selected tag to appear in filtered results, not just one.
- **Ranking is preserved under filtering**: Filtering narrows the Story 014 result set but does not change the relative availability-based order among remaining recipes.
- **Filter chips reflect actual usage**: Only tags currently applied to at least one recipe appear as filter options.
- **Filter state is session/page-level**: Tag selections are not persisted as a saved preference (no "default filter" in this story).

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Filtering updates results in < 150ms after a tag selection change | Should feel instant, consistent with Story 014's live-update expectations |
| NFR-002 | Usability | Selected tag chips are visually distinguishable from unselected ones | User must be able to see their current filter state at a glance |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | User selects a tag, then the last recipe with that tag is deleted (Story 010) | Filter chip disappears or filter yields empty results, handled gracefully |
| EC-002 | Collection has no tags at all | Filter chip area is empty or hidden; all results shown by default |
| EC-003 | User selects every available tag | Likely narrows to zero or very few results; "no matches" message shown as appropriate |
| EC-004 | Tag names are very long | Chips wrap or truncate with ellipsis, remaining tappable |
| EC-005 | Many tags exist (30+) | Chip area scrolls or wraps; all remain selectable |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-007) pass in CI |
| SC-002 | Filtering updates in < 150ms |
| SC-003 | AND semantics across multiple selected tags work correctly |
| SC-004 | Availability ranking order is preserved within filtered results |
| SC-005 | Clearing filters restores the full Story 014 ranked list |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 008 (tags must exist on recipes)
- Depends on Story 014 (filters narrow the availability-ranked result set)

### 9.2 Constraints

- Single-user, personal use
- AND semantics only (no OR/any-of toggle in this story)
- No saved/default filter presets

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Composition of filtering on top of Story 014's ranking query |
| 8. Cross-Cutting Concepts | Reuse of tag data and matching patterns |
| 10. Quality Requirements | Responsiveness of combined filter + ranking |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should multi-tag selection use AND or OR semantics by default? | Product | Open | Recommend AND (narrowing) based on story map phrasing; revisit based on user feedback |
| 2 | Should the most frequently used tags be prioritized/sorted first in the chip list? | UX | Open | Recommend alphabetical for MVP simplicity |

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
