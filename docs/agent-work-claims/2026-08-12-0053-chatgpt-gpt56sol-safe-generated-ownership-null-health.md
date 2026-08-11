# Work claim — Safe generated ownership malformed-project visibility

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-safe-generated-ownership-null-health`
- Registered: `2026-08-12T00:53:00+07:00`
- Last Updated: `2026-08-12T00:53:00+07:00`
- Baseline main SHA: `57b032b615de4d8a92c1ffbe3380dd66457269ea`
- Priority: P1 — health must not report false-clean for a malformed ProjectState that the canonical ownership index rejects.
- Task Key: `CORE-SAFE-GENERATED-OWNERSHIP-MALFORMED-PROJECT`

## Confirmed defect

`GeneratedHandleOwnershipIndex.Build(project)` already fails closed when `project.Elements` contains a null element, a blank semantic element id, or a duplicate canonical element id. `SafeGeneratedHandleOwnershipHealthService.Inspect(project)` independently scans the same collection, but currently executes `if (element == null) continue;`. A malformed project containing a null element can therefore produce no ownership issue from the safe health surface even though the canonical raw ownership index rejects that project.

That is a false-clean health result: the safe wrapper is intended to make ownership diagnostics consumable, not silently weaken the underlying project-integrity contract.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs`
- one focused auto-registered Core smoke for malformed-project visibility plus a valid-project non-regression
- this claim file

## Intended contract

- Keep `GeneratedHandleOwnershipIndex.Build(project)` fail-closed behavior unchanged.
- `SafeGeneratedHandleOwnershipHealthService.Inspect(project)` must convert canonical ownership-index validation failure into a visible `HealthSeverity.Error` issue instead of silently skipping malformed elements.
- Preserve existing conflict detection for valid projects, including same-logical-slot de-duplication.
- Do not mutate `ProjectState` during inspection.
- Do not touch DependencyHealth, generated native builders, BricsCAD adapter/runtime code, or currently ACTIVE claim surfaces.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim.

## Coordination

The historical generated-ownership hardening already made the raw index reject null/blank/duplicate semantic element identities. This lane is a focused follow-up only for the safe health wrapper’s false-clean behavior; it does not reopen or weaken the completed raw-index lane.

## Validation plan

- A project with one null `Elements` entry yields at least one Error issue from safe ownership health rather than an empty result.
- A duplicate/invalid semantic identity rejected by the raw index is likewise visible through safe health without mutating the project.
- A valid project with no ownership conflicts remains clean.
- Existing valid ownership-conflict reporting remains unchanged.
- Re-fetch current source after claim publication, patch from a fresh branch, inspect exact PR diff/changed-file set, and read back merged `main` source.

## Completion condition

Safe generated-handle ownership diagnostics can no longer turn canonical malformed-project rejection into a false-clean result, focused deterministic regression source is on `main`, and this claim is closed with exact merge evidence.