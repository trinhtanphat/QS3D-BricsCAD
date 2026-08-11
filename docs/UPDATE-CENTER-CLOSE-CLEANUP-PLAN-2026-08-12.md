# Update Center close-failure cleanup plan — 2026-08-12

## Goal

Make modeless Update Center event ownership deterministic even when WPF `Window.Close()` fails before `Closed` is raised.

## Source failure path

Current ownership is asymmetric:

- constructor subscribes `UpdateCoordinator.Instance.StateChanged += OnStateChanged`;
- only `Closed` unsubscribes;
- `UpdateCenterWindowHost.Close()` catches every `window.Close()` exception;
- regardless of success/failure, the host then clears `_window`.

Therefore a close exception can produce an unreachable-from-host window that is still strongly retained by the singleton coordinator event.

## Invariants

- Exactly one coordinator subscription per live Update Center window.
- Detachment must be idempotent.
- Successful user/WPF close still detaches normally.
- Host shutdown must detach even if `Window.Close()` throws.
- Existing modeless window reuse, update buttons, release navigation and graceful BricsCAD host-close semantics stay unchanged.
- No changes to `UpdateCoordinator`, release selection, signatures, manifests, launcher, installer or release workflow.

## Implementation

1. Add a small attachment-state field in `UpdateCenterWindow`.
2. Replace the anonymous Closed-only unsubscribe with an explicit `DetachCoordinator()` method.
3. Subscribe once in the constructor, mark attachment state, and have `Closed` call the idempotent detach method.
4. In `UpdateCenterWindowHost.Close()`, keep the existing non-throwing host contract but move `DetachCoordinator()` into `finally` so cleanup executes after either successful or failed `window.Close()`.
5. Keep `_window` reference clearing idempotent after cleanup.

## Regression gate

Add `scripts/preflight-update-center-close-cleanup.py` to require:

- explicit attachment state;
- constructor subscription and state transition;
- idempotent `DetachCoordinator()` that removes `StateChanged` and clears attachment state;
- `Closed` delegates to the explicit detach method;
- host `Close()` has `try` / `catch` / `finally` and invokes detach from `finally`;
- `_window` cleanup remains after close cleanup;
- old anonymous `Closed += (_, __) => UpdateCoordinator.Instance.StateChanged -= OnStateChanged;` pattern is forbidden.

## Verification

- Refresh `main` and exact source before mutation.
- Commit source fix, then focused gate.
- Re-fetch exact committed source and gate.
- Verify source/gate commits are ancestors of current `main` with `behind_by: 0`.
- Close claim with exact SHAs.
- Leave real WPF/BricsCAD exception-path timing in `LOCAL-009 / PENDING_LOCAL`.
- Do not dispatch Actions or publish a release.
