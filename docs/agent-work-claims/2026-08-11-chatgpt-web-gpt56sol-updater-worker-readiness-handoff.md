# Agent Work Claim — updater worker readiness handoff

- Claim ID: `UPDATER-WORKER-READINESS-HANDOFF-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T22:00:00+07:00`
- Released: `2026-08-11T22:07:00+07:00`
- Baseline main SHA: `1e06451bd10b6439055d5274e8f9374b3d73c517`
- Parent lane: `UPDATER-CROSS-PROCESS-SINGLEFLIGHT-20260811` (`RELEASED`)

## Verified residual race

The cross-process named mutex prevented competing BricsCAD processes once the detached updater had opened the same named object. `Process.Start(...)` only proved that the PowerShell process was created; it did not prove the encoded worker had already opened a mutex handle. A very fast BricsCAD shutdown could theoretically end the parent-owned mutex before the child opened it, briefly destroying the named object and reopening a cross-process scheduling window.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-update-cross-process-singleflight.py`
- `scripts/preflight-update-worker-readiness.py`
- this claim file

## Completed changes

### Readiness handshake — `c6afb191c60469231893db6ca99e0831515a0131`

- parent creates a unique named manual-reset `EventWaitHandle` derived from the SID-scoped mutex name plus a GUID before worker launch;
- the readiness event name is passed to the encoded PowerShell worker;
- worker opens the named update mutex **handle first**, then opens/signals the readiness event, disposes the event handle, and only then blocks on mutex ownership;
- parent waits up to exactly 5 seconds for readiness before `TrySchedule(...)` can return success, so Update Center cannot request graceful BricsCAD close until the child demonstrably holds a handle to the named mutex object;
- readiness timeout invokes a narrow `TryTerminateUnreadyWorker(Process updater)` while the parent still owns the update mutex, so the child cannot have crossed the mutex ownership boundary into install;
- timeout resets the scheduling path through the existing outer failure cleanup, leaving BricsCAD open and retryable;
- child readiness event and mutex handles are disposed in worker `finally` paths;
- after readiness succeeds, the previous cross-process ownership transfer/all-BricsCAD wait/Authenticode/update/restart flow is unchanged.

### Existing cross-process gate reconciliation — `fccd68b7ffd649e38f214f35154c4073c165774d`

The active claim was explicitly extended before editing the existing gate. It now:

- permits exactly one `.Kill(` token: `updater.Kill();` inside the detached-worker readiness-timeout helper;
- rejects any additional/generic kill call, `process.Kill`, `Process.GetCurrentProcess().Kill`, `Stop-Process`, or `taskkill`;
- preserves SID mutex, worker ownership, all-BricsCAD wait, WinVerifyTrust, installed updater signature, package-host and graceful-close contracts.

This scoped child termination is not a BricsCAD force-close path. At timeout the scheduling BricsCAD remains alive, owns the cross-process mutex, and has not yet been asked to close.

### Focused readiness gate — `ae51e7cbd523aa43f423155f4e6a4851f34e6426`

Added auto-discovered `scripts/preflight-update-worker-readiness.py` requiring:

- unique bounded readiness channel and 5-second parent wait;
- worker order `open mutex handle -> open/signal readiness -> WaitOne mutex ownership`;
- parent order `Process.Start -> readiness wait -> scheduling success`;
- timeout child termination before outer reservation release / `_scheduled` reset;
- readiness signal before mutex ownership, all-BricsCAD wait and updater invocation;
- only detached `updater.Kill()` as a permitted termination call;
- worker readiness/mutex finally cleanup and retained graceful BricsCAD close.

## Validation / coordination

- A first attempt to add the focused gate received GitHub 409 because concurrent agents advanced `main`. No force update was used; latest `main` was re-read and compare from the preceding updater gate showed `behind_by: 0` with only unrelated files changed, then the gate was retried successfully.
- Compare from `ae51e7cbd523aa43f423155f4e6a4851f34e6426` to then-current `main` reported `behind_by: 0`; subsequent compared files were unrelated authoring/Core/docs work.
- No force-push, reset or rebase was used.
- No GitHub Actions workflow was dispatched and no release was published.
- This connector session did not execute the real Windows timing handoff. Event/mutex scheduling timing and PowerShell startup behavior remain local runtime qualification; no native PASS is claimed here.

## Result

The detached updater handoff now has an explicit readiness barrier: scheduling cannot succeed, and therefore BricsCAD cannot be asked to close, until the worker has opened a handle to the same cross-process update mutex. This closes the remaining named-object lifetime gap between `Process.Start` and child initialization while keeping all host termination graceful.
