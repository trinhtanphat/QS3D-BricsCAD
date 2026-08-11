# Agent Work Claim — updater readiness timeout cancellation

- Claim ID: `UPDATER-READINESS-CANCELLATION-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T22:29:30+07:00`
- Updated: `2026-08-11T22:32:00+07:00`
- Released: `2026-08-11T22:35:30+07:00`
- Baseline main SHA: `ac44f22ee74797b324988755c3873e63e3aad088`
- Parent lane: `UPDATER-WORKER-READINESS-HANDOFF-20260811` (`RELEASED`)

## Verified residual race

The readiness timeout path previously called `TryTerminateUnreadyWorker(updater)` and then threw. `TryTerminateUnreadyWorker` is intentionally best-effort, so a failed `Kill()`/`WaitForExit()` could leave the detached child alive while the outer catch released the parent update mutex and reported scheduling failure. That surviving child could later acquire the mutex and reach install despite the failed UI state.

## Completed changes

- `8941e87103af471100cf56fc74f19050c70af461` — registered this follow-up claim before substantive edits.
- `f24e8ce4e936365126887b7856a53f034de24175` — committed the readiness-cancellation implementation plan before code.
- `75fd7b633464899e04f091c4a032cd4ccdd5c76a` — added a per-handoff named cancellation event. Parent timeout signals cancellation before best-effort worker termination; worker opens the cancellation handle before signaling readiness and checks cancellation before mutex wait, after ownership, while waiting for BricsCAD, and immediately before invoking `update-v25.ps1`.
- `3a7899d1db3bb42dd83d00bb313f72ec890b73e2` — strengthened the readiness gate to require cancellation creation/handoff and cancel-signal-before-timeout-failure ordering.
- `3ee094a46d101d2aee1dd945ff17fc91954d1eab` — explicitly extended claim scope after discovering the aggregate updater gate still rejected the already-approved detached-worker-only `updater.Kill()` exception.
- `311df94c593134726122dead32bf98ee5a5bf1ef` — reconciled `preflight-auto-update.py` so it allows exactly `updater.Kill()` in the detached readiness-timeout helper while still rejecting `Stop-Process`, `taskkill`, current-process/BricsCAD kills and any extra `.Kill()` call.
- `b2b717a4f0a09f6b11d3b28ca05a46022899018d` — added the focused worker-cancellation gate covering parent signal ordering and all worker cancellation barriers before install.

## Resulting contract

1. A reported readiness timeout always signals cancellation before any best-effort child termination or parent reservation release.
2. A child that survives `Kill()` cannot convert that failed scheduling result into a later hidden install: it either cannot open the disposed cancellation event or observes the signaled event before installer execution.
3. The Windows-SID named mutex/readiness handshake, graceful `CloseMainWindow`, WinVerifyTrust signer anchor, installed updater signature pinning, package host allowlist, product-version checks and external logs remain intact.
4. No BricsCAD process is killed. The only permitted `.Kill()` remains the detached PowerShell updater child in readiness-timeout cleanup.
5. `preflight-all.py` auto-discovers the new `preflight-update-worker-cancellation.py` gate.

## Integration verification

- Current source/gates were re-fetched after the edits.
- Compare from `b2b717a4f0a09f6b11d3b28ca05a46022899018d` to current `main` reported `behind_by: 0`; intervening commits touched unrelated Core/UI/claim files, not the updater launcher or these gates.
- No GitHub Actions workflow was dispatched and no release was published.

## Validation boundary

Source/static ordering is hardened. Actual failed-child-termination timing, multiple BricsCAD processes and signed update execution remain `LOCAL-009 / PENDING_LOCAL`; this lane does not claim a remote native/runtime PASS.