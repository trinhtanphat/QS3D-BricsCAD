# Agent Work Claim — updater readiness timeout cancellation

- Claim ID: `UPDATER-READINESS-CANCELLATION-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T22:29:30+07:00`
- Updated: `2026-08-11T22:32:00+07:00`
- Baseline main SHA: `ac44f22ee74797b324988755c3873e63e3aad088`
- Parent lane: `UPDATER-WORKER-READINESS-HANDOFF-20260811` (`RELEASED`)

## Verified residual race

The readiness timeout path currently calls `TryTerminateUnreadyWorker(updater)` and then throws. `TryTerminateUnreadyWorker` is explicitly best-effort: if `Kill()`/`WaitForExit()` fails, it swallows the error. The outer catch then releases the parent-owned cross-process update mutex, resets `_scheduled`, and reports scheduling failure.

A detached worker that survives that best-effort termination can therefore remain alive. Once the parent reservation is released—or later abandoned when BricsCAD exits—the surviving worker can acquire the mutex and continue into update execution even though the UI already reported that scheduling failed. This violates the intended fail-closed readiness contract.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-auto-update.py` (narrow compatibility fix for the already-established detached-worker-only timeout kill)
- `scripts/preflight-update-worker-readiness.py`
- `scripts/preflight-update-cross-process-singleflight.py` only if a narrow compatibility update is required by the cancellation contract
- `scripts/preflight-update-worker-cancellation.py` (new)
- `docs/UPDATER-READINESS-CANCELLATION-PLAN-2026-08-11.md` (new)
- this claim file

## Scope-extension reason

Re-reading the aggregate updater gate after the cancellation source change exposed an older regression from the completed readiness lane: `scripts/preflight-auto-update.py` still rejects every `.Kill(` token, while the current source intentionally permits exactly one `updater.Kill()` for best-effort termination of the detached PowerShell worker before host close. The focused readiness/cross-process gates already restrict that kill to the updater child, but the aggregate gate would still fail. This claim now explicitly reserves the aggregate gate so it can be reconciled without weakening the BricsCAD no-kill invariant.

## Non-overlap / preservation

- Preserve current Windows-SID named mutex, child readiness handshake, lifecycle scheduling boundary, graceful BricsCAD close, WinVerifyTrust/current signer pinning, updater Authenticode check, manifest/package/product-version verification and external logs.
- Do not edit UpdateCoordinator, GitHubReleaseClient, UpdateManifestProbe, installer/update PowerShell, release workflow, Ribbon/UI or unrelated product lanes.
- No GitHub Actions dispatch and no release publication.

## Intended contract

1. Parent creates a unique cancellation event alongside the existing readiness event before worker launch and passes both names to the child.
2. On readiness timeout, parent signals cancellation before any best-effort child termination attempt.
3. Worker checks cancellation before waiting for mutex ownership, after mutex ownership is obtained, and immediately before invoking `update-v25.ps1`; cancellation must abort before installer execution.
4. If worker termination fails, cancellation remains the authoritative fail-closed path, so releasing/abandoning the parent mutex cannot convert a reported schedule failure into a later hidden update.
5. No BricsCAD process is killed. Any `Kill()` remains scoped only to the detached PowerShell worker in timeout cleanup.
6. Aggregate/focused preflights must permit exactly that detached-worker kill and reject every current-process/BricsCAD/general process-kill variant.

## Validation / release conditions

- Commit a planning MD before implementation.
- Add/extend auto-discovered static gates requiring cancel-signal-before-timeout-failure and worker cancellation checks around mutex acquisition/install.
- Reconcile `preflight-auto-update.py` so aggregate validation matches the already-established detached-worker-only timeout exception without weakening host safety.
- Re-fetch current source/gates and verify ancestry with `behind_by: 0`.
- Native timing/process-failure behavior remains `LOCAL-009`; no remote runtime PASS claim.
- Mark `RELEASED` only after source + regression gates are on `main`.