# Manual/automatic updater cross-entry lock plan — 2026-08-11

## Goal

Serialize all supported QS3D V25 installation/update entry points for the same Windows user so detached one-click update, direct secure update and direct installer invocation cannot mutate the shared install directory/DemandLoad registration concurrently.

## Shared lock identity

Use the same named Windows mutex as the C# launcher:

`Global\\QS3D-BricsCAD-V25-Update-<current Windows user SID>`

Both scripts resolve the SID through `System.Security.Principal.WindowsIdentity.GetCurrent().User.Value` and fail closed if the SID cannot be resolved.

## Acquisition contract

1. Keep the existing `Get-Process -Name bricscad` refusal before mutation.
2. Create/open the shared named mutex with initial ownership disabled.
3. Attempt `WaitOne(0)` so a competing updater/installer fails immediately instead of hanging an interactive terminal.
4. Treat `System.Threading.AbandonedMutexException` as successful ownership recovery.
5. On contention, dispose the mutex handle and throw an actionable "another QS3D install/update is active" error.
6. Hold ownership through the entire state-sensitive operation and release exactly once in `finally`.

## `update-v25.ps1`

- Acquire before manifest/installed-version/network/package preparation.
- Keep ownership through package verification, concurrent installed-state recheck, nested signed installer invocation and temp-directory cleanup.
- Nested installer acquisition is recursive on the same synchronous PowerShell thread and releases one recursion level when it returns.

## `install-v25-autoload.ps1`

- Acquire before package integrity/identity and registry/install-state inspection.
- Keep ownership through stage/backup swap, DemandLoad write/readback, success cleanup or transactional rollback.
- Release in an outer `finally` regardless of success/failure.

## Regression gate

Add `scripts/preflight-update-cross-entry-lock.py` requiring both scripts to:

- use the exact shared mutex prefix and Windows SID;
- use immediate `WaitOne(0)` plus abandoned-mutex recovery;
- fail on contention;
- release/dispose deterministically;
- acquire before relevant state/network/install work;
- preserve no-BricsCAD-running checks and existing security/rollback markers.

## Validation boundary

Static/source verification proves naming and control-flow ordering. Real contention among detached worker, direct update and direct installer processes is Windows-local behavior and remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS will be claimed.
