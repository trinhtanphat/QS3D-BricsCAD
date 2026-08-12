# Work claim — Family assignment input freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-assignment-input-freshness-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Completed: `2026-08-12T10:07:00+07:00`
- Baseline main SHA observed: `8238196ef8ee5fb1096c58061a5992e27ed0d38b`
- Claim commit: `164787a6426e693ce3b406d588a37ac9e35b6a0b`
- Source fix commit: `cfa6f0ceb889e2f4003f4282339fdda038a504cb`
- Regression commit: `e2528461e31b40bf3f4b8704008f1bf79456a509`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FAMILY-ASSIGNMENT-INPUT-FRESHNESS`

## Confirmed defect

`ProjectFamilyService.Assign(...)` accepted caller-owned `IEnumerable<ProjectElement>` input. `ResolveOwnedElements(...)` first snapshotted project ownership and then enumerated the caller sequence, but unlike the equivalent Floor mutation guard it did not verify that `ProjectState.ChangeVersion` stayed stable while that external enumeration ran. A lazy enumerable could mutate the project during enumeration and let assignment continue against stale preflight state.

## Completed implementation

- `ProjectFamilyService.ResolveOwnedElements(...)` now captures `ProjectState.ChangeVersion` immediately before enumerating caller-supplied assignment targets.
- It rejects the batch if project version changes before enumeration completes.
- Existing project-owned identity, target-category, duplicate-target and deterministic ordering behavior remains unchanged.
- The guard runs before pending assignment construction and before FamilyId/property/dirty-flag mutation by the assignment itself.

## Regression evidence

`tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs` now includes `LazyAssignmentTargetsRejectStaleProjectInput()`. Its lazy target sequence deliberately calls `project.Touch()` during enumeration; the test requires `InvalidOperationException`, exactly one externally caused project revision, unchanged FamilyId/inherited property values, and unchanged element dirty/timestamp state.

The source and regression commits were fetched back from GitHub after write and their diffs matched the reserved scope. At regression commit `e2528461e31b40bf3f4b8704008f1bf79456a509`, `main` pointed at that exact commit.

## Excluded scope

No changes to BulkEdit global Family identity, Family activation, Family property propagation semantics, Floor/Zone mutation services, CAD/UI/runtime, Actions/build/release.

## Validation boundary

No GitHub Actions/full build/release dispatch occurred. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed. Validation in this lane is repository-level source/regression diff readback and static contract review only.
