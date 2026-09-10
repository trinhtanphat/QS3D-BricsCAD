# V25 Start Center document refresh containment

## Scope

Lane C03. This runbook covers `BltStartCenterWindow` modeless refresh behavior when the BricsCAD active document is closing or switching while a Dispatcher-coalesced Start Center refresh is pending.

The defect fixed by issue #6319 was a native `Document.Name` dereference before the Start Center established document-neutral title/floor/elevation state. If the captured native wrapper became unavailable during host teardown, the getter could throw and leave the previous drawing's UI visible.

## Required behavior

1. `RefreshHomeShell` establishes neutral title, floor and elevation before any active-document lookup/native path read.
2. The active `Document` is captured once for the refresh generation. `TryReadDocumentPath` reads `Name` only from that exact instance and contains native getter failure.
3. The helper never re-resolves `MdiActiveDocument`, mutates project/CAD state, queues Dispatcher work, starts transactions, or locks a document.
4. A failed path read leaves the already-published neutral title/floor/elevation intact and must not create/update a Recent Project entry.
5. Project/floor display lookup uses the same captured `Document` and remains read-only/best-effort.
6. Existing host lifecycle subscription, unsubscribe-on-close, refresh coalescing and reentrancy fences remain authoritative.

## REMOTE_SAFE verification

Run the deterministic source guard:

```text
python scripts/preflight-v25-startcenter-document-refresh-containment.py
```

Run the repository feature guard aggregate and deterministic smoke/gates required by Shared CI. Build `QS3D.BricsCAD.V25` only with the repository-admitted trusted BricsCAD V25 reference path. A remote admitted-reference compile is evidence of compile compatibility only; it is not a licensed BricsCAD runtime pass.

Expected source-guard result: PASS only when neutral state precedes native path reads, the exact-document helper contains stale-wrapper failure, and Recent Project recording is gated on a trustworthy path read.

## LOCAL_ONLY licensed BricsCAD V25 scenario

This scenario requires a real licensed BricsCAD V25 process and is not satisfied by remote CI:

1. Open DWG A and the QS3D Start Center.
2. Give A an identifiable QS3D active floor/elevation and ensure its title/floor/elevation are visible in Start Center.
3. Trigger rapid A -> B activation and, separately, close A while Start Center has a deferred refresh queued. Exercise both active-document destruction and background-document destruction.
4. Allow the Dispatcher queue to drain.
5. Verify Start Center never retains A's title/floor/elevation after A is no longer a trustworthy refresh source.
6. Verify a stale/failed A path read does not add or touch a Recent Project entry for another document.
7. Verify B may publish only from B's own later refresh generation; no fallback inside the stale A helper may substitute B.
8. Close Start Center and repeat document activation/destruction. Verify there are no duplicate handlers, post-close refreshes, or retained modeless-window callbacks.

Record this as `LOCAL_PASS` only when the steps above are actually executed in licensed BricsCAD V25. Otherwise report `LOCAL_ONLY / NO_RESULT`.

## Rollback / regression checks

If this change is reverted or modified, preserve these invariants: no exception detail leaks into user UI, no CAD/project mutation is introduced into Start Center refresh, transaction/document-lock ownership remains unchanged, recent-project persistence stays post-trust only, and host lifecycle unsubscribe/close behavior remains best-effort and non-reentrant.
