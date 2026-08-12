# Work claim — Floor mutation structural freshness

- Status: `COMPLETE`
- State: `COMPLETE`
- Agent: `chatgpt-web-gpt56sol-floor-mutation-structural-freshness-20260812-1023`
- Registered: `2026-08-12T10:23:00+07:00`
- Completed: `2026-08-12T10:26:00+07:00`
- Baseline main SHA observed: `357a71171fca6fa60dcbd5f2b7341cd497126e9a`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FLOOR-MUTATION-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectFloorService.ResolveOwnedElements(...)` guarded `ProjectState.ChangeVersion` while enumerating caller-owned mutation targets, but resolved project element ownership before that enumeration. Because `ProjectState.Elements` is a publicly mutable `IList<ProjectElement>`, a lazy target enumerable could directly remove or replace a yielded target without calling `project.Touch()`. `ChangeVersion` stayed unchanged and the stale pre-enumeration snapshot could be returned to `Assign`, `AssignBottomLevel`, `AssignTopLevel`, or `ClearVerticalLevels`, allowing mutation of an element that no longer belonged to the project.

The same structural gap applied to the target `FloorDefinition` used by `Assign`, `AssignBottomLevel`, and `AssignTopLevel`: `FindRequired(...)` resolved it before caller target enumeration, while `ProjectState.Floors` is also a publicly mutable list. A lazy target enumerable could remove/replace that Floor without `Touch()`, leaving the revision guard unchanged and letting the mutation continue using a detached Floor object.

## Implemented

- Product fix: `e172db3499883a470ea42e2135353d8984cacdce` (`fix(core): revalidate Floor mutation ownership`).
- Regression: `25f3bdf5b57548a55825b30a64a4ef872694912c` (`test(floor): guard structural mutation freshness`).
- `ResolveOwnedElements(...)` now rebuilds current project element identity after external enumeration and requires each unique target to still resolve to the same project-owned object before returning it.
- `Assign`, `AssignBottomLevel`, and `AssignTopLevel` now revalidate the previously resolved target Floor against current project ownership before planning/mutation.
- Existing ChangeVersion freshness behavior remains intact.

## Validation evidence

Readback of the product commit confirms only the intended ownership revalidation was added to the three target-Floor assignment paths and shared target resolver. Readback of the regression commit confirms direct no-`Touch()` removal of a yielded target element is exercised through the vertical Bottom Level path, and direct no-`Touch()` removal of the target Floor is exercised through ordinary Floor assignment. Both regressions assert unchanged ChangeVersion and no assignment-side relation/property/dirty/timestamp mutation beyond the deliberate external list removal.

At validation refresh, current `main` `18d29069348f0808b3b3a24ae7236c08d63c1a9b` was ahead of regression commit `25f3bdf5b57548a55825b30a64a4ef872694912c` by two commits with that regression as merge base; the intervening diff was claim-document work only, so the fix/regression remained on current-main ancestry.

No GitHub Actions/full build/release was dispatched from this lane, and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope

No changes to Floor CRUD/activation/elevation semantics, Zone/Family/Grid freshness, global ProjectState collection tracking, CAD/UI/runtime, LOCAL-003 smoke API repair, Actions/build/release.
