# Selection-sync status document affinity

Issue: #6041  
Lane-Key: `issue-6041`  
Runtime: REMOTE_SAFE source/static/V25 compile. Licensed BricsCAD MDI/modeless exercise remains LOCAL_ONLY/NO_RESULT unless actually executed.

## Defect

`SelectionSyncCoordinator.Refresh(Document)` already fences successful inspection publication to the exact active `Document`, but its exception path historically called process-wide `PaletteCoordinator.SetStatus(...)` without revalidating the refresh origin. An exception originating from DWG A could therefore overwrite the Workspace status after the host had switched to DWG B.

## Contract

- The exception path carries its exact originating `Document` into `SelectionSyncStatusPublisher`.
- The publisher returns for null or non-current source identity before any status publication.
- Same-document failures still publish the stable redacted selection-sync status.
- The publisher remains synchronous and presentation-only: no project creation/mutation, transaction, document lock, implied-selection mutation, command dispatch, or deferred dispatcher work.
- Existing successful selection/inspection publication and #5945 synchronous document lifecycle invalidation remain unchanged.

## Deterministic validation

Run:

```text
python scripts/preflight-selection-sync-status-document-affinity.py
python scripts/preflight-all.py
powershell -ExecutionPolicy Bypass -File scripts/build-v25-with-stable-references.ps1
```

Self-review the A→B switch, destroyed/null source, A→B→A wrapper identity, palette disposal/recreation and reentrancy boundaries. Hosted/source validation must never be reported as licensed BricsCAD `LOCAL_PASS`.
