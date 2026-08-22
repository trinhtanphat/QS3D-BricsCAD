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

`PluginEntry.Initialize()` eagerly constructs all three palette trees during NETLOAD. `PaletteCoordinator.Show()` then makes them visible and also calls `RefreshAll()`, while Workspace, RightPanel and QuantityInsight already perform their initial synchronous refresh work from their WPF `Loaded` handlers. On an already-open project this can duplicate project-sidecar, semantic quantity and CAD-layer refresh work on BricsCAD's UI thread during startup/show.

## Intended contract

- NETLOAD must register QS3D runtime/lifecycle/ribbon services without eagerly constructing palette WPF trees.
- First `QS3D` show may let the panels run their existing initial-load refresh, but must not synchronously run the same full refresh a second time from `PaletteCoordinator.Show()`.
- Existing explicit lifecycle/command refresh paths remain available.
- Native BricsCAD V25 exact runtime verification remains local-only; GitHub CI/static/build checks will be used for this remote patch.

## Implementation status

- Branch: `fix/netload-existing-project-hang-20260813`
- PR: #1031
- Source commits: `5363f0e2aa8b50cc4417af845c8dd166373898e4`, `4f0d6cc247bd19529695aabd145905f544895928`
- Remote readback confirms the PR changes only the two reserved startup files.
- No PR workflow run/status was attached to the current head when checked; exact BricsCAD V25 runtime verification remains required before claiming native PASS.

## Collision check

At registration time, open-PR searches for `netload` and `ProjectContextCoordinator` returned no matching open PR. This claim does not overlap the BLOCKED LOCAL-003 native Level geometry claim; it is limited to V25 plugin/palette startup lifecycle.
