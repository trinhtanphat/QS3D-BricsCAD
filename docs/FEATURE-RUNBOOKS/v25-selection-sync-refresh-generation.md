# BricsCAD V25 SelectionSync refresh generation ownership

## Scope

Lane C03. `SelectionSyncCoordinator` may receive nested modeless/document callbacks while palette creation or native implied-selection capture is in progress. A detached attachment generation must never release the reentrancy ownership of a newer reattachment that happens to reuse the same native `Document` wrapper.

## Failure mode

The coordinator already assigns a unique attachment token on each attach. Before this carrier, however, the `Refreshing` guard was only a `HashSet<Document>`. Sequence A(T1) refresh -> detach -> A(T2) reattach/refresh -> T1 finally could execute `Refreshing.Remove(document)` and erase T2's marker. A third callback could then re-enter refresh for T2 while T2 was still executing.

## REMOTE_SAFE verification

Run:

```text
python scripts/preflight-v25-selection-sync-refresh-generation.py
```

The guard requires in-flight refresh ownership to map the exact `Document` to the exact captured attachment token and requires cleanup to remove the entry only when that same token still owns it. It rejects asynchronous/native transaction work inside release bookkeeping.

Also run aggregate feature source guards, deterministic smoke, and the V25 build against admitted locked reference generations. These are REMOTE_SAFE evidence only.

## LOCAL_ONLY licensed BricsCAD V25

In a licensed host, induce or instrument nested callbacks during `PaletteCoordinator.EnsureCreated` / implied-selection capture so the same `Document` wrapper is detached and reattached before the older refresh unwinds. Verify:

- T1 cannot clear T2's in-flight refresh marker;
- a third callback does not enter concurrently while T2 owns refresh;
- stale T1 snapshots/status are not published;
- after T2 completes, normal selection refresh resumes;
- detach/stop still release pending timers and attachment state.

Do not claim `LOCAL_PASS` without exercising this timing in a real licensed BricsCAD V25 runtime.
