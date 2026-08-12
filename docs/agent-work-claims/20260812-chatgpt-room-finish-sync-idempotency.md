# Work claim — Room Finish synchronization idempotency

- Status: `ACTIVE`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `daf5c6a6009e635b519ab5baf551fb0d05c40bbe`
- Priority: owner-requested whole-repository audit; Core mutation/revision integrity

## Verified defect

`RoomFinishSynchronizationService.SynchronizeCore(...)` rewrites the synchronized Room Finish state and always calls `finish.MarkDirty(ElementDirtyFlags.All)` plus `project.Touch()`. A second synchronization against an already canonical, unchanged Room therefore advances element/project timestamps and project revision and forces regeneration despite no semantic state change.

This conflicts with Core no-op mutation behavior used by APIs such as `ProjectElement.SetProperty`, `SetQuantity`, and `MarkClean`, which preserve mutation timestamps when state is unchanged.

## Reserved scope

- `src/QS3D.Core/Services/RoomFinishSynchronizationService.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishSynchronizationSmoke.cs`
- this claim file

## Intended contract

1. Synchronization changes/touches/marks dirty only when Room-derived state actually differs or Room dependency canonicalization changes the finish.
2. Repeating synchronization against an already canonical finish is a semantic no-op: preserve project `ChangeVersion` / `UpdatedUtc` and finish `UpdatedUtc` / dirty state.
3. Removing a stale Room metric or repairing duplicate/non-canonical Room dependencies remains a real mutation.
4. Existing rollback, ownership, stale-AutoRoom, and finite-metric validation behavior remains unchanged.

## Validation

- Re-fetch exact source/test after this claim lands and before implementation.
- Extend focused smoke coverage for first-sync mutation, second-sync no-op, and dependency/metric repair.
- No GitHub Actions dispatch, no force-push, no release publication, and no BricsCAD V25 runtime PASS claim.
