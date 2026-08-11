# Agent Work Claim

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-20260811-project-save-identity-preflight-atomicity`
- Started (UTC): `2026-08-11T16:55:00Z`
- Last Updated (UTC): `2026-08-11T16:55:00Z`
- Expected Completion: `same session after source-safe implementation and repository-verifiable qualification`
- Task Key: `PERSISTENCE-PROJECT-SAVE-IDENTITY-PREFLIGHT-ATOMICITY`
- Intended Work: Prevent fail-fast QS3D Save / Save As validation failures from mutating the in-memory project's drawing identity or ChangeVersion before any sidecar write is authorized.
- Scope: `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs`; one focused source preflight; this claim and planning documentation.
- Verified Defect: `Save()` currently calls `SyncDrawingIdentity(project, document)` before recovery-block checks, project-file lock acquisition, backing-store freshness/path-transition validation, and sidecar-baseline validation. `SyncDrawingIdentity` can change `DrawingPath`, `DrawingFingerprint`, matching `ProjectElement.DrawingFingerprint` values and call `project.Touch()`. Therefore a Save As rejected because the target sidecar already exists/stale, or another pre-write freshness/baseline guard fails, can leave RAM identity/revision changed even though no write was authorized.
- Implementation Contract: Keep Save As identity synchronization, but defer it until all fail-fast no-write guards that do not require the synchronized identity have passed and the project lock is held. Do not add whole-project rollback around store writes; disk publication may already have occurred on later I/O failures and a blind rollback would create RAM/disk divergence. Preserve recovery metadata semantics and same-lock freshness/conditional-commit guarantees.
- Out of Scope: Qsdb serialization/replacement internals; recovery backup selection; post-publication I/O rollback semantics; Reload/HasPendingChanges/TrySavePending behavior unless required by source contract; native BricsCAD V25 runtime claims; GitHub Actions dispatch.
- Coordination: Historical local V25 parity branch is 870 commits behind current main with zero unique commits, so it does not own this file. The previous `recovery-save-stamp` claim is `RELEASED` and concerned `ProjectPersistenceStamp`, not Save As identity/preflight ordering. No current matching claim was found before registration.
- Verification Plan: Record detailed plan before source edit; preserve current path-transition/backing-store guard and recovery ordering; add a focused preflight that isolates `Save()` and requires path/recovery/no-write guards before `SyncDrawingIdentity`, while requiring identity sync before Store SaveNew/Save/SavePreservingValidatedBackup and before MarkSaved; verify no broad rollback introduced; compare current-main concurrency before merge and re-fetch merge-SHA source after integration.
- Native V25 Disposition: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` where interactive Save As qualification is required; source-order behavior is remotely verifiable.
