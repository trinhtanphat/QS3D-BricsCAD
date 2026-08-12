# Work claim — Floor mutation target input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-mutation-input-freshness`
- Registered: `2026-08-12T09:57:00+07:00`
- Baseline main SHA: `57c21d477cd8e5b47b30a95cfbc07566a9b2ce9c`
- Priority: P1 — fail-closed Core Floor mutation freshness at caller-controlled enumeration boundaries.

## Confirmed defect

`ProjectFloorService.Assign(...)`, `AssignBottomLevel(...)`, `AssignTopLevel(...)`, and `ClearVerticalLevels(...)` all pass caller-controlled `IEnumerable<ProjectElement>` targets into the shared `ResolveOwnedElements(...)` helper. That helper snapshots project ownership, then enumerates caller code without checking whether the same `ProjectState` changed during enumeration. A lazy target sequence can call `project.Touch()` while yielding otherwise-owned targets; the calling mutation API then continues validation/no-op calculation and can mutate Floor/Level metadata against a newer project state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs`, limited to target-enumeration freshness in `ResolveOwnedElements(...)`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-floor-mutation-input-freshness.md`
- this claim file

## Intended contract

- Capture `project.ChangeVersion` immediately before enumerating caller-supplied `elements` in `ResolveOwnedElements(...)`.
- Preserve project-element snapshotting, null/ownership validation, duplicate-target collapse, and deterministic ordering.
- Immediately after enumeration, fail closed if the project version changed.
- Ensure all four callers reject freshness drift before their validation/no-op/mutation stages.
- Preserve stable-input behavior and existing Floor/Level semantics.

## Coordination

Recent vertical-level canonicality and floor-integrity claims were verified completed before registration. This lane does not change reference canonicality, Floor create/update/delete, active-floor behavior, or UI audit wrappers.

## Excluded scope

- Floor/Level reference canonicality and vertical placement calculations.
- Floor create/update/delete/activate semantics.
- Floor/Zone UI audit behavior.
- Persistence, Actions, build/release dispatch, or licensed BricsCAD runtime qualification.
