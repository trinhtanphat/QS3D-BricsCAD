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
- `src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs`
- `scripts/preflight-netload-existing-project-startup.py`

Remote source work for the currently identified startup lifecycle is integrated. Keep this claim ACTIVE because exact licensed BricsCAD V25 reproduction/qualification is still pending.

## Observed source invariant

The original V25 startup/show path had four avoidable sources of repeated or synchronous UI-thread work:

1. `PluginEntry.Initialize()` eagerly constructed all three palette/WPF trees during NETLOAD.
2. `PaletteCoordinator.Show()` made the palettes visible and immediately called `RefreshAll()`, duplicating the panels' own first `Loaded` refresh work.
3. `RibbonInitializationCoordinator.Start()` synchronously reconciled the large reflective ribbon tree from NETLOAD and document-created/activated callbacks.
4. `WorkspacePanel` and `RightPanel` used permanent anonymous `Loaded` handlers, allowing WPF unload/reload or palette reparenting to repeat constructor-owned project/CAD refresh work without an explicit refresh request.

## Intended contract

- NETLOAD registers runtime/lifecycle/ribbon/update services without constructing palette WPF trees.
- Passive lifecycle/status refresh calls do not materialize unopened palettes.
- First `QS3D` show lets each panel own exactly one initial refresh and does not immediately duplicate it with `RefreshAll()`.
- Workspace and RightPanel initial `Loaded` refreshes are one-shot per panel instance; later refreshes come through explicit lifecycle/command/manual paths.
- Ribbon reconciliation is eventual/idempotent but never executes synchronously inside NETLOAD or document-availability callbacks.
- Deferred ribbon work runs at `DispatcherPriority.ApplicationIdle`.
- Existing explicit lifecycle/command refresh paths and teardown remain available.
- Native BricsCAD V25 exact runtime verification remains local-only; source/static evidence cannot manufacture `LOCAL_PASS`.

## Implementation status

### Current source merged to `main`

- PR #1048 merged as `7a5e75b511aff0b55a2c692556121df0ffe9d25f`.
  - `PluginEntry.Initialize()` no longer calls `PaletteCoordinator.EnsureCreated()` during NETLOAD.
  - Passive `SetStatus`, `RefreshProject`, `RefreshCad`, and `ResetForUnavailableProject` no longer eagerly create palettes.
- PR #1050 merged as `b37961d94d75ef943c569f01713fba8045b0693f`.
  - Removes duplicate `RefreshAll()` from `PaletteCoordinator.Show()` while preserving selection sync and panel-owned initial loads.
- PR #1051 merged as `696603c014f306ff495a0d622e4c47eae44a17d7`.
  - Adds `scripts/preflight-netload-existing-project-startup.py`.
- PR #1052 merged as `942df413335cd244e843926c209daf7c227cc53a`.
  - Ribbon initialization and document callbacks now schedule the bounded retry path instead of synchronously reconciling the Ribbon.
- PR #1053 merged as `b8ed521e87fe579c45adc0f99e0b45e581bcbe46`.
  - Deferred Ribbon retry now runs at `DispatcherPriority.ApplicationIdle`.
- PR #1054 merged as `cad4466829fe5b134b2701b6fced5f4846997204`.
  - `WorkspacePanel` and `RightPanel` use named `OnInitialLoaded` handlers that self-unsubscribe before the first refresh.
  - Explicit/manual/lifecycle refresh methods remain unchanged.
  - The startup preflight requires the one-shot subscribe/unsubscribe contract and rejects the old permanent anonymous `Loaded` refresh handlers.
- Superseded PR #1031 and duplicate PR #1049 remain closed/unmerged and must not be revived over current `main`.

### Remote audit result

- NETLOAD no longer constructs palettes or synchronously reconciles the full Ribbon tree.
- Selection sync remains gated on Workspace visibility during startup.
- Source Reconcile Undo startup registration remains subscription-only.
- Update checking crosses its asynchronous boundary rather than synchronously waiting on release-network work in NETLOAD.
- The loaded-binary identity capture remains synchronous intentionally so stale-binary diagnostics retain load-time truth.
- Workspace/RightPanel no longer repeat constructor-owned initial refresh merely because the same panel instance receives a later WPF `Loaded` event; supported explicit refresh paths remain available.
- No additional remote-safe source change is currently justified in this exact startup lane after #1054. The remaining decision boundary is native V25 behavior.

### Validation status

- Source/readback confirms the startup contracts above are integrated.
- The focused preflight guards palette laziness, no duplicate first-show refresh, deferred application-idle Ribbon reconciliation, and one-shot Workspace/RightPanel initial refresh.
- No status checks or pull-request workflow runs were attached to the #1054 head when inspected. Do not report CI-green from this evidence.
- Licensed BricsCAD V25 runtime qualification remains pending.

### Native validation still required

Keep this claim `ACTIVE` until a clean exact intended SHA containing #1048/#1050/#1051/#1052/#1053/#1054 is exercised through the reported sequence:

1. Fully close every BricsCAD process.
2. Reopen BricsCAD V25.
3. Open the existing project DWG that previously reproduced the hang, with its existing QS3D project/sidecar.
4. NETLOAD the exact candidate V25 DLL and verify the command prompt returns promptly.
5. Run `QS3D` and verify the workspace opens without the previous freeze while preserving the canonical existing project identity.
6. Hide/show and dock/undock the palette once; the same Workspace/RightPanel instances must not repeat their constructor-owned heavy initial refresh merely because WPF raises `Loaded` again.
7. Exercise normal explicit Refresh and one document-lifecycle refresh after the palettes exist and verify those supported refresh paths still work.
8. Record the exact Git SHA/ProductVersion plus sanitized cleanup/process evidence.

Do not close this claim or report native PASS until that exact runtime path succeeds.

## Collision check

Open-PR searches for `netload`, `ProjectContextCoordinator`, `WorkspacePanel`, `RightPanel`, `startup`, and `RibbonInitializationCoordinator` found no overlapping open PR at the times each startup surface was reserved. Concurrent changes observed while #1054 was prepared were on unrelated responsive-UI files. This claim does not overlap the BLOCKED LOCAL-003 native Level geometry claim; it is limited to V25 plugin/palette/ribbon startup lifecycle.
