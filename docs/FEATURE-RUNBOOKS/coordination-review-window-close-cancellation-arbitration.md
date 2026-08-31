# Coordination review window-close cancellation arbitration

## Purpose

Qualify issue #4695: the Coordination Manager must preserve its live transient review presentation when another WPF `Closing` subscriber has already cancelled the close attempt.

This runbook does not claim licensed BricsCAD runtime evidence. Hosted/source checks prove only source structure and deterministic admission semantics; the native/modeless observations below remain `LOCAL_ONLY` until run in a matching licensed BricsCAD host against the exact candidate SHA.

## Source contract

`CoordinationManagerReviewUi.Controller.OnWindowClosing` must:

1. remain inert when the controller is not live/attached;
2. treat an incoming `CancelEventArgs.Cancel == true` as authoritative and return immediately;
3. on that pre-cancel path, perform no transient cleanup, cleanup-barrier mutation, status mutation, action-state mutation, or native CAD presentation mutation;
4. never write `e.Cancel = false`;
5. when the close was not already cancelled, retain #4668 semantics: attempt transient cleanup before terminal close; if cleanup fails or ownership remains, set `e.Cancel = true`, preserve the cleanup barrier, and keep retry controls available;
6. preserve `Closed -> Dispose` and destroyed-document explicit-abandon boundaries.

## Deterministic repository checks

Run from repository root:

```powershell
python scripts/preflight-coordination-review-window-close-cleanup-ownership.py
python scripts/preflight-coordination-review-window-close-cancellation-arbitration.py
```

Both must PASS on the exact candidate SHA. Shared CI remains authoritative for the complete source-guard/build matrix selected by changed paths.

## LOCAL_ONLY BricsCAD matrix

Use a clean matching BricsCAD V25 host and the exact candidate package/source SHA.

| Case | Setup | Action | Required observation |
|---|---|---|---|
| Pre-cancel + Highlight | Highlight a valid issue; install an earlier `Closing` subscriber that sets `e.Cancel = true` | Attempt to close manager | Window stays open; highlight remains unchanged; no cleanup status/barrier mutation occurs |
| Pre-cancel + Isolation | Isolate a valid issue; earlier subscriber vetoes close | Attempt close | Window stays open; isolation remains owned/active; no `UNISOLATEOBJECTS` cleanup is triggered by this handler |
| Pre-cancel + Section/Focus | Apply section/focus; earlier subscriber vetoes close | Attempt close | Window stays open; view remains unchanged by close attempt |
| Ordinary cleanup failure | No earlier veto; arrange a supported fixture where transient cleanup cannot complete | Attempt close | Coordination handler sets cancellation, window remains open, cleanup controls remain enabled for retry |
| Retry then close | Resolve the cleanup failure and retry cleanup | Close again | Cleanup completes, barrier clears, close can proceed and terminal `Closed -> Dispose` runs |
| Successful no-debt close | No transient state, no earlier veto | Close | No artificial cancellation; window closes normally |
| Destroyed document | Trigger document-destroy boundary while manager is live | Observe close path | State is explicitly abandoned before close; no native cleanup is attempted against destroyed host |

Record exact source SHA, BricsCAD version, host/package identity, and sanitized PASS/FAIL evidence. Do not promote source/static checks to `LOCAL_PASS`.
