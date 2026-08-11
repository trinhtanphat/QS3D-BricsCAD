# Work claim — Update Center close-failure subscription cleanup

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:27:00+07:00`
- Baseline main SHA: `9fe9483ce571604caa52022ffb23a852efc369f7`
- Priority: owner-requested whole-repository audit; modeless updater UI lifecycle cleanup

## Verified defect

`UpdateCenterWindow` previously subscribed to singleton `UpdateCoordinator.Instance.StateChanged` and removed that handler only from its `Closed` event. `UpdateCenterWindowHost.Close()` intentionally catches `window.Close()` exceptions and then drops `_window` anyway. If `Window.Close()` throws before `Closed` is raised, the host could lose its reference while the singleton coordinator still strongly retained `OnStateChanged` and the orphaned window.

## Implemented contract

1. `UpdateCenterWindow` now tracks coordinator subscription ownership explicitly with `_coordinatorAttached`.
2. `DetachCoordinator()` is idempotent and owns the actual `StateChanged -= OnStateChanged` operation.
3. Normal WPF `Closed` delegates to `DetachCoordinator()`.
4. `UpdateCenterWindowHost.Close()` retains its non-throwing public behavior but runs `DetachCoordinator()` from `finally`, so cleanup occurs whether `window.Close()` succeeds or throws.
5. Host `_window` clearing remains idempotent and occurs after subscription cleanup.
6. No force-close behavior was added and no coordinator/network/signing/launcher/installer/release source was changed.

## Commits

- Claim registration: `dfcbabad2c84cc1decd9207ade393163b26f3eb6`
- Planning: `86e7272fb3d869bcb1db2b1b21c3a9d534cd3842`
- Source fix: `00522ce67118312d361890a458d603e5e6a03896`
- Focused regression gate: `0d26c546c2a91e2d17df5c4ccd55524edf651e18`

## Verification

- Re-fetched exact committed `UpdateCenterWindow.cs` and `scripts/preflight-update-center-close-cleanup.py` from current `main` after the gate commit.
- Against observed `main` `ca3aef380b3a841d5867b6528f8799b31b4b5d68`:
  - source fix compare: `behind_by: 0`;
  - focused gate compare: `behind_by: 0`.
- Commits added after the source fix did not modify `UpdateCenterWindow.cs`.
- The gate rejects a return to the old Closed-only anonymous unsubscription and requires detach from the host `finally` path.
- Verification is source/static only. Real WPF exception-path timing and BricsCAD V25 shutdown/reload qualification remain `LOCAL-009 / PENDING_LOCAL`.
- No GitHub Actions workflow was dispatched and no release was created/published in this batch.

## Outcome

A failed modeless `Window.Close()` can no longer leave the singleton updater coordinator as the strong event owner of an Update Center instance that the host has discarded. Claim released.
