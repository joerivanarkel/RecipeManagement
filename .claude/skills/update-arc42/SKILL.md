---
name: update-arc42
description: >
  Use this skill when the user asks to update, refresh, or sync the arc42
  architecture documentation in docs/architecture/ so it reflects the current
  state of the codebase. Triggers on phrases like "update the architecture
  docs", "sync arc42", "refresh docs/architecture", "bring the arc42 docs up
  to date".
allowed-tools: Read, Glob, Grep, Write, Edit, Agent, Bash(git log*), Bash(git diff*)
---

# Update arc42 Architecture Documentation

You are refreshing the project's arc42 documentation (`docs/architecture/`) so
it accurately describes the codebase as it exists today — not as it was when
the docs were last written. The docs and the code will have drifted; your job
is to reconcile them, grounded in evidence from the actual repository, not
assumption or invention.

## Overview of the Approach

1. Gather shared context once (repo survey + arc42 file list).
2. Spawn one subagent **per arc42 file**, all launched in parallel in a single
   message, each updating exactly one file.
3. Run a short consistency pass afterward to catch cross-file contradictions
   (e.g., a building block renamed in one file but not the glossary).
4. Summarize what changed to the user.

Do not update the files yourself inline — the parallelization is the point.
Each file gets its own focused subagent so the work completes in one round
trip instead of twelve sequential edits.

## Step 1 — Shared Repo Survey

Before spawning any subagents, build a compact shared briefing that every
subagent will receive verbatim. Gather it yourself with Glob/Grep/Read
(don't delegate this part — it needs to happen once, not twelve times):

- The repository's top-level structure (main project folder(s), `.csproj`
  files, `Program.cs`, `appsettings*.json`, `Properties/launchSettings.json`).
- The folder structure under the main web app project (e.g.,
  `Components/Layout`, `Components/Pages`, any `Services/`, `Data/`,
  `Models/`, `Repositories/` folders — note which of these actually exist;
  don't assume a layer exists if there's no folder for it).
- Whether `docs/specs/*.md` exist. If they do, they are a goldmine: they
  already define domain entities, relationships, invariants, and
  non-functional requirements in the project's own vocabulary. Read their
  section headers (Domain Model, Non-Functional Requirements) so you can
  point subagents at them instead of having each one rediscover the domain
  model from scratch.
- Whether `docs/product/story-map.md` exists, for product context.
- Recent git history (`git log --oneline -30`) for a sense of what's
  actively changing versus stable.

Keep this briefing tight — a few paragraphs, not a dump of file contents.
Each subagent will do its own deeper reading of whatever's relevant to its
section.

## Step 2 — Enumerate arc42 Files

Run `Glob("docs/architecture/*.md")`. Use whatever files actually exist —
don't hardcode a list, since sections may have been added, removed, or
renumbered since this skill was written. The typical arc42 set is:

| File | Section | What changes should look for |
| --- | --- | --- |
| 00-table-of-contents.md | Table of Contents | Update only after other files change, in the final pass — not in the parallel round |
| 01-introduction-and-goals.md | Introduction & Goals | Project purpose, stakeholders, top quality goals — cross-check against what's actually built vs. aspirational |
| 02-architecture-constraints.md | Constraints | Actual framework/language versions from `.csproj`, platform constraints, team conventions evidenced in code |
| 03-system-scope-and-context.md | System Scope & Context | External systems/integrations actually wired up in `Program.cs`/config — not ones merely planned |
| 04-solution-strategy.md | Solution Strategy | Key technology and pattern choices evidenced in the code (e.g., Blazor Server render mode, chosen persistence approach) |
| 05-building-block-view.md | Building Block View | Actual folder/namespace structure and responsibilities — describe what exists, not an idealized layering |
| 06-runtime-view.md | Runtime View | Real interaction flows for the implemented features (e.g., how a request/render cycle actually flows through the code) |
| 07-deployment-view.md | Deployment View | Actual launch profiles, environments, hosting setup from `launchSettings.json` / deployment config, if any |
| 08-cross-cutting-concepts.md | Cross-Cutting Concepts | Error handling, validation, logging patterns as actually implemented (or flagged as not yet implemented) |
| 09-architecture-decisions.md | Architecture Decisions | Decisions evidenced by the code and git history — do not fabricate formal ADRs for decisions that were never actually made |
| 10-quality-requirements.md | Quality Requirements | Pull concrete, measurable NFRs from `docs/specs/*.md` where available; reference rather than duplicate feature-specific ones |
| 11-risks-and-technical-debt.md | Risks & Technical Debt | TODO/FIXME comments, missing test coverage, empty/placeholder sections, obvious gaps between spec and implementation |
| 12-glossary.md | Glossary | Domain terms from `docs/specs/*.md` and actual entity/class names in code (e.g., Recipe, Ingredient, Step, PantryItem, CookLogEntry) |

If the file set in the repo differs from this table, use the table as
*guidance for what a section is for*, not as a required checklist.

## Step 3 — Spawn Parallel Subagents

For every file **except** `00-table-of-contents.md`, launch one `Agent` tool
call with `subagent_type: general-purpose`, all in the **same message** so
they run concurrently. Do not batch them sequentially — the entire value of
this skill is that twelve files update at once instead of one at a time.

Each subagent's prompt must be self-contained (it has no memory of this
conversation) and should include:

1. **The shared repo briefing** from Step 1, verbatim.
2. **Which exact file** to update (full path) and what that arc42 section is
   for (from the table above, or the file's existing heading if it diverges).
3. **Explicit instruction to read the file first**, then investigate the
   parts of the codebase and `docs/specs/*.md` relevant to that section
   before changing anything.
4. **Explicit instruction to preserve arc42 structure**: keep the existing
   heading hierarchy and any HTML comment scaffolding already in the file;
   update content, don't restructure the document.
5. **Explicit instruction against fabrication**: if something can't be
   evidenced from the code, specs, or git history, either leave a clearly
   marked placeholder/TODO or note it as an open item — never invent
   architecture that doesn't exist to make the document look complete.
6. **A short return value**: ask the subagent to report back (as its final
   text) a 2-4 bullet summary of what it changed and why, so you can
   compile a changelog for the user without re-reading every file.

Use `run_in_background: true` (the default) so they all proceed concurrently;
you'll receive completion notifications as each finishes.

## Step 4 — Consistency Pass

Once all file-update subagents have completed:

1. Skim the actual diffs (or re-read the updated files) for cross-file
   contradictions — e.g., a component renamed in `05-building-block-view.md`
   that the glossary (`12-glossary.md`) still calls by its old name, or a
   quality goal in `01-introduction-and-goals.md` that
   `10-quality-requirements.md` no longer reflects.
2. If you find contradictions, fix them directly yourself (small, targeted
   edits) rather than re-spawning subagents for minor reconciliation.
3. Update `00-table-of-contents.md` last, only if section titles or
   structure actually changed — otherwise leave it alone.

## Step 5 — Summarize for the User

Report, grouped by file: what was updated and why (in one line per file),
and call out anything you deliberately left as an open question/placeholder
because the codebase didn't have enough evidence to document it confidently.
Do not claim completeness the evidence doesn't support — a documented gap is
more useful than a fabricated answer.
