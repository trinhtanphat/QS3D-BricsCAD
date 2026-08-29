# Coordination review window-close cleanup ownership

Lane-Key: `issue-4668`

## Product invariant

A Coordination Manager window must not reach terminal `Closed` while the review session still owns transient highlight, isolation, isolation-mode, or section-view cleanup debt. The inner session already keeps failed native cleanup ownership retryable; the controller must also preserve a live UI/event retry path until that debt is discharged.

## Source-safe acceptance

1. `Controller.Attach()` acquires a cancellable WPF `Closing` handler before the terminal `Closed` handler and tracks both independently in `Attachment`.
2. `OnWindowClosing` attempts `TryResetTransientStateBestEffort()` while the controller is still attached.
3. If the attempt reports an exception or `HasTransientState` remains true, the handler sets `e.Cancel = true`, raises the existing cleanup barrier, emits a fail-closed status, and refreshes action state so only owned cleanup actions remain available.
4. Successful cleanup does not force `e.Cancel = false`; another subscriber's cancellation remains authoritative.
5. `Closed` remains the terminal `Dispose()` boundary after cleanup has been admitted.
6. Teardown removes `Closing` and `Closed` with per-handler retry ownership; failed detach never clears its attachment bit.
7. `DocumentToBeDestroyed` remains the explicit-abandon boundary: `AbandonDestroyedDocumentState()` runs before requesting window close, so no native cleanup is attempted against a destroyed host.
8. Current full-pair provenance/relink validation, same-row action composition, cross-row cleanup barrier semantics, and per-resource retry ownership remain unchanged.

## Hosted validation

Run the auto-discovered preflight:

```text
python scripts/preflight-coordination-review-window-close-cleanup-ownership.py
```

Then run normal shared branch CI. Required source-safe evidence is exact-head `preflight` and applicable `core` success, including trusted V25 compile references and the V25 plugin build when selected by CI.

## LOCAL_ONLY runtime matrix

Licensed BricsCAD runtime evidence is not available from hosted CI. A local agent may additionally validate, against an explicitly authorized exact SHA:

- create Highlight/Isolate/Section review state, close the manager, and confirm successful cleanup permits closure;
- inject/reproduce a native cleanup failure, attempt close, and confirm the window remains open with mutation actions blocked and relevant cleanup controls available;
- retry cleanup, confirm the barrier clears only when all owned transient state is discharged, then close successfully;
- destroy the owning document and confirm explicit-abandon permits closure without a stale controller or host exception.

Do not promote hosted/source evidence to `LOCAL_PASS`.
