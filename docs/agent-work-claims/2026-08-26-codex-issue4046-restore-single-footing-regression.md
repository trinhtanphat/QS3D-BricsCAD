# Issue 4046 Móng đơn regression restoration claim

- Status: READY_FOR_REVIEW / SOURCE_FIX_COMPLETE
- Lane-Key: `issue-4046-restore-single-footing-regression`
- Owner/session: `codex-01a03be6`
- Issue: `#4046`
- Branch: `agent/codex/issue4046-restore-single-footing-regression`
- Exact baseline: `f22ab79cf7243b36dac77f39ae32436d355a5dce`
- Original feature merge: PR `#4021`, commit `0d489713ce3b302845a53d185bd02441a7341a89`
- Regression merge: PR `#4035`, commit `8b018f803ff30aabdb574c66bbcbc045c3f14260`
- Synchronized baseline: `origin/main` `3e33af4a8a8f97c191df79f1e323971f007934f5`

## Reserved scope

Restore the already-reviewed Móng đơn source, geometry smoke and focused preflight that disappeared from current `main`, then deliberately reconcile its `QS3DDRAWACTIVE` dispatch and smoke registration with all newer Foundation/Raft, Workspace, reporting and test changes. Update only the minimum source-safe handoff needed for runtime issue `#4034` to re-freeze the repaired exact main SHA.

Reserved implementation paths:

- `src/QS3D.BricsCAD.V25/SingleFootingCommands.cs`
- `src/QS3D.BricsCAD.V25/SingleFootingContract.cs`
- `src/QS3D.BricsCAD.V25/UI/SingleFootingDimensionsDialog.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SingleFooting.cs`
- `src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs` (Single Footing dispatch only)
- `src/QS3D.Core/Geometry/SingleFootingGeometry.cs`
- `tests/QS3D.Core.SmokeTests/SingleFootingGeometrySmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (Single Footing registration only)
- `scripts/preflight-single-footing-workflow.py`
- claim/handoff documentation for issues `#4046` and `#4034`

## Boundaries

- Do not restore unrelated files dropped by PR `#4035`; report those separately if necessary.
- Do not revert or weaken newer Raft/Foundation behavior, current Workspace handlers, multiselect safety, reporting provenance, PowerShell enumeration, or unrelated smoke registrations.
- Do not write or merge directly to `main`; validate, push this task branch and open a PR.
- No BricsCAD runtime is part of this source lane. Licensed V25/V26 interactive evidence remains owned by LOCAL_ONLY issue `#4034` after the source repair lands.

## Baseline evidence

At reservation time, current `origin/main` contains the original feature merge in ancestry but all seven standalone Móng đơn artifacts are absent. `ActiveFamilyQuickDrawCommands` no longer checks `SingleFootingContract`, so Foundation dispatch falls through to the generic route. The first-parent loss begins at merge `8b018f803ff30aabdb574c66bbcbc045c3f14260`.

## Restoration result

The seven dropped source/test/preflight artifacts were restored from the reviewed PR #4021 tree. The Foundation branch in `ActiveFamilyQuickDrawCommands` now routes a Single Footing Family to `SingleFootingCommands.DrawSingleFooting()` before the generic Foundation path, and the focused geometry smoke is registered without changing neighboring smoke registrations. No unrelated PR #4035 deletions were restored.

## Validation evidence

- `python scripts/preflight-single-footing-workflow.py` — PASS (UTF-8 console mode; focused tree/Add/dialog/Family/center-pick/native-ownership guard).
- `dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release -p:Platform=x64 --no-restore` — PASS, 0 warnings / 0 errors.
- `dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release -p:Platform=x64 --no-restore -m:1 -nr:false` — PASS, 0 warnings / 0 errors.
- `dotnet tests/QS3D.Core.SmokeTests/bin/x64/Release/net8.0/QS3D.Core.SmokeTests.dll` — PASS, `ALL PASS`.
- V25 Release build with `BRICSCAD_V25_DIR=C:\Program Files\Bricsys\BricsCAD V25 en_US` — PASS, 0 warnings / 0 errors.
- V26 Release build with `BRICSCAD_V26_DIR=C:\Program Files\Bricsys\BricsCAD V26 en_US` — PASS, 0 warnings / 0 errors.
- `python scripts/preflight-all.py` — PASS, all `1054` discovered feature gates passed.

This is source/build/smoke evidence only. Licensed BricsCAD V25/V26 interactive placement remains LOCAL_ONLY under issue #4034 and must use the exact post-landing `main` SHA plus an explicitly released shared-host slot.
