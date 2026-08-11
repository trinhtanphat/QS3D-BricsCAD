# Work claim — updater restart single-flight lifecycle

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-updater-restart-singleflight`
- Registered: `2026-08-11T21:29:00+07:00`
- Baseline main SHA: `d77afad0332ef00d1b3e5d4bf65b18baf9ec4770`
- Priority: keep automatic update discovery reliable when the updater lifecycle is stopped and restarted while an older GitHub check is still in flight.

## Confirmed defect

`UpdateCoordinator.Stop()` advances `_generation` but leaves `_inFlight` pointing at the old unfinished task. A subsequent `Start()` advances generation again and calls `CheckAsync(true)`, but `CheckAsync` returns any unfinished `_inFlight` without checking which generation owns it. The restarted coordinator can therefore adopt the stale pre-stop task; when that task completes its generation is no longer current, so it publishes nothing, and the new lifecycle never actually performs its own automatic check.

This is a source-proven lifecycle/single-flight defect; it does not depend on BricsCAD rendering or network timing beyond an older check still being unfinished at restart.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- new focused auto-discovered static preflight under `scripts/`
- this claim file for close-out

## Intended contract

- Single-flight reuse is allowed only for an in-flight check that belongs to the current coordinator generation.
- Stop/restart must not reuse an older-generation task.
- A new generation starts its own check even if an older network task is still completing; stale results remain unable to publish through the existing generation guard.
- Preserve strict release/SemVer/security selection, WinVerifyTrust/publisher checks, detached updater behavior and Update Center UI semantics.

## Excluded scope

- No edits to release/update PowerShell, manifest schema, `SecureUpdateLauncher.cs`, Update Center UI, Ribbon, PluginEntry, Quantity/BQ, Workspace, Direct Draw, Core, signing or LOCAL inbox.
- No GitHub Actions dispatch and no remote network/runtime PASS claim.

## Validation plan

Re-fetch current source immediately before writes. Track the generation owning `_inFlight`, gate task reuse by that generation, clear lifecycle ownership on Stop as needed, and add a focused source contract that rejects generation-blind `_inFlight` reuse. Re-fetch diffs/current ancestry after merge without force-push.

## Completion condition

A restarted update lifecycle cannot be starved by an unfinished task from a previous generation, stale tasks still cannot publish, focused regression coverage is on `main`, and native signed-update qualification remains LOCAL-009.