# Updater post-failure BricsCAD restart plan — 2026-08-11

## Goal

Restore host availability after a one-click update failure that occurs only after all BricsCAD processes have already closed, without creating duplicate instances for pre-close cancellation/readiness failures and without hiding the original update error.

## Current behavior

The detached worker restarts the captured `bricscad.exe` only after `update-v25.ps1` succeeds. Any exception after the all-BricsCAD wait falls into the worker catch block, writes the error, stops the transcript and exits 1. This leaves BricsCAD closed even when the previous QS3D payload was untouched or transactionally rolled back.

## Design

1. Add worker-local `$hostClosed = $false` before the try block.
2. Keep the current cancellation-aware all-BricsCAD wait unchanged.
3. Set `$hostClosed = $true` immediately after the wait exits and before signature/package/update work begins.
4. Keep the success restart against the exact captured `$bricscad` executable.
5. In `catch`, preserve the original exception in `$updateFailure` before any recovery action.
6. If `$hostClosed` is true, the captured executable still exists, and no `bricscad` process is currently running, attempt one recovery `Start-Process -FilePath $bricscad`.
7. Wrap recovery restart in its own try/catch and log a warning on failure; never replace the original update error or success/failure exit semantics.
8. Cancellation/readiness errors before `$hostClosed` becomes true must never restart the host.
9. Do not kill BricsCAD or weaken existing mutex/readiness/cancellation/AuthentiCode/package gates.

## Regression gate

Add `scripts/preflight-update-worker-restart.py` requiring:

- `$hostClosed = $false` initialization;
- all-BricsCAD wait before `$hostClosed = $true`;
- updater/signature/install work after hostClosed;
- normal success restart after updater success;
- catch preservation of the original failure;
- failure restart guarded by `$hostClosed`, executable existence and absence of a running BricsCAD process;
- recovery restart error isolation;
- final `exit 1` retained for the original update failure;
- no BricsCAD/process kill regression.

## Validation boundary

Static/source gates can prove control-flow ordering. Actual Windows process launch behavior, transactional rollback plus restart and real signed package failure scenarios remain `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS will be claimed.
