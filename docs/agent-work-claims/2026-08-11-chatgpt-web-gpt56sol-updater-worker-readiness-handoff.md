# Agent Work Claim — updater worker readiness handoff

- Claim ID: `UPDATER-WORKER-READINESS-HANDOFF-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T22:00:00+07:00`
- Baseline main SHA: `1e06451bd10b6439055d5274e8f9374b3d73c517`
- Parent lane: `UPDATER-CROSS-PROCESS-SINGLEFLIGHT-20260811` (`RELEASED`)

## Verified residual race

The cross-process named mutex prevents competing BricsCAD processes once the detached updater has opened the same named object. `Process.Start(...)` only proves that the PowerShell process was created; it does not prove the encoded worker has already opened a mutex handle. A very fast BricsCAD shutdown can theoretically end the parent-owned mutex before the child opens it, briefly destroying the named object and reopening a cross-process scheduling window.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-update-worker-readiness.py` (new)
- this claim file

## Intended contract

1. Parent creates a unique named `EventWaitHandle` readiness channel before worker launch and passes its name to the worker.
2. Worker opens the cross-process mutex handle first, then opens/signals the readiness event before blocking on mutex ownership.
3. Parent does not return `Scheduled` until readiness is signaled, proving the child holds a handle to the named mutex before any graceful BricsCAD close request can happen.
4. Bound readiness wait to a short explicit timeout. If readiness fails, terminate **only the detached updater child before installation can begin**, release the parent mutex, reset `_scheduled`, and return an error so BricsCAD is not asked to close.
5. Never terminate/kill a BricsCAD process. Existing all-BricsCAD wait, Authenticode, manifest/package, product-version, logging and restart contracts remain intact.

## Validation / release conditions

- Add a focused auto-discovered gate requiring worker-open-mutex -> ready-signal ordering, parent readiness wait before scheduling success, timeout cleanup, and explicit proof that any `Kill()` is scoped only to the detached updater `Process` variable before host close.
- Re-fetch current launcher/gate and verify ancestry.
- No Actions/release dispatch.
- Native timing behavior remains local qualification; no runtime PASS claimed remotely.
- Release claim only after source + gate land on `main`.
