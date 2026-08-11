# Work claim — Previous Family default integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-previous-family-default-integrity`
- Registered: `2026-08-11T21:07:00+07:00`
- Baseline main SHA: `55357e57981a996b3b3bfa75f19f5969184d2af6`
- Priority: confirmed Core integrity defect — Family reassignment and Auto Room synchronization validate the target Family before mutation but still consume the previous Family's raw property map directly. In assignment this occurs after `ProjectState.Touch()`, so malformed previous-Family defaults can participate in a committed semantic mutation instead of failing closed under the same structural contract already enforced for target/source Family snapshots.

## Reserved scope

Harden only previous-Family default consumption at the two Core transfer boundaries already using `ProjectFamilyService.SnapshotProperties(...)` for target data. Resolve and validate every previous Family property snapshot completely before any project/element/room/metadata mutation, then consume the immutable validated snapshots during mutation planning/application.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs`
- this claim file for close-out

## Excluded scope

- No new Family property schema and no numeric parsing policy; preserve the existing canonical key/value structural contract only.
- No Family catalog CRUD redesign, BulkEdit behavior beyond transitively inheriting the hardened `ProjectFamilyService.Assign(...)`, or Family UI/Workspace changes.
- No `RoomBoundaryCommands.cs` / native Room Auto regeneration work.
- No QSDB/`ProjectSession` persistence/session surfaces owned by `chatgpt-web-gpt56sol-core-atomicity-20260811-1930`.
- No Create Similar/Direct Draw, Ribbon, Quantity/BQ, updater/release/signing, LOCAL inbox edits, GitHub Actions dispatch, or BricsCAD V25 runtime qualification.

## Validation plan

- Re-fetch latest `main`, exact four target blobs and active claims before implementation writes.
- `ProjectFamilyService.Assign(...)`: cache a validated property snapshot for each unique previous Family while building the complete pending batch, before `project.Touch()`. Mutation must consume only those cached snapshots.
- `AutoRoomLifecycle.SyncFamilyDefaults(...)`: when switching Families, validate the previous Family snapshot before any mutation planning and use it for inherited-default comparison and stale-default cleanup.
- Add focused regressions using a malformed previous Family and prove rejection leaves FamilyId/properties/metadata/dirty flags/timestamps/`ChangeVersion` unchanged.
- Preserve existing successful reassignment and AutoRoom instance-override semantics.
- Review final diff against current `main`; no unexecuted smoke/CI/native runtime result will be reported as PASS.

## Coordination

The active Core mutation-atomicity claim is currently focused on `QsdbProjectStore.cs` / `ProjectSession.cs` and explicitly excludes speculative unrelated mutation refactors. The blocked Create Similar claim reserves command/Ribbon/local-handoff surfaces, not these Core Domain services. The Room Auto command-side claim does not own Core Family-default transfer. PR #457 is Workspace presentation-only and disjoint. If a newer claim reserves any exact Domain/test surface above before implementation, stop and re-scope rather than compete.

## Completion condition

Both Family reassignment and Auto Room Family switching reject malformed previous-Family defaults before any canonical state mutation, regressions lock full state preservation on failure, current-main integration is verified, and this claim is marked `COMPLETED` with exact pushed SHAs and validation actually performed.
