# Project Save Identity Preflight Atomicity Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-2355-chatgpt-web-gpt56sol-project-save-identity-preflight-atomicity.md`
Status: `IMPLEMENTATION_PENDING`

## Goal

Keep in-memory `ProjectState` drawing identity and `ChangeVersion` unchanged when `ProjectContextCoordinator.Save()` / Save As is rejected by a fail-fast no-write guard.

## Verified defect

Current `Save()` calls `SyncDrawingIdentity(project, document)` immediately after resolving the canonical project. `SyncDrawingIdentity()` is a mutator:

- it may assign `ProjectState.DrawingPath`;
- it may assign `ProjectState.DrawingFingerprint`;
- legacy adoption can rewrite matching `ProjectElement.DrawingFingerprint` values;
- both adoption and path-only synchronization call `project.Touch()`.

Only afterward does `Save()` check recovery-block state, acquire the project lock, call `EnsureBackingStoreUnchanged(...)`, require a sidecar revision baseline, and decide whether the current DWG path is a path transition. `EnsureBackingStoreUnchanged(...)` can reject Save As because the destination already has a `.qsdb/.bak`, because the existing source backing store changed externally, or because the backing-store state cannot be read stably.

Therefore a rejected no-write Save can mutate RAM identity/revision even though no sidecar write was authorized. The existing catch only restores recovery metadata, not drawing identity or `ChangeVersion`.

## Verified dependency boundary

`EnsureBackingStoreUnchanged(document, project, true, ...)` does not require a synchronized `project.DrawingPath`. It uses:

- the cached `Document -> ProjectState` reference;
- the stored sidecar revision baseline;
- `baseline.MatchesCurrent()`;
- `GetProjectPath(document)` for the current destination;
- `baseline.IsForPath(currentPath)`;
- a target `ProjectSidecarRevisionStamp.Capture(currentPath)` to refuse an existing destination sidecar.

Thus drawing-identity synchronization can be deferred until those no-write checks have passed.

## Implementation

In `ProjectContextCoordinator.Save()` only:

1. Keep canonical project resolution first.
2. Compute `path` and keep the recovery-required overwrite block before any drawing-identity mutation.
3. Preserve capture/clear/restore handling of recovery metadata.
4. Acquire `ProjectFileLock` before freshness/transition validation exactly as today.
5. Run `EnsureBackingStoreUnchanged(...)` and require the cached sidecar baseline before identity synchronization.
6. Determine `pathTransition = !baseline.IsForPath(path)`.
7. **Only then call `SyncDrawingIdentity(project, document)`**, immediately before selecting `Store.SaveNew`, `Store.SavePreservingValidatedBackup`, or `Store.Save`.
8. Preserve the existing same-lock Store commit, sidecar revision capture and persistence-stamp `MarkSaved()` ordering.
9. Do not add whole-project rollback around Store calls. Once I/O begins, publication may have succeeded before a later readback/cleanup failure; blindly rolling RAM back could create a worse disk/RAM divergence.

This change guarantees atomicity only across deterministic pre-write rejection boundaries, which are the defect being fixed.

## Regression gate

Add an auto-discovered source preflight that isolates `Save()` and verifies:

- `GetProjectPath` and recovery-required block occur before `SyncDrawingIdentity`;
- `ProjectFileLock.Acquire`, `EnsureBackingStoreUnchanged`, sidecar baseline requirement and `pathTransition` calculation all occur before `SyncDrawingIdentity`;
- `SyncDrawingIdentity` occurs before every Store save dispatch and before `MarkSaved()`;
- recovery metadata is still captured, cleared before persistence, and restored in catch;
- `EnsureBackingStoreUnchanged` still checks destination sidecar existence on path transitions;
- no `ProjectStateSnapshot.Restore`, broad project rollback, or force-overwrite behavior is introduced.

## Qualification

- Re-fetch current `ProjectContextCoordinator.cs` immediately before source edit.
- Compare path history/current `main` for concurrent ownership; historical local V25 branch is currently 870 commits behind main with zero unique commits and does not own this file.
- Parse focused preflight and inspect ordering contracts.
- Merge through PR with expected head SHA and no force update.
- Re-fetch exact merge-SHA source/gate and compare post-merge main for overwrite.
- Record missing GitHub status/workflow records as absence, not CI PASS.

Interactive BricsCAD Save As qualification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue.
