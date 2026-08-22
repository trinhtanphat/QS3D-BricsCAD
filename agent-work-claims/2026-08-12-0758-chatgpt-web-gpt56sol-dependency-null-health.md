# Work claim — Dependency health null-element fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-dependency-null-health`
- Registered: `2026-08-12`
- Baseline main SHA: `b5a9b2bc879682fbf937ae3e3c95696efe9f0cc1`
- Priority: P1 — malformed semantic dependency graphs must not be silently normalized by diagnostics.
- Task Key: `CORE-DEPENDENCY-NULL-HEALTH`

## Confirmed defect

`DependencyHealthService.Inspect(ProjectState)` filtered `project.Elements` with `Where(x => x != null)`, removing null semantic Elements before identity counting and graph construction. A malformed project could therefore receive dependency diagnostics as though the corrupt entry did not exist.

## Reserved scope

- `src/QS3D.Core/Diagnostics/DependencyHealthService.cs`
- `tests/QS3D.Core.SmokeTests/DependencyHealthSmoke.cs`
- this claim file

## Completed contract

- Direct Dependency health now rejects a null semantic Element with `InvalidOperationException` before graph classification.
- Existing acyclic/cyclic/self/missing/ambiguous dependency behavior remains in the same smoke suite unchanged.
- Smoke coverage also pins `ComprehensiveModelHealthService` surfacing the provider failure through Error-level `HEALTH_PROVIDER_FAILED` for `DependencyHealthService`.
- Inspection remains read-only.

## Commits

- Claim: `fe78978b3184edc523e62e90195845b75b9a5de5`
- Source fix: `950b39e08767bc29f7c449ee212b3f8506390f32`
- Smoke regression: `f0074f030d0a320147969fd7ac51c03ee2d79ebe`

## Verification

Readback from `main` after the smoke commit confirmed the fail-visible null guard and the new direct/aggregate smoke assertions remain present. The executable Core smoke suite was not run by this GitHub connector session, so no executable test/build PASS is claimed. No GitHub Actions/build/release was dispatched and no BricsCAD runtime PASS is claimed.
