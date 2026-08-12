# Work claim — Family assignment input freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-assignment-input-freshness-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Baseline main SHA observed: `8238196ef8ee5fb1096c58061a5992e27ed0d38b`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FAMILY-ASSIGNMENT-INPUT-FRESHNESS`

## Confirmed defect

`ProjectFamilyService.Assign(...)` accepts caller-owned `IEnumerable<ProjectElement>` input. `ResolveOwnedElements(...)` first snapshots project ownership and then enumerates the caller sequence, but unlike the equivalent Floor mutation guard it does not verify that `ProjectState.ChangeVersion` stayed stable while that external enumeration ran. A lazy enumerable can mutate the project during enumeration and let assignment continue against stale preflight state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — Family assignment target-enumeration freshness guard only
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs` — focused regression in the already-registered smoke
- this claim file

## Intended contract

- Capture `ProjectState.ChangeVersion` immediately before enumerating caller-supplied Family assignment targets.
- Reject if the project changes before target enumeration completes.
- Preserve existing project-owned identity/category/duplicate-target validation and deterministic target ordering.
- Failure must occur before FamilyId/property/dirty-flag/project mutation by the assignment itself.

## Excluded scope

No changes to BulkEdit global Family identity, Family activation, Family property propagation semantics, Floor/Zone mutation services, CAD/UI/runtime, Actions/build/release.

## Validation plan

Extend the existing registered Family assignment atomicity smoke with a lazy target enumerable that calls `project.Touch()` during enumeration and assert fail-closed behavior before assignment mutation. Re-fetch moving `main` and exact source/test blobs before each write.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.
