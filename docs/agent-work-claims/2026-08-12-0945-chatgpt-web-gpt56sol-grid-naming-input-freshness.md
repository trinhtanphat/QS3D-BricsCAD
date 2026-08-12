# Work claim — Grid naming input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-naming-input-freshness`
- Registered: `2026-08-12T09:45:00+07:00`
- Baseline main SHA: `4647f0ec44e3af8f3e685e2c93a1430b7dd11dfd`
- Priority: P1 — fail-closed Core mutation freshness at a caller-controlled enumeration boundary.

## Confirmed defect

`GridNamingService.Renumber(ProjectState, IEnumerable<string>, GridNamingOptions?)` enumerates caller-controlled `orderedGridElementIds` before resolving targets and mutating Grid naming metadata, but it does not verify that the project stayed at the same `ChangeVersion` while enumeration ran. A lazy enumerable can mutate/touch the same `ProjectState` while yielding otherwise-valid Grid IDs; renumbering then continues and can write labels against stale assumptions.

## Reserved scope

- `src/QS3D.Core/Domain/GridNamingService.cs`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-grid-naming-input-freshness.md`
- this claim file

## Intended contract

- Capture `project.ChangeVersion` immediately before enumerating `orderedGridElementIds`.
- Immediately after enumeration, fail closed if the version changed.
- Perform freshness rejection before empty/duplicate/options validation, project element resolution, planning, and all renumber mutations.
- Preserve bounded enumeration, stable-input behavior, label rules, collision checks, ordering, and no-op semantics.

## Excluded scope

- Grid annotation health/owner canonicality.
- BricsCAD Grid command lifecycle/native annotation behavior.
- Grid naming health diagnostics and unrelated naming semantics.
- No GitHub Actions dispatch or BricsCAD V25 runtime qualification.
