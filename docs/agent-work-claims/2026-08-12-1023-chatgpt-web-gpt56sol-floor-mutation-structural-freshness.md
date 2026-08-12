# Work claim — Floor mutation structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-mutation-structural-freshness-20260812-1023`
- Registered: `2026-08-12T10:23:00+07:00`
- Baseline main SHA observed: `357a71171fca6fa60dcbd5f2b7341cd497126e9a`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FLOOR-MUTATION-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectFloorService.ResolveOwnedElements(...)` guards `ProjectState.ChangeVersion` while enumerating caller-owned mutation targets, but it resolves project element ownership before that enumeration. `ProjectState.Elements` is a publicly mutable `IList<ProjectElement>`, so a lazy target enumerable can directly remove or replace a yielded target without calling `project.Touch()`. `ChangeVersion` remains unchanged and the stale pre-enumeration snapshot can then be returned to `Assign`, `AssignBottomLevel`, `AssignTopLevel`, or `ClearVerticalLevels`, allowing mutation of an element that no longer belongs to the project.

The same structural gap applies to the target `FloorDefinition` used by `Assign`, `AssignBottomLevel`, and `AssignTopLevel`: `FindRequired(...)` resolves it before caller target enumeration, while `ProjectState.Floors` is also a publicly mutable list. A lazy target enumerable can remove/replace that Floor without `Touch()`, leaving the revision guard unchanged and letting the mutation continue using a detached Floor object.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs` — post-enumeration current ownership revalidation for unique target elements and the target Floor of Floor/Level assignment APIs only
- `tests/QS3D.Core.SmokeTests/FloorMutationInputFreshnessSmoke.cs` — focused regression in the existing registered smoke
- this claim file

## Intended contract

Preserve the existing ChangeVersion freshness guard. After caller target enumeration, re-resolve every unique target against current `project.Elements` by semantic id and object identity before returning from the shared resolver. For `Assign`, `AssignBottomLevel`, and `AssignTopLevel`, also re-resolve the previously selected Floor by semantic id and object identity before planning/mutation. Structural removal/replacement/duplicate identity must fail before Floor/Level planning, `project.Touch()`, relation/property mutation, or element dirty-state mutation.

## Excluded scope

No changes to Floor CRUD/activation/elevation semantics, Zone/Family/Grid freshness, global ProjectState collection tracking, CAD/UI/runtime, LOCAL-003 smoke API repair, Actions/build/release.

## Validation plan

Extend the existing Floor mutation input freshness smoke with lazy sequences that (1) yield a target then directly remove it from `project.Elements`, and (2) directly remove the selected Floor from `project.Floors`, both without calling `Touch()`. Require fail-closed behavior with unchanged ChangeVersion, unchanged FloorId/vertical-level properties and unchanged dirty state apart from the deliberate external list removal. Exercise ordinary Floor assignment plus a vertical-level path to pin the shared resolver and target-Floor guard.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.
