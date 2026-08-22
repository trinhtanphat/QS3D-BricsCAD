# Agent Work Claim — serialize manual and automatic update entry points

- Claim ID: `UPDATER-MANUAL-CROSS-ENTRY-LOCK-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T22:46:30+07:00`
- Released: `2026-08-11T22:55:30+07:00`
- Baseline main SHA: `721c0379762e95db440aec923580cc40c5dbd819`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The C# launcher/detached worker already serialized one-click updates with `Global\\QS3D-BricsCAD-V25-Update-<Windows SID>`, but direct supported invocations of `update-v25.ps1` and `install-v25-autoload.ps1` did not participate. Once CAD was closed, manual update/install could therefore race the detached worker over the same per-user payload and DemandLoad registration.

## Completed changes

- `e96277b737166b3ef1a186d09a56b647a0f63ee9` — registered this lane before implementation.
- `f5f25c816631345e17b98d3459feafa8d8738067` — committed the cross-entry serialization plan before code.
- `51ec87753eca3c7e687b8226323cf9079d3c7774` — direct `update-v25.ps1` now resolves the current Windows SID, opens the exact launcher-compatible global mutex, attempts nonblocking `WaitOne(0)`, recovers abandoned ownership, fails fast on contention, and holds ownership from before manifest/network/installed-state preparation through package verification, nested signed installer completion and temp cleanup.
- `efc5d7ce568803236934dd133ae35a6efb113ec8` — direct `install-v25-autoload.ps1` now uses the same SID/mutex identity and holds ownership before package/registry/install-state inspection through transactional payload/DemandLoad commit or rollback.
- `bc11513875b40320a00bf21bdb1640689942f726` — added auto-discovered `scripts/preflight-update-cross-entry-lock.py`, requiring identical mutex naming, nonblocking ownership, abandoned recovery, fail-fast contention, acquire-before-state ordering, release-after-update/rollback ordering and preservation of security/atomicity markers.

## Resulting contract

1. Detached one-click worker, direct secure updater and direct installer share the same per-Windows-user update lane.
2. Manual entry points fail immediately instead of hanging when another thread/process owns the lane.
3. Nested synchronous `update-v25.ps1 -> install-v25-autoload.ps1` can recursively acquire the Windows mutex on the same PowerShell thread; each script releases one ownership level in its own outer `finally`.
4. Updater manifest/product-version/archive/signature/stale-state checks and installer SHA256 coverage/package identity/MOTW/DemandLoad rollback/readback remain intact.
5. Existing all-BricsCAD-closed preconditions remain intact and neither script force-terminates a host/process.

## Integration verification

- Re-fetched updater blob `51d0346b8a3a19766b9e505451c6895f00053ca3` and installer blob `39f554773f7a5e47140daa75098da868f04eb332` after writes; outer try/finally lock lifetimes are present around their existing state-sensitive bodies.
- Compare from `bc11513875b40320a00bf21bdb1640689942f726` to current `main` reported `behind_by: 0`; intervening commits did not touch these two entry points or this gate.
- The current container has no `pwsh`/`powershell`, so no PowerShell parser/runtime PASS is claimed here.
- No GitHub Actions workflow was dispatched and no release was published.

## Validation boundary

Source/static serialization is implemented. Actual contention among detached worker/direct updater/direct installer, recursive mutex ownership and signed install/update behavior remain `LOCAL-009 / PENDING_LOCAL`; this lane does not claim native/runtime PASS.