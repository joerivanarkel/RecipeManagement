# Feature Specification: "Surprise Me" Skipping Recently Cooked Recipes

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 016 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Decide What to Cook Tonight (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Even with ranked availability (Story 014), a cook can still default to the same two or three recipes out of habit. Without a nudge toward variety, the "what can I make" loop goes stale and repeats the same meals.

### 1.2 Goal

Provide a "Surprise Me" action that picks one recipe at random from the available (Story 014) results, excluding anything cooked within a recent window (using the cook log from Story 023), to encourage variety.

### 1.3 Non-Goals

- User-configurable recency windows (fixed default for MVP)
- Weighted randomness based on preference/rating (not part of the domain model)
- Multiple "surprise" picks per click (one recipe per action)

---

## 2. User Stories

### US-001: Get a Random Suggestion That Skips Recent Repeats

**As a** home cook,
**I want** to hit "surprise me" and get a suggestion that skips anything I've cooked recently,
**so that** I don't fall into a rut of making the same few recipes over and over.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall display a "Surprise Me" button on the "what can I cook" page (Story 014) | Must | US-001 |
| FR-002 | The system shall, when clicked, select one recipe at random from the current (possibly tag-filtered, Story 015) availability-ranked results | Must | US-001 |
| FR-003 | The system shall exclude recipes cooked within the last 7 days (default recency window) from the random selection pool | Must | US-001 |
| FR-004 | The system shall weight the random selection toward recipes with higher ingredient availability (favor recipes closer to 100% available) | Should | US-001 |
| FR-005 | The system shall fall back to the full pool (including recently cooked recipes) if excluding recent ones leaves zero candidates, and shall inform the user this happened | Must | US-001 |
| FR-006 | The system shall navigate to or highlight the chosen recipe's detail page (Story 007) after selection | Should | US-001 |
| FR-007 | The system shall allow the user to click "Surprise Me" again to get a different pick | Should | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Surprise Pick Excludes Recently Cooked
*(FR-001, FR-002, FR-003)*

```gherkin
Given available recipes: "Stir-Fry" (cooked 2 days ago), "Pasta" (cooked 30 days ago), "Soup" (never cooked)
When the user clicks "Surprise Me"
Then the pick is randomly selected from "Pasta" and "Soup" only
And "Stir-Fry" is excluded (cooked within the last 7 days)
```

### SC-002: Fallback When All Recipes Are Recent
*(FR-005)*

```gherkin
Given every available recipe was cooked within the last 7 days
When the user clicks "Surprise Me"
Then the system selects from the full pool anyway
And displays a note: "You've cooked everything recently — here's a pick anyway"
```

### SC-003: Weighted Toward Higher Availability
*(FR-004)*

```gherkin
Given candidate recipes with availability 100%, 80%, and 40%
When "Surprise Me" is clicked repeatedly many times
Then the 100% and 80% available recipes are selected more often than the 40% one
(Note: still random — not deterministic — but statistically weighted)
```

### SC-004: Navigate to Chosen Recipe
*(FR-006)*

```gherkin
Given the user clicks "Surprise Me" and "Garlic Pasta" is selected
Then the app navigates to or highlights "Garlic Pasta" detail page (Story 007)
```

### SC-005: Click Again for a Different Pick
*(FR-007)*

```gherkin
Given the user clicked "Surprise Me" and received "Garlic Pasta"
When the user clicks "Surprise Me" again
Then a new random pick is made (may or may not be different, since it's random, but repeated clicks should not get stuck returning the exact same recipe every time by design)
```

### SC-006: No Available Recipes At All
*(FR-002)*

```gherkin
Given the "what can I cook" results are empty (e.g., empty pantry and no recipes have 0 required ingredients)
When the user clicks "Surprise Me"
Then the system displays: "Nothing to suggest yet — add some pantry items or recipes"
```

### SC-007: Respects Active Tag Filter
*(FR-002)*

```gherkin
Given the user has filtered results to "vegetarian" (Story 015)
When the user clicks "Surprise Me"
Then the pick is selected only from vegetarian-tagged, available recipes
```

### SC-008: Only One Candidate Available
*(FR-002)*

```gherkin
Given only one recipe remains after excluding recently cooked ones
When the user clicks "Surprise Me"
Then that one recipe is selected deterministically (no randomness needed with a single candidate)
```

---

## 5. Domain Model

### 5.1 Entities

No new entities. This story operates on existing:

#### Recipe, RecipeAvailability
_(Reused from Story 001, Story 014)_

#### CookLogEntry
_(Reused from Story 023)_ — used to determine `lastCookedAt` for recency exclusion.

### 5.2 Relationships

Unchanged from Story 014 and Story 023.

### 5.3 Value Objects

#### SurpriseCandidatePool

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| candidates | array of RecipeAvailability | derived | Recipes eligible for random selection after recency exclusion |
| usedFallback | boolean | derived | True if the recency exclusion emptied the pool and the full pool was used instead |

### 5.4 Domain Rules and Invariants

- **Recency window default is 7 days**: A recipe is excluded from the primary candidate pool if its `lastCookedAt` (from Story 023's cook log) falls within the last 7 days.
- **Fallback never returns "nothing"**: If recency exclusion empties the pool, the system falls back to the full available pool rather than showing no suggestion.
- **Weighting favors availability, not certainty**: Higher-availability recipes are more likely but not guaranteed to be picked; selection remains random.
- **Respects active filters**: The candidate pool is drawn from whatever the user is currently viewing (including any Story 015 tag filter), not the unfiltered full collection.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | A surprise pick is generated in < 200ms | Should feel instant and encourage repeated use |
| NFR-002 | Fairness | Over many picks, selection distribution should not be so skewed that only one or two recipes are ever chosen (barring small pools) | Keeps the feature feeling genuinely surprising |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Only one recipe exists in the entire collection | That recipe is always picked; no randomness needed |
| EC-002 | Pantry is empty (Story 012) so no recipe has any availability | "Surprise Me" still works if recipes exist, treating 0% availability candidates as eligible; or shows "add pantry items" message per Story 014's own empty-state handling |
| EC-003 | Cook log has no entries for any recipe (nothing ever cooked) | No recipes are excluded by recency; full pool is eligible |
| EC-004 | Tag filter active (Story 015) narrows pool to zero recipes before recency exclusion even applies | "Surprise Me" reflects the same "no matches" state as Story 015 |
| EC-005 | Recipe is deleted between page load and clicking "Surprise Me" | Candidate pool is recalculated at click time, avoiding stale references |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-008) pass in CI |
| SC-002 | A surprise pick is generated in < 200ms |
| SC-003 | Recently cooked recipes (within 7 days) are excluded except when that would empty the pool |
| SC-004 | Selection respects active tag filters (Story 015) |
| SC-005 | Fallback behavior is clearly communicated to the user when triggered |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends on Story 014 (availability-ranked pool to draw from)
- Depends on Story 023 (cook log for recency exclusion)
- Optionally respects Story 015 (active tag filter)
- Navigates to Story 007 (recipe detail) on selection

### 9.2 Constraints

- Single-user, personal use
- Fixed 7-day recency window for MVP (not user-configurable)
- True randomness within the weighted pool; no deterministic "smart" selection beyond the availability weighting

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Composition with Story 014's ranking and Story 023's cook log |
| 8. Cross-Cutting Concepts | Randomization approach, fallback handling |
| 10. Quality Requirements | Responsiveness of the pick action |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should the 7-day recency window be configurable by the user in a future story? | Product | Open | Recommend fixed default for MVP; revisit if users request tuning |
| 2 | Should "Surprise Me" exclude the immediately-previous surprise pick to avoid back-to-back repeats? | UX | Open | Recommend yes as a simple improvement, but not a hard requirement for MVP |

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
