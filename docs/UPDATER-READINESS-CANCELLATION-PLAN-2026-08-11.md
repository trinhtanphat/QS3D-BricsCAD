# Updater readiness timeout cancellation plan — 2026-08-11

## Goal

Make the detached updater readiness timeout fail closed even when best-effort child termination fails. A UI-visible scheduling failure must never later turn into a hidden update after the parent BricsCAD process releases or abandons the cross-process mutex.

## Current failure window

`SecureUpdateLauncher.TrySchedule()` waits up to five seconds for the detached PowerShell worker to signal readiness. On timeout it currently attempts `updater.Kill()` best-effort, throws, then releases the parent mutex and resets `_scheduled`. If the worker survives the kill attempt, it can still acquire the mutex later and reach `update-v25.ps1` despite the scheduling failure already returned to the UI.

## Design

1. Keep the existing per-Windows-user named mutex as the cross-process single-flight boundary.
2. Add a second unique named `EventWaitHandle` for cancellation alongside the existing readiness event.
3. Pass both event names to the generated PowerShell worker.
4. Worker opens the mutex handle, readiness event and cancellation event before signaling readiness.
5. Worker checks cancellation before blocking on mutex ownership.
6. After mutex ownership/abandoned-mutex recovery, worker checks cancellation again before waiting for all BricsCAD processes.
7. Worker checks cancellation one final time immediately before invoking `update-v25.ps1`.
8. On parent readiness timeout, signal cancellation first, then perform the existing best-effort detached-worker termination. The outer cleanup may then release the parent mutex safely because a surviving child is cancellation-gated before installer execution.
9. Dispose all parent/worker event handles through deterministic `using` / `finally` cleanup.
10. Do not terminate BricsCAD. `CloseMainWindow()` remains the only host-close request.

## Regression gates

- Extend `preflight-update-worker-readiness.py` to require cancellation event creation/handoff and signal-before-timeout-failure ordering.
- Add `preflight-update-worker-cancellation.py` to require worker cancellation checks before mutex wait, after mutex acquisition and directly before the updater invocation.
- Preserve `preflight-update-cross-process-singleflight.py` restrictions that only the detached `updater` child may be killed.

## Validation boundary

Source/static verification can prove control-flow ordering and absence of host-kill regressions. Actual behavior under a failed Windows child termination, multiple BricsCAD instances and real signed update remains part of `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS will be claimed.
