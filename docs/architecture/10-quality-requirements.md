# 10. Quality Requirements

This section is the project-wide reference point for quality requirements. Individual
feature specs in `docs/specs/*.md` define **feature-specific** NFRs in their own
Section 6 and explicitly point back here for cross-cutting goals (see
`.claude/skills/spec/spec-template.md`: "Only include NFRs specific to this feature.
Project-wide NFRs should live in your arc42 Section 10 ... and be referenced, not
duplicated"). The tree and table below aggregate the recurring NFR themes found across
the specs rather than repeating every per-feature number.

**Implementation status caveat**: only **Story 001 (add recipe manually)**, **Story 006
(browse recipes grid)**, and **Story 007 (view recipe detail)** are built in `src/` as of
this writing. The remaining stories referenced below (012, 014, 019, 020, etc.) are
specified in `docs/specs/` but not yet implemented — their quality scenarios describe
intended behavior for the system the specs define, not verified, measured behavior. Each
scenario in the table is marked **Implemented** or **Specified** accordingly. Where a
scenario is Implemented, it is backed by code (`RecipeValidator.cs`, `RecipeService.cs`)
and/or a test (`RecipeValidatorTests.cs`, `RecipeServiceTests.cs`) — see the Source
column for the exact reference.

## Quality Tree

<!-- Hierarchical breakdown of quality goals into concrete scenarios -->

```
Quality
├── Performance & Responsiveness
│   ├── QS-01 Recipe save completes in < 500ms p95 (Story 001 — Implemented)
│   ├── QS-02 Recipe detail loads < 500ms; servings recalculation < 100ms (Story 007 — Implemented)
│   ├── QS-03 Browse grid renders < 2s for 100+ recipes (Story 006 — Implemented)
│   ├── QS-04 Pantry-match results load < 500ms for 100 recipes / 50 pantry items (Story 014 — Specified)
│   ├── QS-05 Cook-mode step navigation responds < 100ms (Story 019 — Specified)
│   └── QS-06 Search results update < 200ms per keystroke, debounced (Story 009 — Specified)
├── Usability & Kitchen Accessibility
│   ├── QS-07 Forms and grids are keyboard-navigable (Stories 001, 006, 007, 010 — Implemented for 001/006/007 forms; Specified for 010)
│   ├── QS-08 Cook mode has large touch targets and 18pt+ high-contrast text, readable across the kitchen (Story 019 — Specified)
│   └── QS-09 Layout is responsive from desktop (3-4 col) down to mobile (1-2 col) (Stories 006, 007 — Implemented)
├── Data Integrity & Reliability
│   ├── QS-10 Recipe create/edit is atomic, all-or-nothing (Stories 001, 010 — Implemented for 001)
│   ├── QS-11 Servings-scaling math is exact for integers, decimals, fractions, ranges (Story 007 — Implemented)
│   ├── QS-12 Ingredient/pantry matching is deterministic and case/whitespace-insensitive (Story 014 — Specified)
│   └── QS-13 Pantry data and cook-session state survive app restarts (Stories 012, 019 — Specified)
└── Resilience & Graceful Degradation
    ├── QS-14 A failed/optional photo upload never blocks a recipe save (Story 001 — Implemented)
    ├── QS-15 A missing/corrupted photo shows a placeholder, never an error (Stories 006, 007 — Implemented)
    └── QS-16 Features relying on optional browser APIs degrade silently when unsupported (Story 020 — Specified)
```

## Quality Scenarios

<!-- Concrete, measurable quality scenarios -->

| ID | Quality Goal | Scenario | Expected Response | Priority | Source / Status |
| --- | --- | --- | --- | --- | --- |
| QS-01 | Performance | User submits the add-recipe form | Recipe is persisted and the user sees confirmation in < 500ms at p95 | High | Spec 001 NFR-001 — **Implemented** (`RecipeService.CreateAsync`) |
| QS-02 | Performance | User opens a recipe's detail page / drags the servings slider | Detail page loads in < 500ms; scaled quantities recalculate in < 100ms | High | Spec 007 NFR-001/002 — **Implemented** |
| QS-03 | Performance | User opens the recipe grid with 100+ recipes in the collection | Grid loads and renders in < 2s, with photos lazy-loaded | Medium | Spec 006 NFR-001 — **Implemented** |
| QS-04 | Performance | User opens "recipes by available ingredients" with 100 recipes and 50 pantry items | Ranked results render in < 500ms | Medium | Spec 014 NFR-001 — **Specified** (not built) |
| QS-05 | Performance | User swipes/taps next-step in cook mode | Step transition responds in < 100ms | High | Spec 019 NFR-002 — **Specified** (not built) |
| QS-06 | Performance | User types into the search box | Visible results update in < 200ms per keystroke, input debounced | Medium | Spec 009 NFR-001/002 — **Specified** (not built) |
| QS-07 | Usability/Accessibility | User navigates the add-recipe form or browse grid using only a keyboard | All fields, buttons, and recipe cards are reachable and operable via Tab/Enter | High | Specs 001 NFR-003, 006 NFR-003, 007 NFR-003 — **Implemented** (standard HTML form/anchor semantics; no dedicated a11y test suite yet) |
| QS-08 | Usability/Accessibility | User is cooking with wet/messy hands, phone propped on the counter | Cook-mode buttons are large and easily tapped; step text is 18pt+ with high contrast, readable from across the kitchen | High | Spec 019 NFR-003/004 — **Specified** (not built) |
| QS-09 | Usability/Responsiveness | User opens the app on desktop, tablet, and mobile | Grid and detail layouts reflow to 3-4 / 2-3 / 1-2 columns respectively and remain readable | High | Specs 006 NFR-002, 007 NFR-004 — **Implemented** |
| QS-10 | Data Integrity | Save or edit operation fails partway through (e.g., network drop) | No partial recipe is persisted; the operation is all-or-nothing | High | Specs 001 NFR-002, 010 NFR-003 — **Implemented** for create (`RecipeService.CreateAsync` single `SaveChangesAsync` call wraps recipe + ingredients + steps); edit (Story 010) not yet built |
| QS-11 | Data Integrity | User scales a recipe with fractional/decimal/range ingredient quantities | Scaled output is mathematically correct for integers, decimals, fractions, and ranges | High | Spec 007 NFR-005 — **Implemented** |
| QS-12 | Data Integrity | Two users (or the same user twice) run the ingredient-availability match against the same pantry/recipes | Results are identical every time; matching is case-insensitive and whitespace-trimmed | Medium | Spec 014 NFR-004, EC-001/002 — **Specified** (not built) |
| QS-13 | Reliability | App restarts (deploy, crash, reboot) | Pantry items and in-progress cook-session step position are unaffected | Medium | Specs 012 NFR-004, 019 NFR-006 — **Specified** (not built) |
| QS-14 | Resilience | Photo upload fails or is omitted during recipe save | The recipe itself still saves successfully; only a non-blocking warning is surfaced | High | Spec 001 NFR-005, FR-013 — **Implemented** (`RecipeService.CreateAsync` catches photo failure and still calls `SaveChangesAsync`) |
| QS-15 | Resilience | A recipe's photo file is missing or corrupted | A placeholder image displays instead of an error, on both grid and detail views | Medium | Specs 006 EC-001, 007 EC-010 — **Implemented** (nullable `Photo`/`PhotoId` in domain + summary query) |
| QS-16 | Compatibility | Browser/device does not support the Screen Wake Lock API | Cook mode continues to function normally with no error or crash; it simply has no wake lock | Low | Spec 020 NFR-003 — **Specified** (not built) |

## Validation Notes

- Scenarios marked **Implemented** are currently exercised, at minimum, by the form
  validation and persistence logic in `src/RecipeManagementWebApp/Services/RecipeValidator.cs`
  and `RecipeService.cs`, and covered by `src/RecipeManagementWebApp.Tests/RecipeValidatorTests.cs`
  and `RecipeServiceTests.cs`. None of these tests currently assert on the *timing*
  budgets (e.g., the < 500ms p95 save latency) — they verify correctness/atomicity, not
  performance. **TODO**: no load/performance test harness exists yet in the repo; the
  millisecond budgets in this table are spec-asserted targets, not measured results.
- Scenarios marked **Specified** describe stories not yet present in `src/` and should be
  treated as acceptance criteria for future implementation work, not as current system
  behavior.
- No automated accessibility audit (e.g., axe, Lighthouse CI) is wired into the repo —
  **TODO** if/when one is added, link it here as the verification method for the
  Usability/Accessibility scenarios.
