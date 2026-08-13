# Work claim — V25 NETLOAD / QS3D existing-project startup hang

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T17:20:00+07:00`
- Baseline main SHA: `52c946738bb9423d58e6fff18eb8582072f4a19c`

## Scope

Fix the V25 host-startup/UI lifecycle path reported by the user: after fully closing BricsCAD, reopening it, opening an existing project drawing, then NETLOADing QS3D and/or running `QS3D`, the host can appear hung.

Reserved implementation surface still requiring no further remote source write:
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs`
- `scripts/preflight-netload-existing-project-startup.py`

`WorkspacePanel.xaml.cs` and `RightPanel.xaml.cs` were audited but are no longer reserved by this claim. Their persistent WPF `Loaded` handlers are a possible reparent/reload performance edge, but the reported first-show sequence does not prove those controls are unloaded/reloaded. Changing them without native evidence could suppress a legitimate reload refresh, so no speculative source mutation is claimed there.

## Observed source invariant

The original V25 startup path had three independent sources of synchronous UI-thread work:

1. `PluginEntry.Initialize()` eagerly constructed all three palette/WPF trees during NETLOAD.
2. `PaletteCoordinator.Show()` made the palettes visible and immediately called `RefreshAll()`, duplicating the panels' own first `Loaded` refresh work (project-sidecar binding, quantity preview/regeneration and CAD layer catalog).
3. After palette creation was made lazy, `RibbonInitializationCoordinator.Start()` still synchronously reconciled the large reflective ribbon tree from NETLOAD and from document-created/activated callbacks.

## Intended contract

- NETLOAD registers runtime/lifecycle/ribbon/update services without constructing palette WPF trees.
- Passive lifecycle/status refresh calls do not materialize unopened palettes.
- First `QS3D` show lets the panels own their initial refresh and does not immediately duplicate it with `RefreshAll()`.
- Ribbon reconciliation is eventual/idempotent but never executes synchronously inside NETLOAD or document-availability callbacks.
- Deferred ribbon work runs at `DispatcherPriority.ApplicationIdle` so input/document/palette work wins dispatcher priority.
- Existing explicit lifecycle/command refresh paths and teardown remain available.
- Native BricsCAD V25 exact runtime verification remains local-only; no native PASS may be claimed from source/static evidence alone.

## Implementation status

### Current source merged to `main`

- PR #1048 merged as `7a5e75b511aff0b55a2c692556121df0ffe9d25f`.
  - `PluginEntry.Initialize()` no longer calls `PaletteCoordinator.EnsureCreated()` during NETLOAD.
  - Passive `SetStatus`, `RefreshProject`, `RefreshCad`, and `ResetForUnavailableProject` no longer eagerly create palettes.
  - Explicit `Show()` / `ShowSafeMode()` still create palettes on demand.
- PR #1050 merged as `b37961d94d75ef943c569f01713fba8045b0693f`.
  - Removes the duplicate `RefreshAll()` from `PaletteCoordinator.Show()` while preserving selection sync and panel-owned initial loads.
- PR #1051 merged as `696603c014f306ff495a0d622e4c47eae44a17d7`.
  - Adds `scripts/preflight-netload-existing-project-startup.py` for lazy palette creation/passive refresh/first-show contracts.
- PR #1052 merged as `942df413335cd244e843926c209daf7c227cc53a`.
  - `RibbonInitializationCoordinator.Start()` and document-created/activated callbacks now only schedule the existing bounded retry timer; full reflective ribbon reconciliation is no longer synchronous inside those host callbacks.
  - Coordinator-level `_initialized` state prevents unnecessary retry restarts after success.
  - The startup preflight now rejects synchronous `TryInitializeAll()` calls from those callbacks.
- PR #1053 merged as `b8ed521e87fe579c45adc0f99e0b45e581bcbe46`.
  - The deferred ribbon retry timer now runs at `DispatcherPriority.ApplicationIdle`.
  - The preflight pins the idle-priority contract.
- Superseded PR #1031 and duplicate PR #1049 remain closed/unmerged and must not be revived over current `main`.

### Remote audit result

- `PluginEntry.Initialize()` now contains only startup identity capture, lifecycle registration, deferred ribbon coordinator start and asynchronous update bootstrap; it no longer constructs palettes.
- `SelectionSyncCoordinator.Attach()` gates its immediate refresh on `PaletteCoordinator.IsWorkspaceVisible`, so attaching selection sync during NETLOAD does not read selection/project UI while the workspace is unopened.
- `SourceReconcileUndoCoordinator.Attach()` is subscription-only; it does not load/bind a project during startup.
- `UpdateCoordinator.Start()` schedules `CheckAsync(true)` and the GitHub release request/manifest work crosses an async boundary; no synchronous network wait was found in the NETLOAD path.
- `RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity()` intentionally hashes the loaded DLL synchronously to preserve stale-binary truth at load time. That invariant was added for the user's stale-version diagnosis and is not moved/deferred without a replacement that preserves load-time fingerprint truth.
- No additional remote-safe source change is justified in this exact startup lane after #1053. The remaining decision boundary is native V25 behavior.

### Validation status

- Source/readback confirms all merged contracts above are present on `main`.
- The focused preflight is present and now guards palette laziness, no duplicate first-show refresh, deferred ribbon callbacks and application-idle priority.
- No pull-request workflow run was attached to the #1052 head when checked; this claim does not invent a CI-green result.
- The remote environment cannot execute licensed BricsCAD V25, so PROJECT=YES runtime qualification remains pending.

### Native validation still required

Keep this claim `ACTIVE` until the merged current build is exercised through the reported exact sequence:

1. Fully close every BricsCAD process.
2. Reopen BricsCAD V25.
3. Open the existing project DWG that previously reproduced the hang.
4. NETLOAD the V25 DLL built from current `main` and verify the command prompt returns promptly.
5. Run `QS3D` and verify the workspace opens without the previous freeze.
6. Hide/show or dock/undock the palette once; if a second-load freeze is observed, capture that native evidence before changing Workspace/RightPanel `Loaded` semantics.

Do not close this claim or report native PASS until that exact runtime path succeeds.

## Collision check

Open-PR searches for `netload`, `ProjectContextCoordinator`, `WorkspacePanel`, `RightPanel`, `startup`, and `RibbonInitializationCoordinator` found no overlapping open PR at the times each startup surface was reserved. This claim does not overlap the BLOCKED LOCAL-003 native Level geometry claim; it is limited to V25 plugin/palette/ribbon startup lifecycle.
