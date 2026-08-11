# Agent Work Claim — updater cross-process single-flight

- Claim ID: `UPDATER-CROSS-PROCESS-SINGLEFLIGHT-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T21:52:00+07:00`
- Baseline main SHA: `d2b7fa9aaaf378ebfd385ac7ed78c58040a2134d`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

`SecureUpdateLauncher._scheduled` is process-local. Two BricsCAD processes running under the same Windows user can each pass the in-process flag and launch a detached updater worker. Both workers wait for all BricsCAD processes to exit and can then race the same per-user install directory/registry update. The later product-version rechecks reduce stale installs but do not provide a cross-process linearization boundary around worker scheduling/installation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-update-cross-process-singleflight.py` (new)
- this claim file

## Explicit non-overlap

- The earlier Authenticode, restart-single-flight, schedule-lifecycle and manifest-preclose lanes are completed and their contracts must remain intact.
- Do not edit UpdateCoordinator, GitHubReleaseClient, UpdateManifestProbe, UI, PowerShell updater/installer, release workflow or unrelated product lanes.

## Intended contract

1. Before starting a detached worker, acquire a named Windows mutex scoped globally but namespaced by the current Windows user SID, so multiple BricsCAD processes/sessions for the same per-user QS3D install cannot schedule competing workers.
2. Keep the parent BricsCAD process holding that reservation while it remains alive. If another process already owns/created the same reservation, reject one-click scheduling without closing that second BricsCAD instance.
3. Pass the exact mutex name to the detached worker. The worker acquires/recovers the mutex after the scheduling BricsCAD exits, treating an abandoned mutex from normal parent-process termination as ownership, and holds it through wait/update/restart completion.
4. If detached worker launch fails, release/dispose the parent reservation and reset the in-process `_scheduled` flag.
5. The worker must still wait for all BricsCAD processes and must never kill them.
6. Preserve WinVerifyTrust/current signer pinning, installed updater signer verification, GitHub host allowlist, product-version updater arguments, graceful close behavior and external log path.

## Validation / release conditions

- Add a focused auto-discovered preflight requiring SID-namespaced cross-process mutex reservation, parent launch-failure cleanup, worker abandoned-mutex recovery/hold/release, and no process-kill regression.
- Re-fetch `SecureUpdateLauncher.cs` after commit and verify current-main ancestry.
- Do not dispatch GitHub Actions or publish a release.
- Actual two-BricsCAD-instance Windows execution remains local/runtime qualification; no remote runtime PASS claim.
- Mark `RELEASED` only after source + regression gate are committed on `main`.
