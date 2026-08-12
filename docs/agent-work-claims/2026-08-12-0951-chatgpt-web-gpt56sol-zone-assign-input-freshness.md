# Work claim — Zone assignment input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-assign-input-freshness`
- Registered: `2026-08-12T09:51:00+07:00`
- Baseline main SHA: `edc009584887e74e8d54fb658f9a4f3482f715c2`
- Priority: P1 — fail-closed Core Zone mutation freshness at a caller-controlled enumeration boundary.

## Confirmed defect

`ProjectZoneService.Assign(ProjectState, string, IEnumerable<ProjectElement>)` resolves the project and then enumerates caller-controlled target elements without checking whether that enumeration changed the same `ProjectState`. A lazy target enumerable can call `project.Touch()` while yielding an otherwise-owned element; assignment then continues, calls `project.Touch()` again, and mutates `ZoneId` against a newer project state than the target-enumeration baseline.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-zone-assign-input-freshness.md`
- this claim file

## Intended contract

- Capture `project.ChangeVersion` immediately before enumerating `elements` in `Assign(...)`.
- Immediately after target enumeration, fail closed if the project version changed.
- Reject freshness drift before changed-target calculation, assignment no-op return, `project.Touch()`, or any `ZoneId`/dirty mutation.
- Preserve ownership checks, duplicate-target collapse, canonical no-op semantics, and normal assignment behavior.

## Coordination

The completed Floor/Zone UI audit lane explicitly excluded `ProjectFloorService` / `ProjectZoneService` domain assignment semantics. `ProjectFloorService` is intentionally excluded here because a current vertical-level canonicality lane may overlap it.

## Excluded scope

- `ProjectFloorService` and vertical level behavior.
- Zone UI audit wrappers and active-zone behavior.
- Zone create/update/delete semantics.
- GitHub Actions/build/release dispatch or licensed BricsCAD runtime qualification.
