# Agent Work Claim — restart BricsCAD after updater failure

- Claim ID: `UPDATER-FAILURE-RESTART-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T22:39:30+07:00`
- Baseline main SHA: `807ad1a2405b6dac7b91a57e73fdf16d295e6acc`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The detached updater worker restarts the captured `bricscad.exe` only on the success path. After the worker has acquired the update reservation and observed that all BricsCAD processes are closed, any later failure—installed updater signature rejection, network/package verification failure, transactional installer failure or even the success-path restart itself throwing—falls into `catch`, logs the error and exits 1 without restoring the host application.

One-click update can therefore leave the user with BricsCAD closed after a recoverable update failure. Pre-close manifest probing reduces this risk but cannot eliminate post-close TOCTOU/network/package/installer failures.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-update-worker-restart.py` (new)
- `scripts/preflight-update-worker-readiness.py` / `scripts/preflight-update-worker-cancellation.py` only if narrow compatibility is required
- `docs/UPDATER-FAILURE-RESTART-PLAN-2026-08-11.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve current readiness+cancellation handoff, Windows-SID cross-process mutex, all-BricsCAD wait, WinVerifyTrust/current signer pinning, installed updater signer verification, package host allowlist, product-version checks, graceful host close and external logs.
- Do not edit UpdateCoordinator/UI, GitHubReleaseClient, UpdateManifestProbe, installer/update PowerShell, release workflow or unrelated feature lanes.
- Never launch an extra BricsCAD instance for cancellation/readiness failures that occur while a host is still running.
- No GitHub Actions dispatch and no release publication.

## Intended contract

1. Worker tracks an explicit `hostClosed` state that becomes true only after the all-BricsCAD wait completes.
2. Success path restarts the exact captured `bricscad.exe` as today.
3. Catch path preserves the original failure, and if `hostClosed` is true and no BricsCAD process has already appeared, performs one best-effort recovery restart of the exact captured executable.
4. Cancellation/readiness failures before host closure must not trigger a restart or create a duplicate BricsCAD instance.
5. Failure recovery restart errors are logged but never mask the original updater failure/exit code.
6. No BricsCAD process is killed.

## Validation / release conditions

- Commit a planning MD before implementation.
- Add an auto-discovered gate proving hostClosed is set only after the CAD wait, success/failure restart ordering, duplicate-instance avoidance and preservation of original error exit.
- Re-fetch launcher/gate and verify ancestry with `behind_by: 0`.
- Native restart behavior remains part of `LOCAL-009`; no remote runtime PASS claim.
- Mark `RELEASED` only after source + regression gate are on `main`.