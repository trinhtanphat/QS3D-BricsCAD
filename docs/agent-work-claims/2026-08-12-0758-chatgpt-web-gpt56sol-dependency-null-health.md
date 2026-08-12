# Work claim — Dependency health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-dependency-null-health`
- Registered: `2026-08-12`
- Baseline main SHA: `b5a9b2bc879682fbf937ae3e3c95696efe9f0cc1`
- Priority: P1 — malformed semantic dependency graphs must not be silently normalized by diagnostics.
- Task Key: `CORE-DEPENDENCY-NULL-HEALTH`

## Confirmed defect

`DependencyHealthService.Inspect(ProjectState)` begins with `project.Elements.Where(x => x != null).ToList()`. A null semantic Element is therefore removed before identity counting and graph construction, allowing a malformed project to receive dependency diagnostics as though the corrupt entry did not exist. Existing canonical/missing/blank dependency lanes are completed and do not cover this malformed collection case.

## Reserved scope

- `src/QS3D.Core/Diagnostics/DependencyHealthService.cs`
- `tests/QS3D.Core.SmokeTests/DependencyHealthSmoke.cs`
- this claim file

Do not modify dependency mutation/regeneration, ProjectState contracts, other health providers, BricsCAD runtime, or reporting/quantity code.

## Intended contract

- Direct Dependency health rejects a null semantic Element with `InvalidOperationException` before graph classification.
- Valid acyclic/cyclic/self/missing/ambiguous dependency behavior remains unchanged.
- Comprehensive health keeps its existing `AddSafely` boundary and can surface provider failure without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Focused Core smoke pins malformed null rejection and existing valid paths remain in the same smoke suite; source/test are read back from merged `main`, then this claim is closed.
