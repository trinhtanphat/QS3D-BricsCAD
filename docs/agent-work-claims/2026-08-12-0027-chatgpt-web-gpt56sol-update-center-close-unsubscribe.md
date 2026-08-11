# Work claim — Update Center close-failure subscription cleanup

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:27:00+07:00`
- Baseline main SHA: `9fe9483ce571604caa52022ffb23a852efc369f7`
- Priority: owner-requested whole-repository audit; modeless updater UI lifecycle cleanup

## Verified defect

`UpdateCenterWindow` subscribes to singleton `UpdateCoordinator.Instance.StateChanged` and removes that handler only from its `Closed` event. `UpdateCenterWindowHost.Close()` intentionally catches `window.Close()` exceptions and then drops `_window` anyway. If `Window.Close()` throws before `Closed` is raised, the host loses its only reference while the singleton coordinator still holds `OnStateChanged` strongly. A later updater lifecycle can therefore retain/call an orphaned window instance.

This is an exception-path lifecycle leak visible directly from source; it does not require changing updater network, signing, scheduling or installation behavior.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs`
- `scripts/preflight-update-center-close-cleanup.py` (new)
- `docs/UPDATE-CENTER-CLOSE-CLEANUP-PLAN-2026-08-12.md` (new)
- this claim file

## Intended contract

1. Coordinator event detachment is explicit and idempotent rather than depending solely on successful WPF `Closed` delivery.
2. Normal `Closed` still detaches the handler.
3. Host-driven `Close()` detaches in a `finally`-equivalent path even when `Window.Close()` throws.
4. `_window` host reference cleanup remains idempotent.
5. No force-close/kill behavior is added, and no updater security/scheduling/source outside the reserved UI file is changed.

## Validation / release conditions

- Commit a planning MD before implementation.
- Add a focused source regression requiring explicit idempotent detach and exception-safe host cleanup, while rejecting a return to Closed-only unsubscription.
- Re-fetch exact source/gate and require `behind_by: 0` ancestry before claim closure.
- Real WPF/BricsCAD close timing remains `LOCAL-009 / PENDING_LOCAL`; do not claim remote runtime PASS.
- Do not dispatch GitHub Actions and do not publish a release.
