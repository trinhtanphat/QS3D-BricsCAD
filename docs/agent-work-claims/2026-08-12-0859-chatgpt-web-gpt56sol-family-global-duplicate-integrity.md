# Work claim — Family target operations global duplicate integrity

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-family-global-duplicate-integrity-20260812-0859`
- Registered: `2026-08-12T08:59:00+07:00`
- Released: `2026-08-12T09:06:00+07:00`
- Baseline main SHA: `8287081b4b92b597b8d83093e91dc50f821612c3`
- Priority: P1 — Family target operations must reject globally ambiguous Family identity state before mutation or result production.

## Reserved scope

Harden target-based `ProjectFamilyService` operations so a project containing any case-insensitive duplicate existing Family IDs fails closed before resolving/using a unique target Family. Reuse one internal uniqueness preflight for Create and target operations.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Family Create null/duplicate lanes already completed.
- Family UI audit/no-op behavior, active-Family UI state, Bulk Family, templates, persistence/interchange, Floor/Zone services, native BricsCAD adapters, Actions/release.
- No change to valid Family assignment/default inheritance/category semantics.

## Validation plan

- Seed an unrelated duplicate pair such as `F1`/`f1` plus a unique target `F2`.
- Prove representative target operations fail before project/family/element mutation; include read-only `ReferenceCount` fail-closed coverage.
- Preserve valid controls and existing null/member/property guards.
- Inspect final source/test diff and ancestry on refreshed `main`; do not claim GitHub Actions or licensed BricsCAD runtime PASS.

## Coordination / release reason

A concurrent agent had already registered the same semantic lane before this reservation became visible and completed it while this claim was being published. The winning implementation is `44779226f6fe49129cbc82c830b79232cc80426f`, regression is `d5509b126a59b7753c459c4b7c0ee0f137ffed80`, and its ownership close-out is `ae7f54b33b8f5e4de2f9072dc3f2b3dc94fd5302`.

No source, tests, scripts, runtime evidence or product behavior were changed under this claim. This reservation is therefore released rather than marked completed; the already-merged implementation remains authoritative.

## Completion condition

`RELEASED`: duplicate ownership removed without overwriting concurrent work. No GitHub Actions/build/release or licensed BricsCAD runtime qualification was performed.
