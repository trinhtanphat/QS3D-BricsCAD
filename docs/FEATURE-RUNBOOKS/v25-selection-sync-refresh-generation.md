# BricsCAD V25 SelectionSync attachment/refresh generation ownership

## Scope

Lane C03. `SelectionSyncCoordinator` may receive nested modeless/document callbacks while native event subscription, palette creation, implied-selection capture, or delayed `DispatcherTimer` work is in progress. A stale attachment generation must never unsubscribe, detach, schedule selection work, publish selection state, or release refresh ownership belonging to a newer reattachment that reuses the same native `Document` wrapper.

## Failure modes

Each attachment has a unique token. Before this carrier, in-flight refresh ownership was only a `HashSet<Document>`. Sequence A(T1) refresh -> detach -> A(T2) reattach/refresh -> T1 finally could execute `Refreshing.Remove(document)` and erase T2's marker, allowing a third refresh to re-enter while T2 was still executing.

A second ABA boundary existed in `Attach`: subscription/initial refresh happened before catch rollback, while rollback blindly unsubscribed and removed document state. If native/modeless work detached T1 and reattached T2 before T1 threw, T1's catch could tear down T2.

The native subscription itself is also treated as a reentrancy boundary. Each attachment now publishes a generation-specific `EventHandler` that captures the exact `Document` + attachment token. Detach/rollback unpublish state before native unsubscribe and remove only that exact handler. A delayed callback from T1 fails closed unless T1 remains the current attachment generation and source document is still the exact active MDI document.

Self-review found a third delayed-work boundary: the event callback originally passed only `Document` into `ScheduleRefresh`, so a timer created by T1 could fire after detach -> reattach and call `Refresh(document)`, which then acquired T2's current token. The scheduler and timer callback now retain the exact attachment token and revalidate it before invoking `Refresh(document, attachmentToken)`; `Refresh` no longer recaptures a newer generation for stale queued work.

## REMOTE_SAFE verification

Run:

```text
python scripts/preflight-v25-selection-sync-refresh-generation.py
```

The guard requires:

- attachment token and generation-specific handler ownership to be published before native subscription;
- callback authority to be fenced by the exact attachment token and active document;
- delayed timer ownership to retain and revalidate the same exact attachment token;
- refresh entry to consume the supplied generation rather than recapturing a replacement generation;
- in-flight refresh ownership to map the exact `Document` to the captured attachment token;
- stale attach rollback to preserve a newer token/handler generation;
- detach/rollback to unsubscribe only their exact handler;
- refresh cleanup to remove only its exact generation token;
- rollback/release helpers to remain synchronous bookkeeping without CAD transactions, command dispatch, or deferred work.

Also run aggregate feature source guards, deterministic smoke, and the V25 build against admitted locked reference generations. These are REMOTE_SAFE evidence only.

## LOCAL_ONLY licensed BricsCAD V25

In a licensed host, induce or instrument nested callbacks during event subscription, `PaletteCoordinator.EnsureCreated`, implied-selection capture, and delayed timer firing so the same `Document` wrapper is detached and reattached before the older attach/refresh unwinds. Verify:

- stale T1 rollback does not unsubscribe or detach T2;
- delayed T1 selection callbacks/timers do not schedule or publish refresh for T2;
- T1 refresh cleanup cannot clear T2's in-flight marker;
- a third callback does not enter concurrently while T2 owns refresh;
- stale T1 snapshots/status are not published;
- after T2 completes, normal selection refresh resumes;
- detach/stop still release pending timers and current attachment state.

Do not claim `LOCAL_PASS` without exercising these timings in a real licensed BricsCAD V25 runtime.
