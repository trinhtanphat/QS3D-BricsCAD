# Work claim — V25 NETLOAD / QS3D existing-project startup hang

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T17:20:00+07:00`
- Baseline main SHA: `52c946738bb9423d58e6fff18eb8582072f4a19c`

## Scope

Fix the V25 host-startup/UI lifecycle path reported by the user: after fully closing BricsCAD, reopening it, opening an existing project drawing, then NETLOADing QS3D and/or running `QS3D`, the host can appear hung.

Reserved implementation surface:
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- a focused non-native regression/preflight if needed

## Observed source invariant

`PluginEntry.Initialize()` eagerly constructed all three palette trees during NETLOAD. `PaletteCoordinator.Show()` then made them visible and also called `RefreshAll()`, while Workspace, RightPanel and QuantityInsight already perform their initial synchronous refresh work from their WPF `Loaded` handlers. On an already-open project this could duplicate project-sidecar, semantic quantity and CAD-layer refresh work on BricsCAD's UI thread during startup/show.

## Intended contract

- NETLOAD must register QS3D runtime/lifecycle/ribbon services without eagerly constructing palette WPF trees.
- First `QS3D` show may let the panels run their existing initial-load refresh, but must not synchronously run the same full refresh a second time from `PaletteCoordinator.Show()`.
- Passive status/lifecycle refresh notifications must not materialize palettes that the user has never opened.
- Existing explicit lifecycle/command refresh paths remain available after palette creation.
- Native BricsCAD V25 exact runtime verification remains local-only; no native PASS may be claimed from source/static evidence alone.

## Implementation status

### Current source merged to `main`

- PR #1048 merged as `7a5e75b511aff0b55a2c692556121df0ffe9d25f`.
  - `PluginEntry.Initialize()` no longer calls `PaletteCoordinator.EnsureCreated()` during NETLOAD.
  - Passive `SetStatus`, `RefreshProject`, `RefreshCad`, and `ResetForUnavailableProject` no longer eagerly create palettes.
  - Explicit `Show()` / `ShowSafeMode()` still create palettes on demand.
- Follow-up PR #1050 merged as `b37961d94d75ef943c569f01713fba8045b0693f`.
  - Source commit `34e526604cb19b8172a1f82150e16bcecaa209b3` removes only the duplicate `RefreshAll()` call from `PaletteCoordinator.Show()`.
  - Workspace, RightPanel and QuantityInsight retain their own initial `Loaded` refresh handlers, and `SelectionSyncCoordinator.Refresh()` remains after show.
- Regression PR #1051 merged as `696603c014f306ff495a0d622e4c47eae44a17d7`.
  - Adds `scripts/preflight-netload-existing-project-startup.py` covering deferred NETLOAD palette construction, passive refresh laziness, on-demand `Show()` creation, no first-show `RefreshAll()`, and teardown presence.
- Superseded PR #1031 was closed without merge after a handoff comment pointed to #1048; its stale branch must not be merged over the current palette architecture.
- Duplicate PR #1049 was closed without merge after #1048/#1050/#1051 collectively superseded its source and gate diff.
- GitHub reported no status checks or workflow runs attached to the #1048/#1050 source heads when checked. This is source/readback/regression evidence, not a CI-green or native-runtime claim.

### Native validation still required

- Licensed BricsCAD V25 PROJECT=YES exact-SHA validation is still pending.
- Keep this claim `ACTIVE` until the merged source is exercised through the reported sequence: close BricsCAD → reopen → open an existing project DWG → NETLOAD the current V25 DLL → run `QS3D`.
- Do not close the claim or report native PASS unless that exact runtime path is observed to complete without the prior hang.

## Collision check

At registration time, open-PR searches for `netload` and `ProjectContextCoordinator` returned no matching open PR. This claim does not overlap the BLOCKED LOCAL-003 native Level geometry claim; it is limited to V25 plugin/palette startup lifecycle. Concurrent main changes observed during implementation were claim/doc-only on the compared snapshots and did not overlap these two startup files.
