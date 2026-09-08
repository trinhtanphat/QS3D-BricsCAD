# Selection-sync status document affinity

Issue: #6041  
Lane-Key: `issue-6041`  
Runtime: REMOTE_SAFE source/static/V25 compile. Licensed BricsCAD MDI/modeless exercise remains LOCAL_ONLY/NO_RESULT unless actually executed.

## Defects

`SelectionSyncCoordinator.Refresh(Document)` already fenced successful inspection publication to the exact active `Document`, but its exception path historically called process-wide `PaletteCoordinator.SetStatus(...)` without revalidating the refresh origin. An exception originating from DWG A could therefore overwrite the Workspace status after the host had switched to DWG B.

The debounce lifetime also had two detach races discovered during carrier self-review. A `DispatcherTimer.Tick` can already be queued when `Detach` stops/removes its timer; the queued closure historically called `Refresh(document)` without proving that it was still the canonical pending timer or that the document was still attached. A detach/reattach ABA could therefore let the old attachment's queued work run under the new attachment. Separately, re-entrant native/modeless work inside `Refresh` could detach the document and then throw; the success path was revalidated, but the catch could still publish stale status while the detached document remained active.

## Contract

- The exception path carries its exact originating `Document` into `SelectionSyncStatusPublisher`.
- The publisher returns for null or non-current source identity before any status publication.
- `Refresh` fails closed for detached documents before palette work and revalidates attachment after native snapshot capture.
- A queued debounce Tick may call `Refresh` only while its timer is still `Pending[document]` by reference and the document is still attached. Consuming a valid Tick removes that pending-timer authority before refresh, so an old queued closure cannot regain authority across detach/reattach ABA.
- The exception path verifies that the source document remains attached before delegating to the source-document status publisher.
- Same-document, still-attached failures still publish the stable redacted selection-sync status.
- The publisher remains synchronous and presentation-only: no project creation/mutation, transaction, document lock, implied-selection mutation, command dispatch, or deferred dispatcher work.
- Existing successful selection/inspection publication and synchronous document lifecycle invalidation remain unchanged.

## Deterministic validation

Run:

```text
python scripts/preflight-selection-sync-status-document-affinity.py
python scripts/preflight-all.py
powershell -ExecutionPolicy Bypass -File scripts/build-v25-with-stable-references.ps1
```

Self-review the A→B switch, destroyed/null source, detach with already-queued Tick, detach→reattach ABA, re-entrant detach before success/exception publication, palette disposal/recreation and duplicate/re-entrant refresh boundaries. Hosted/source validation must never be reported as licensed BricsCAD `LOCAL_PASS`.
