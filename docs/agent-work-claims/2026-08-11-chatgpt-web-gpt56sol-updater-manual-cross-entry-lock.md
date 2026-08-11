# Agent Work Claim — serialize manual and automatic update entry points

- Claim ID: `UPDATER-MANUAL-CROSS-ENTRY-LOCK-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T22:46:30+07:00`
- Baseline main SHA: `721c0379762e95db440aec923580cc40c5dbd819`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The C# launcher/detached worker serializes one-click updates with `Global\\QS3D-BricsCAD-V25-Update-<Windows SID>`, but the supported direct PowerShell entry points do not participate in that reservation. After BricsCAD is closed, a user/local agent can run `update-v25.ps1` or `install-v25-autoload.ps1` while the detached auto-updater is preparing/installing the same per-user payload. The scripts' version/BricsCAD checks reduce stale writes but do not provide a single cross-entry mutation boundary around the shared install directory and DemandLoad registration.

## Reserved scope

- `scripts/update-v25.ps1`
- `scripts/install-v25-autoload.ps1`
- `scripts/preflight-update-cross-entry-lock.py` (new)
- `docs/UPDATER-MANUAL-CROSS-ENTRY-LOCK-PLAN-2026-08-11.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve all current updater manifest/product-version/archive/signature/recheck gates and installer identity/hash/signature/MOTW/rollback/DemandLoad contracts.
- Preserve the C# launcher/worker mutex name exactly; no launcher edit is required in this lane.
- Do not edit release workflow, UI/Coordinator, GitHub client, Core, Ribbon or unrelated lanes.
- No Actions dispatch or release publication.

## Intended contract

1. Both PowerShell entry points compute the same current Windows user SID and exact mutex name used by `SecureUpdateLauncher`.
2. Each script opens the named mutex and attempts immediate ownership (`WaitOne(0)`), treating `AbandonedMutexException` as ownership and failing fast if another process/thread owns the update lane.
3. `update-v25.ps1` holds the mutex from before installed-state/network/package preparation through nested installer completion and temp cleanup.
4. `install-v25-autoload.ps1` holds the mutex before package/registry/install-state inspection through transactional payload/registry commit or rollback.
5. Nested `update-v25.ps1 -> install-v25-autoload.ps1` remains valid because Windows mutex ownership is recursive on the same synchronous PowerShell thread; each script releases exactly one acquisition in `finally`.
6. Direct install/update cannot race a detached worker or another manual entry point for the same Windows user.
7. No BricsCAD process is killed and existing close-all-CAD preconditions remain intact.

## Validation / release conditions

- Commit a planning MD before implementation.
- Add an auto-discovered static gate proving identical SID/mutex naming, nonblocking acquisition, abandoned recovery, fail-fast contention and deterministic release in both scripts.
- Re-fetch current scripts/gate and verify ancestry with `behind_by: 0`.
- Actual simultaneous PowerShell/BricsCAD process behavior remains `LOCAL-009`; no remote runtime PASS claim.
- Mark `RELEASED` only after both scripts + gate are on `main`.