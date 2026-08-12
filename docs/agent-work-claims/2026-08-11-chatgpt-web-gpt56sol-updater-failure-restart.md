# Agent Work Claim — restart BricsCAD after updater failure

- Claim ID: `UPDATER-FAILURE-RESTART-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T22:39:30+07:00`
- Released: `2026-08-11T22:43:00+07:00`
- Baseline main SHA: `807ad1a2405b6dac7b91a57e73fdf16d295e6acc`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The detached updater worker previously restarted the captured `bricscad.exe` only on the success path. After all BricsCAD processes were closed, any later signature/network/package/installer/restart failure entered `catch` and exited without restoring host availability.

## Completed changes

- `fbb7fae24e2fb2086715aba071aa8946e88e47ad` — registered this lane before source changes.
- `cb0446777272894c2eb26eee8b9f3272d1e6385f` — committed the post-failure restart plan before implementation.
- `f6fb76c3bacb3432abb4d4ea137609704631f2d0` — worker now tracks `$hostClosed`, sets it only after the cancellation-aware all-BricsCAD wait completes, preserves normal success restart, and performs one best-effort recovery restart only after a post-close failure when the captured BricsCAD executable still exists and no BricsCAD process is already running.
  - Pre-close readiness/cancellation failures cannot create a duplicate host because `$hostClosed` remains false.
  - Catch now captures `$updateFailure` and logs it with `Write-Error ... -ErrorAction Continue`; this avoids `$ErrorActionPreference='Stop'` turning the logging statement itself into a new terminating error before recovery/transcript cleanup.
  - Recovery restart failures are warning-only and do not mask the original worker failure; the worker still exits 1.
- `d6a9d21bf0da5dae32a3debfdb28dc4dc196364d` — added the auto-discovered `preflight-update-worker-restart.py` gate locking host-close/restart ordering, duplicate-instance avoidance, original-error preservation and no-host-kill policy.

## Preserved contracts

- Windows-SID cross-process update mutex.
- Readiness + cancellation handoff and cancellation barriers.
- Graceful `CloseMainWindow()` host-close request; no BricsCAD kill.
- WinVerifyTrust/current signer anchor and installed updater Authenticode signer pinning.
- GitHub package-host allowlist, product-version updater checks and external transcript logs.
- Success path still restarts the exact captured `bricscad.exe`.

## Integration verification

- Compare from `d6a9d21bf0da5dae32a3debfdb28dc4dc196364d` to current `main` reported `behind_by: 0`; concurrent commits after the gate touched unrelated claim/Core/test files, not updater source.
- `preflight-all.py` discovers `scripts/preflight-*.py`, so the new restart gate is part of aggregate source validation.
- No GitHub Actions workflow was dispatched and no release was published.

## Validation boundary

Source/static control flow is hardened. Actual Windows restart behavior after package/network/transaction failures remains `LOCAL-009 / PENDING_LOCAL`; this lane does not claim native/runtime PASS.