# Work claim — Floor referenced-level structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-referenced-level-structural-freshness`
- Registered: `2026-08-12T13:38:00+07:00`
- Baseline main SHA: `c08f04828c06113c3a20e6b15813c6337c6a9b33`
- Priority: P2 — reject same-ID replacement/removal of opposite vertical levels during lazy Floor mutation target enumeration.

## Confirmed defect

`ProjectFloorService.AssignBottomLevel(...)` and `AssignTopLevel(...)` already protect `ProjectState.ChangeVersion`, selected `ProjectElement` instances, and the target `FloorDefinition` selected before caller-controlled target enumeration. They do not preserve/recheck structural ownership of the *opposite* Floor/Level references already stored on those selected elements.

`ProjectState.Floors` is a publicly mutable list and direct remove/replace does not necessarily advance `ChangeVersion`. A lazy target enumerable can replace a selected element's existing `TopLevelId` (for bottom assignment) or `BottomLevelId` (for top assignment) with a new `FloorDefinition` instance carrying the same ID but a different elevation. Existing freshness checks pass because the target Floor and selected element instances remain unchanged. Elevation validation then reads the replacement Floor and can silently allow an assignment that the pre-enumeration project state would reject.

Concrete counterexample: bottom target `B` is at elevation 0, selected element references top level `T` at elevation -1, so assigning `B` must fail. During lazy target enumeration, replace `T` with a same-ID Floor at elevation 3 without calling `Touch()`. The existing code then accepts the assignment using the replacement elevation.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs`, limited to referenced opposite-level structural freshness in `AssignBottomLevel(...)` and `AssignTopLevel(...)`
- focused Core smoke regression + ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-floor-referenced-level-structural-freshness.md`
- this claim file

## Intended contract

- Snapshot unique Floor ID -> exact `FloorDefinition` instance ownership before caller-controlled target enumeration in vertical-level assignment APIs.
- Preserve existing `ChangeVersion`, selected-element, and target-Floor freshness checks.
- Before reading existing opposite-level elevations, reject removal/same-ID replacement of any opposite-level Floor referenced by selected elements.
- Do not change ordinary Floor assignment, CRUD, activation, or same-instance Floor property semantics.
- Stable lazy vertical-level assignment remains unchanged.

## Excluded scope

- Existing Floor target/selected-element structural freshness lane completed by `50138ab6b698fa827a5904e136e4177021662a95`.
- Same-instance Floor elevation mutation semantics, Floor CRUD, UI/CAD/runtime, and global ProjectState collection tracking.
- GitHub Actions or licensed BricsCAD runtime qualification.
