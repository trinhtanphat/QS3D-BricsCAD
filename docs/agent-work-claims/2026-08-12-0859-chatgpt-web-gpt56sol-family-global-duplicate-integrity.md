# Work claim — Family target operations global duplicate integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-global-duplicate-integrity-20260812-0859`
- Registered: `2026-08-12T08:59:00+07:00`
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

## Coordination

Recent Family Create integrity work is completed. Current Floor/Zone global-duplicate claims reserve their own service files only; this claim does not touch those lanes. Re-check moving `main` and claims before source/test writes.

## Completion condition

A focused source + regression batch is pushed on current `main`, read back, and this claim is marked `COMPLETED` with exact implementation SHA and validation actually performed.
