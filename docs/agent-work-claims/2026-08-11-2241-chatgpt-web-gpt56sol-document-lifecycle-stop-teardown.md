# Work claim — document lifecycle stop teardown resilience

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-document-lifecycle-stop-teardown`
- Registered: `2026-08-11T22:41:00+07:00`
- Baseline main SHA: `26f1015647da6a6ae8003564c94a147a58683ae0`
- Priority: P1 deterministic teardown ownership hardening found during owner-requested `continue all` audit.

## Confirmed defect

`DocumentLifecycleCoordinator.Stop()` removes four BricsCAD `DocumentManager` handlers sequentially without per-handler isolation, then detaches document persistence, stops selection sync and finally clears `_started`. If any earlier event removal throws, teardown exits immediately: later manager handlers remain subscribed, persistence/selection ownership is not fully released, and `_started` stays true.

The startup rollback already treats native event removal as best-effort; normal Stop should provide at least the same cleanup resilience.

## Reserved scope

- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs`
- `scripts/preflight-document-lifecycle-stop-teardown.py` (new)
- this claim file for close-out

## Intended contract

- Stop attempts all four manager unsubscriptions independently.
- Persistence cleanup and selection-sync cleanup still run even if one manager event removal fails.
- `_started` is deterministically cleared after teardown attempts so repeated termination does not duplicate cleanup work.
- Existing startup rollback, exact-Document destruction, save/close persistence and active-document rebind semantics remain unchanged.

## Excluded scope

- No SelectionSyncCoordinator internals, ProjectContext semantics, palette/modeless/Ribbon/updater/Core changes, installer/signing/release or LOCAL inbox edits.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

Re-fetch source immediately before writing, make Stop best-effort across each cleanup stage without touching Start/Document event behavior, add a focused auto-discovered static gate, inspect exact diff and verify ancestry on moving `main` without force-push.

## Completion condition

Normal document lifecycle teardown cannot be short-circuited by one failing manager unsubscription, focused regression source is merged, and native failure-injection remains local-only.
