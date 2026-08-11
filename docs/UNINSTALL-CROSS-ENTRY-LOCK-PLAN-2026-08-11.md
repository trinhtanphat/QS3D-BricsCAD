# Uninstall cross-entry serialization plan — 2026-08-11

## Goal

Make direct uninstall participate in the same per-Windows-user mutation lane as detached one-click update, direct secure update and direct install, so DemandLoad/file removal cannot race package replacement or rollback.

## Shared mutex identity

Use exactly:

`Global\\QS3D-BricsCAD-V25-Update-<current Windows user SID>`

Resolve SID through `System.Security.Principal.WindowsIdentity.GetCurrent().User.Value`.

## Acquisition and lifetime

1. Keep the existing all-BricsCAD-closed refusal first.
2. Open the named mutex without initial ownership.
3. Attempt immediate `WaitOne(0)`; recover `AbandonedMutexException` as ownership and fail immediately on contention.
4. Acquire before `Assert-InstallDirectorySafeToRemove`, registry enumeration or any `ShouldProcess` mutation.
5. Hold through DemandLoad removal and optional install-directory deletion, including `-KeepFiles` registry-only mode.
6. Release one ownership level and dispose the handle in an outer `finally`.

## Preserved behavior

- Custom install paths outside `%LOCALAPPDATA%\\QS3D` still require `-Force` before recursive removal.
- Non-forced removal still verifies QS3D package identity markers.
- `VersionKeys`, `LanguageKeys`, `KeepFiles`, `Force`, and `ShouldProcess` semantics remain unchanged.
- No BricsCAD/process termination is introduced.

## Regression gate

Extend `scripts/preflight-update-cross-entry-lock.py` to require identical prefix/SID/nonblocking/abandoned/release behavior in update/install/uninstall and ordering:

`CAD refusal -> mutex acquire -> safe-path/registry inspection -> DemandLoad/file mutation -> mutex release`.

## Validation boundary

Static/source checks prove shared naming and ordering. Real contention against a detached updater/direct installer and Windows registry/filesystem behavior remain `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS will be claimed.
