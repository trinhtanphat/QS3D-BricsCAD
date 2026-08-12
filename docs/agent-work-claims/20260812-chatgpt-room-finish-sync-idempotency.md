# Work claim — Room Finish synchronization idempotency

- Status: `COMPLETED`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `daf5c6a6009e635b519ab5baf551fb0d05c40bbe`
- Priority: owner-requested whole-repository audit; Core mutation/revision integrity

## Verified defect

`RoomFinishSynchronizationService.SynchronizeCore(...)` rewrote the synchronized Room Finish state and always called `finish.MarkDirty(ElementDirtyFlags.All)` plus `project.Touch()`. A second synchronization against an already canonical, unchanged Room therefore advanced element/project timestamps and project revision and forced regeneration despite no semantic state change.

This conflicted with Core no-op mutation behavior used by APIs such as `ProjectElement.SetProperty`, `SetQuantity`, and `MarkClean`, which preserve mutation timestamps when state is unchanged.

## Delivered

- Claim registration: `6e56c1a9fdd9bd673ebf852393c39d1b6a854d30`
- Source fix: `56bf20302f4b4b9c1d4ed6103eedbaf95cff8af6`
- Focused regression: `585b4a186a6ae3e8bf09b04a85de74f243027098`
- Synchronization now tracks actual Room-derived field/property/metric/dependency changes and only marks dirty/touches the project when a semantic repair occurred.
- A canonical single Room dependency is preserved without remove/re-add churn; duplicate, non-canonical, or non-terminal Room dependencies are repaired to the existing canonical output contract.
- Missing Room metrics still remove stale finish metrics and count as a mutation.

## Reserved scope

- `src/QS3D.Core/Services/RoomFinishSynchronizationService.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishSynchronizationSmoke.cs`
- this claim file

## Verification

- Re-fetched committed source on observed main `15d2f198397748f399efc4f4478ff936d7c21464`; blob `52417942667b6a3c234b348a27a8c97390a7384d`.
- Re-fetched committed smoke on the same observed main; blob `79c647758120db145a1412eb7e36dead5d41ece3`.
- `compare_commits` confirms source commit `56bf2030...` is the merge base/ancestor of regression commit `585b4a18...` (`behind_by: 0`). Concurrent unrelated commits landed between them without touching the claimed source.
- Regression locks first-sync mutation, clean second-sync no-op (`ChangeVersion`, project/finish `UpdatedUtc`, and dirty state preserved), plus stale-metric/duplicate-dependency repair.
- Smoke code was committed but not executed in this connector-only environment; no CI/build/runtime PASS is claimed.
- No GitHub Actions dispatch, no force-push, no release publication, and no BricsCAD V25 runtime PASS claim.
