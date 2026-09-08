# Selection-sync status document affinity

Issue: #6041  
Lane-Key: `issue-6041`  
Runtime: REMOTE_SAFE source/static/V25 compile. Licensed BricsCAD MDI/modeless exercise remains LOCAL_ONLY/NO_RESULT unless actually executed.

## Defects

`SelectionSyncCoordinator.Refresh(Document)` already fenced successful inspection publication to the exact active `Document`, but its exception path historically called process-wide `PaletteCoordinator.SetStatus(...)` without revalidating the refresh origin. An exception originating from DWG A could therefore overwrite the Workspace status after the host had switched to DWG B.

The debounce/lifecycle audit found three related detach races. A `DispatcherTimer.Tick` can already be queued when `Detach` stops/removes its timer; the queued closure historically called `Refresh(document)` without proving that it was still the canonical pending timer or that the document was still attached. Re-entrant native/modeless work inside `Refresh` could detach the document and then throw; the success path was revalidated, but the catch could still publish stale status while the detached document remained active. Finally, detach followed by reattach of the same BricsCAD `Document` wrapper is an ABA boundary: a simple `Attached.Contains(document)` recheck can become true again and let an older in-flight refresh publish after the new attachment has taken ownership.

## Contract

- The exception path carries its exact originating `Document` into `SelectionSyncStatusPublisher`.
- The publisher returns for null or non-current source identity before any status publication.
- Every successful attachment receives a fresh object-identity token. `Refresh` captures that token before native/modeless work and must still own the exact current token before inspection or error publication.
- `Detach` removes the attachment token synchronously; reattaching the same `Document` creates a distinct token, so an older in-flight refresh cannot regain authority through A→detached→A ABA.
- A queued debounce Tick may call `Refresh` only while its timer is still `Pending[document]` by reference and the document is still attached. Consuming a valid Tick removes that pending-timer authority before refresh, so an old queued closure cannot regain authority across detach/reattach ABA.
- The exception path verifies exact attachment-generation authority before delegating to the source-document status publisher.
- Same-document, same-attachment failures still publish the stable redacted selection-sync status.
- The publisher remains synchronous and presentation-only: no project creation/mutation, transaction, document lock, implied-selection mutation, command dispatch, or deferred dispatcher work.
- Existing successful selection/inspection publication and synchronous document lifecycle invalidation remain unchanged outside the new authority fences.

## Deterministic validation

Run:

```text
python scripts/preflight-selection-sync-status-document-affinity.py
python scripts/preflight-all.py
powershell -ExecutionPolicy Bypass -File scripts/build-v25-with-stable-references.ps1
```

Self-review the A→B switch, destroyed/null source, detach with already-queued Tick, detach→reattach of the same wrapper while a Refresh is in flight, re-entrant detach before success/exception publication, palette disposal/recreation and duplicate/re-entrant refresh boundaries. Hosted/source validation must never be reported as licensed BricsCAD `LOCAL_PASS`.
