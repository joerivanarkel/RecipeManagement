# Feature Specification: Keep Screen Awake in Cook Mode

## 1. Overview

| Field | Value |
| --- | --- |
| Feature ID | 020 |
| Status | Draft |
| Author | Claude Code |
| Created | 2026-10-20 |
| Last updated | 2026-10-20 |
| Epic / Parent | Cook It (Story Map) |
| Arc42 reference | 5, 8, 10 |

### 1.1 Problem Statement

A phone screen that auto-locks mid-recipe is a small annoyance that becomes a real problem when the cook's hands are wet or covered in flour — unlocking the phone becomes genuinely difficult. Without a wake lock, Story 019's cook mode is actively disrupted by the device's own power-saving behavior.

### 1.2 Goal

Keep the device screen awake for the entire duration the cook is in step-by-step cook mode (Story 019), releasing the wake lock as soon as cook mode is exited so normal device battery behavior resumes.

### 1.3 Non-Goals

- Keeping the screen awake outside of cook mode (e.g., while browsing the grid)
- Adjusting screen brightness
- Preventing the device from sleeping system-wide (only the current page/session)

---

## 2. User Stories

### US-001: Prevent Screen Sleep While Cooking

**As a** home cook,
**I want** my phone screen to stay awake while I'm in cook mode,
**so that** my phone doesn't lock and become hard to use again with messy hands mid-recipe.

---

## 3. Functional Requirements

| ID | Requirement | Priority | User Story |
| --- | --- | --- | --- |
| FR-001 | The system shall request a screen wake lock when cook mode (Story 019) is entered | Must | US-001 |
| FR-002 | The system shall release the wake lock when cook mode is exited (via "Exit"/"Back" or "Finished Cooking") | Must | US-001 |
| FR-003 | The system shall re-acquire the wake lock if the browser/OS releases it automatically (e.g., due to tab visibility changes) while cook mode is still active | Should | US-001 |
| FR-004 | The system shall fail silently (no error shown to the user) if the wake lock API is unsupported on the current device/browser | Must | US-001 |
| FR-005 | The system shall release the wake lock if the user navigates away from cook mode by any means (including closing the tab) | Must | US-001 |

---

## 4. Acceptance Scenarios

### SC-001: Wake Lock Acquired on Entering Cook Mode
*(FR-001)*

```gherkin
Given the user clicks "Start Cooking" from recipe detail (Story 007)
When cook mode (Story 019) loads
Then a screen wake lock is requested and acquired
And the device screen does not auto-lock while cook mode remains open
```

### SC-002: Wake Lock Released on Exit
*(FR-002)*

```gherkin
Given the user is in cook mode with an active wake lock
When the user clicks "Exit" or "Finished Cooking"
Then the wake lock is released
And normal device auto-lock behavior resumes
```

### SC-003: Wake Lock Re-Acquired After Visibility Change
*(FR-003)*

```gherkin
Given the user is in cook mode with an active wake lock
When the user briefly switches to another app and returns to cook mode
Then the wake lock is re-acquired if it was released during the switch
```

### SC-004: Unsupported Browser Fails Silently
*(FR-004)*

```gherkin
Given the user's browser does not support the Wake Lock API
When the user enters cook mode
Then cook mode functions normally
And no error or warning is shown about the missing wake lock
```

### SC-005: Wake Lock Released on Tab Close
*(FR-005)*

```gherkin
Given the user is in cook mode with an active wake lock
When the user closes the browser tab
Then the wake lock is released by the browser automatically
And no lingering effect persists after the tab is closed
```

### SC-006: Wake Lock Not Requested Outside Cook Mode
*(FR-001)*

```gherkin
Given the user is browsing the recipe grid (Story 006) or viewing a detail page (Story 007)
Then no wake lock is requested
And normal device sleep behavior applies
```

---

## 5. Domain Model

### 5.1 Entities

No new persisted entities. This is a client-side, ephemeral device-API concern tied to the existing:

#### CookSession
_(Reused from Story 019)_ — wake lock lifecycle is scoped to the active cook session's duration.

### 5.2 Relationships

No new relationships.

### 5.3 Value Objects

No new value objects; wake lock state is a transient client-side handle, not domain data.

### 5.4 Domain Rules and Invariants

- **Wake lock lifecycle matches cook mode lifecycle exactly**: Acquired on entry, released on exit, with no persisted state and no effect outside the session.
- **Silent degradation**: Lack of wake lock support must never block or degrade the core cook mode experience (Story 019) — it is a pure enhancement.
- **No cross-session state**: Nothing about the wake lock is remembered between cook sessions; each entry into cook mode is a fresh acquisition attempt.

---

## 6. Non-Functional Requirements

| ID | Category | Requirement | Rationale |
| --- | --- | --- | --- |
| NFR-001 | Responsiveness | Wake lock is requested within 200ms of cook mode loading | Should not introduce any perceptible delay to Story 019's entry flow |
| NFR-002 | Responsiveness | Wake lock is released within 200ms of exiting cook mode | Avoids unnecessary battery drain after the user leaves |
| NFR-003 | Compatibility | Feature degrades gracefully (no crash, no error) on devices/browsers without Wake Lock API support | Broad device compatibility is expected for a kitchen-use feature |

---

## 7. Edge Cases and Error Scenarios

| ID | Scenario | Expected Behavior |
| --- | --- | --- |
| EC-001 | Device battery is critically low and the OS denies the wake lock request | Request fails silently; cook mode continues to function without the wake lock |
| EC-002 | User has multiple tabs open, one in cook mode and one not | Wake lock applies only to the tab actively in cook mode |
| EC-003 | Wake Lock API throws an unexpected error on request | Error is caught and logged internally; no user-facing disruption |
| EC-004 | User rapidly enters and exits cook mode multiple times | Each entry/exit cleanly acquires/releases without leaking a lingering lock |
| EC-005 | Device rotates or app is backgrounded briefly during cooking | Wake lock re-acquisition (FR-003) handles brief interruptions where supported |

---

## 8. Success Criteria

| ID | Criterion |
| --- | --- |
| SC-001 | All acceptance scenarios (SC-001 through SC-006) pass in CI (where testable; manual verification for actual device sleep behavior) |
| SC-002 | Wake lock is acquired within 200ms of cook mode load on supported devices |
| SC-003 | Wake lock is released within 200ms of exiting cook mode |
| SC-004 | No errors or crashes occur on unsupported browsers/devices |

---

## 9. Dependencies and Constraints

### 9.1 Dependencies

- Depends entirely on Story 019 (cook mode); has no independent entry point
- Tied to the CookSession lifecycle defined in Story 019

### 9.2 Constraints

- Relies on browser/OS support for a wake lock mechanism (e.g., Wake Lock API); behavior is best-effort on unsupported platforms
- Single-user, personal use; no server-side component

### 9.3 Architecture References

| Arc42 Section | Relevance |
| --- | --- |
| 5. Building Block View | Client-side device API integration |
| 8. Cross-Cutting Concepts | Graceful degradation patterns for unsupported browser features |
| 10. Quality Requirements | Responsiveness targets for acquire/release |

---

## 10. Open Questions

| # | Question | Owner | Status | Resolution |
| --- | --- | --- | --- | --- |
| 1 | Should the app detect and inform the user (subtly) when wake lock isn't supported, so they know to adjust device settings manually? | UX | Open | Recommend silent degradation per FR-004; revisit only if user feedback indicates confusion |

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
