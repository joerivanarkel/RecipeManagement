# Feature Specification: Start a Timer from a Cook Mode Step

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 021 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Cook It (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

Many recipe steps mention a duration ("simmer 20 minutes"), and today the cook has to use a separate phone timer app or just guess. This breaks the hands-free flow Story 019's cook mode is meant to provide.

### 1.2 Goal

Detect duration mentions within the current cook mode step's text and offer a one-tap "Start Timer" action that runs a countdown with an audible/vibration alert at zero, without leaving cook mode.

### 1.3 Non-Goals

- Natural-language understanding beyond simple duration pattern matching (e.g., "a little while" is not detected)
- Multiple simultaneous active timers with a dedicated timer-management screen (one highlighted active timer at a time; see edge cases for stacking)
- Scheduling or background push notifications after the app/tab is fully closed

---

## 2. User Stories

### US-001: Start a Timer from a Step

**As a** home cook,
**I want** to start a timer directly from a step that mentions a duration ("simmer 20 min"),
**so that** I don't have to switch to a separate timer app while my hands are busy cooking.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall scan the current cook mode step's text for duration patterns (e.g., "20 minutes", "20 min", "2 hours", "30 sec") | Must | US-001 |
| FR-002 | The system shall display a "Start Timer" button when a duration is detected, pre-filled with the detected duration | Must | US-001 |
| FR-003 | The system shall start a visible countdown when the button is tapped, showing remaining time | Must | US-001 |
| FR-004 | The system shall alert the user audibly and/or via vibration when the countdown reaches zero | Must | US-001 |
| FR-005 | The system shall allow the user to pause, resume, or cancel an active timer | Should | US-001 |
| FR-006 | The system shall display multiple "Start Timer" buttons if a step mentions multiple distinct durations | Should | US-001 |
| FR-007 | The system shall keep an active timer running and visible even if the user navigates to a different step within cook mode | Should | US-001 |
| FR-008 | The system shall not display a timer button when no duration is detected in the step text | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Detect and Start a Simple Duration
*(FR-001, FR-002, FR-003)*

```gherkin
Given the current cook mode step reads: "Simmer for 20 minutes, stirring occasionally"
When the step is displayed
Then a "Start Timer (20:00)" button appears
When the user taps it
Then a countdown begins from 20:00 and counts down in real time
```

### SC-002: Alert at Zero
*(FR-004)*

```gherkin
Given a timer is running and reaches 00:00
Then the device vibrates and/or plays an audible alert
And the countdown display indicates the timer has finished
```

### SC-003: Pause and Resume
*(FR-005)*

```gherkin
Given a timer is running at 15:32 remaining
When the user taps "Pause"
Then the countdown stops at 15:32
When the user taps "Resume"
Then the countdown continues from 15:32
```

### SC-004: Cancel a Timer
*(FR-005)*

```gherkin
Given a timer is running
When the user taps "Cancel"
Then the countdown stops and is removed
And the "Start Timer" button becomes available again for that step
```

### SC-005: Multiple Durations in One Step
*(FR-006)*

```gherkin
Given the current step reads: "Boil for 2 minutes, then simmer for 20 minutes"
When the step is displayed
Then two buttons appear: "Start Timer (2:00)" and "Start Timer (20:00)"
And each can be started independently
```

### SC-006: No Duration Detected
*(FR-008)*

```gherkin
Given the current step reads: "Add salt and pepper to taste"
When the step is displayed
Then no "Start Timer" button appears
```

### SC-007: Timer Persists Across Step Navigation
*(FR-007)*

```gherkin
Given the user starts a 20-minute timer on Step 3
When the user navigates to Step 4
Then the timer continues counting down
And remains visible (e.g., in a persistent header/footer area) regardless of which step is shown
```

### SC-008: Various Duration Formats Detected
*(FR-001)*

```gherkin
Given steps containing: "bake 1 hour", "rest 5 mins", "blanch for 30 seconds", "cook 1.5 hours"
When each step is displayed
Then a correctly-durationed "Start Timer" button appears for each format
```

### SC-009: Hour-and-Minute Combination
*(FR-001)*

```gherkin
Given a step reads: "Slow cook for 1 hour 30 minutes"
When the step is displayed
Then a "Start Timer (1:30:00)" button appears with the combined duration
```

---

## 5. Domain Model

### 5.1 Entities

No new persisted entities — timers are ephemeral, client-side/session state tied to the active cook session.

#### Timer
_(Transient value object)_

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| id | UUID | generated, client-side | Identifier for this timer instance |
| stepId | UUID | required | The Step (Story 001/019) this timer was started from |
| durationSeconds | integer | required, > 0 | Total duration detected/set |
| remainingSeconds | integer | derived | Time left in the countdown |
| status | enum | one of: RUNNING, PAUSED, FINISHED, CANCELLED | Current timer state |
| startedAt | datetime | generated | When the timer was started (or resumed) |

### 5.2 Relationships

- A **Timer** references the **Step** (Story 001) whose text triggered it.
- Multiple Timers may exist concurrently within a single **CookSession** (Story 019), one per started duration.

### 5.3 Value Objects

#### DetectedDuration

| Attribute | Type | Constraints | Description |
| --- | --- | --- | --- |
| rawMatch | string | required | The matched text from the step (e.g., "20 minutes") |
| durationSeconds | integer | required, > 0 | Parsed duration in seconds |

### 5.4 Domain Rules and Invariants

- **Timers are not persisted**: All timer state is in-memory/session-only; closing the app/tab loses active timers (per Non-Goals — no background push notification infrastructure in this story).
- **Detection is pattern-based, not NLP**: Duration detection relies on recognizable numeric + unit patterns (minutes, mins, hours, hrs, seconds, secs), not free-form language understanding.
- **One button per distinct detected duration**: A step with multiple distinct durations yields multiple independent timer buttons/instances.
- **Timers persist across step navigation within the same cook session**: Navigating steps does not cancel a running timer.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Performance | Duration detection on step text completes in < 50ms | Should not introduce any perceptible delay when navigating steps in Story 019 |
| NFR-002 | Reliability | Countdown accuracy stays within 1 second of real elapsed time over a 1-hour timer | Cooking timing matters; drift must be minimal |
| NFR-003 | Accessibility | Timer alert includes both audible and vibration feedback where supported, since the user may not be looking at the screen | User's attention is on the stove, not the phone |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Step mentions a non-cooking duration (e.g., "let rest for 2 minutes before serving" at the very last step) | Still detected and offered as a timer; no semantic filtering of "relevant" vs "irrelevant" durations |
| EC-002 | Step mentions an ambiguous number without units (e.g., "wait 20") | Not detected as a duration; no timer button shown (unit is required for a match) |
| EC-003 | User leaves cook mode entirely while a timer is running | Timer is cancelled/lost, consistent with the no-persistence rule; acceptable per Non-Goals |
| EC-004 | Device does not support vibration API | Falls back to audible alert only; no error shown |
| EC-005 | Device is muted and does not support vibration | Visual alert (e.g., flashing/color change) is the final fallback |
| EC-006 | User starts the same step's timer twice | A second independent timer instance starts; both run concurrently (no restriction on duplicate timers for the same step) |
| EC-007 | Very long duration detected (e.g., "marinate for 24 hours") | Timer accepts and displays long durations correctly (e.g., "24:00:00") |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-009) pass in CI |
| SC-002 | Duration detection completes in < 50ms per step |
| SC-003 | Countdown accuracy stays within 1 second per hour of elapsed time |
| SC-004 | Alerts (audible/vibration) fire reliably at zero |
| SC-005 | Multiple durations in a single step each produce an independent, correctly-labeled timer button |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends entirely on Story 019 (cook mode); timers are displayed and interacted with inside cook mode's step view
- No dependency on Story 023 (cook log) or any other story

### 9.2 Constraints

- Single-user, personal use
- No persistence of timer state; app/tab closure loses in-progress timers
- Detection is regex/pattern-based, not a full NLP duration parser
- No background/push notifications when the app is not in the foreground

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Client-side timer and duration-detection implementation |
| 8. Cross-Cutting Concepts | Pattern-matching approach, alert/notification fallback chain |
| 10. Quality Requirements | Countdown accuracy and detection performance targets |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should timers survive if the user backgrounds the browser tab (via a service worker or similar), or is in-tab-only acceptable for MVP? | Architecture | Open | Recommend in-tab-only for MVP; background timer survival is a larger undertaking for a future story |
| 2 | Should there be a visible list of all currently-running timers across steps, not just the one tied to the current step view? | UX | Open | Recommend a persistent compact timer bar showing all active timers regardless of current step |

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
