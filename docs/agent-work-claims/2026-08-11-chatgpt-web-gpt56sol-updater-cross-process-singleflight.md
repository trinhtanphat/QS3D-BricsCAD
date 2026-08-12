# Agent Work Claim — updater cross-process single-flight

- Claim ID: `UPDATER-CROSS-PROCESS-SINGLEFLIGHT-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T21:52:00+07:00`
- Released: `2026-08-11T21:56:00+07:00`
- Baseline main SHA: `d2b7fa9aaaf378ebfd385ac7ed78c58040a2134d`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

`SecureUpdateLauncher._scheduled` was process-local. Two BricsCAD processes running under the same Windows user could each pass the in-process flag and launch a detached updater worker. Both workers waited for all BricsCAD processes to exit and could then race the same per-user install directory/registry update. Product-version rechecks reduced stale installs but did not provide a cross-process linearization boundary around worker scheduling/installation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-update-cross-process-singleflight.py`
- this claim file

## Completed changes

### Cross-process reservation — `12e9ecbf3b260dee6a887d6db744b3d4e7d4b85c`, explicit worker constructor refinement `3a507903e14a4f1db2ad4f85ced57f4ce8a47858`

- preserves the existing process-local `Interlocked` single-flight guard;
- before detached worker launch, resolves the current Windows SID and creates/owns `Global\QS3D-BricsCAD-V25-Update-<SID>`;
- the SID namespace prevents unrelated Windows users from sharing one logical per-user QS3D updater reservation while covering multiple BricsCAD processes/sessions for the same user;
- if the named reservation already exists, scheduling fails before a detached worker is created, so the second BricsCAD instance is not returned as `Scheduled` and is not asked to close;
- the parent keeps its mutex handle/ownership for the remaining BricsCAD process lifetime;
- worker launch failure releases/disposes the parent reservation and resets `_scheduled`;
- the exact mutex name is safely embedded in the detached encoded worker;
- worker uses explicit PowerShell 5.1-compatible `[System.Threading.Mutex]::new($false, $mutexName)`, waits for ownership and treats `AbandonedMutexException` from normal parent-process termination as successful ownership transfer;
- once owned, the worker still waits for **all** BricsCAD processes to exit, then verifies the installed updater signer and runs the existing hardened updater while holding the same cross-process reservation;
- worker releases/disposes the mutex in `finally`;
- no `Stop-Process`, `taskkill` or process `.Kill()` path was introduced; normal BricsCAD close/save prompts remain authoritative.

All prior updater security boundaries remain: WinVerifyTrust for the running plugin, exact current signer pinning, installed updater Authenticode check, GitHub package-host allowlist, product-version-aware updater arguments, external update log path and restart only after success.

### Regression gate — `3689dd63f3a18a9dcfb2b74bacd9432677df94ef`

Added auto-discovered `scripts/preflight-update-cross-process-singleflight.py` requiring:

- Windows SID-scoped global mutex naming;
- process-local flag -> cross-process reservation -> worker-launch ordering;
- existing-reservation rejection;
- launch-failure parent cleanup/reset;
- exact mutex handoff to worker;
- worker ownership wait + abandoned-mutex recovery;
- worker reservation ownership before the all-BricsCAD wait and updater invocation;
- worker release/disposal in `finally`;
- preservation of WinVerifyTrust, updater signature, GitHub host allowlist and graceful host close;
- explicit rejection of force-kill APIs.

## Validation / coordination

- Re-fetched current `SecureUpdateLauncher.cs` after the first mutex commit and refined the generated PowerShell constructor before closing.
- Compare at gate commit `3689dd63f3a18a9dcfb2b74bacd9432677df94ef` reported current `main` identical (`ahead_by: 0`, `behind_by: 0`).
- No force-push, reset or rebase was used.
- No GitHub Actions workflow was dispatched and no release was published.
- This connector session did not run two real BricsCAD processes plus the detached worker. The timing/ownership transfer and `Global\` named-object behavior on the customer Windows environment remain native qualification, not a remote PASS.

## Result

One-click updater scheduling now has both process-local and same-Windows-user cross-process single-flight boundaries. Concurrent BricsCAD instances can no longer independently authorize competing update workers by source contract; the detached worker inherits the same named reservation through the install window. Exact multi-instance Windows proof remains local/runtime qualification.
