# Work claim — Family target operations null collection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-target-null-integrity-20260812-0910`
- Registered: `2026-08-12T09:10:00+07:00`
- Baseline main SHA: `55301299f8878eee87ef447aa110bb98cd01af73`
- Priority: P1 — Family target operations must fail closed when the project Family collection itself is structurally invalid.

## Reserved scope

Harden `ProjectFamilyService` target-based operations so any `null` entry in `project.Families` is rejected before resolving or using a target Family. The completed global-duplicate helper currently skips null entries, while `Create` already rejects them explicitly; this leaves target operations able to mutate or return results from malformed Family state.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Family Create null/duplicate lanes already completed.
- null entries in the `elements` argument to Family assignment, already completed under the older assign null-target lane.
- Family activation/UI state, FamilyWindow, audit/no-op behavior, templates, persistence/interchange, Floor/Zone services, native BricsCAD adapters, Actions/release.

## Validation plan

- Seed a valid target Family plus an unrelated `null` Family collection entry.
- Prove representative target mutation paths reject before `ProjectState.ChangeVersion`, Family/property/member state changes or collection removal/addition.
- Prove read-only `ReferenceCount` also rejects malformed Family state.
- Preserve valid target behavior and completed duplicate-ID enforcement.
- Inspect exact source/test diff and ancestry after refreshing moving `main`; do not claim GitHub Actions or licensed BricsCAD runtime PASS.

## Coordination

This lane builds on completed Family global-duplicate implementation `44779226f6fe49129cbc82c830b79232cc80426f` and regression `d5509b126a59b7753c459c4b7c0ee0f137ffed80`. The current Family activation global-duplicate reservation owns `FamilyWindow`/activation surfaces, not `ProjectFamilyService` CRUD/property/member operations. The HostLink global-element claim is independent.

## Completion condition

The common Family identity preflight fails closed on null collection entries for all target operations, focused deterministic Core smoke coverage is pushed, and this claim is marked `COMPLETED` without dispatching Actions.
