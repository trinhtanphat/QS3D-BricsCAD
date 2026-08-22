# Work claim — V25 NETLOAD / QS3D existing-project startup hang

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T17:20:00+07:00`
- Baseline main SHA: `52c946738bb9423d58e6fff18eb8582072f4a19c`
- Scope extended: `2026-08-13T23:17:00+07:00` from refreshed `main` baseline `5940d3f93c9f244a3bfd721d57c5702ec82b8d70`
- Latest remote source checkpoint: `df846111efbb1777babadeee4c312bdb4a58a4ba`

## Scope

Fix the V25 host-startup/UI lifecycle path reported by the user: after fully closing BricsCAD, reopening it, opening an existing project drawing, then NETLOADing QS3D and/or running `QS3D`, the host can appear hung.

Reserved implementation surface:
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs`
- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs`
- `scripts/preflight-netload-existing-project-startup.py`
- the matching exact-SHA NETLOAD/reopen scenario already parked under `LOCAL-001` in `docs/LOCAL-AGENT-INBOX.md`

Remote source work for the identified startup lifecycle is integrated through the latest checkpoint above. Keep this claim `ACTIVE` because exact licensed BricsCAD V25 reproduction/qualification remains local-only and pending.

## Observed source invariant

The V25 startup/show path has had five avoidable sources of repeated or synchronous UI-thread work:

1. `PluginEntry.Initialize()` eagerly constructed all three palette/WPF trees during NETLOAD.
2. `PaletteCoordinator.Show()` made the palettes visible and immediately called `RefreshAll()`, duplicating the panels' own first `Loaded` refresh work.
3. `RibbonInitializationCoordinator.Start()` synchronously reconciled the large reflective ribbon tree from NETLOAD and document-created/activated callbacks.
4. `WorkspacePanel` and `RightPanel` used permanent anonymous `Loaded` handlers, allowing WPF unload/reload or palette reparenting to repeat constructor-owned project/CAD refresh work without an explicit refresh request.
5. The 23:17 source refresh found `DocumentLifecycleCoordinator` still performing project/selection/UI reconciliation inline during NETLOAD and document callbacks. The first concurrent follow-up over-corrected by also deferring persistence and Undo subscriptions; the final source split keeps those critical lightweight hooks immediate and defers only project/selection/UI work.

## Intended contract

- NETLOAD registers runtime/lifecycle/ribbon/update services without constructing palette WPF trees.
- Passive lifecycle/status refresh calls do not materialize unopened palettes.
- First `QS3D` show lets each panel own exactly one initial refresh and does not immediately duplicate it with `RefreshAll()`.
- Workspace and RightPanel initial `Loaded` refreshes are one-shot per panel instance; later refreshes come through explicit lifecycle/command/manual paths.
- Ribbon reconciliation is eventual/idempotent but never executes synchronously inside NETLOAD or document-availability callbacks; deferred Ribbon work runs at `DispatcherPriority.ApplicationIdle`.
- Save/close persistence hooks plus Source Reconcile/Curtain Undo observers attach synchronously before a higher-priority input, command, save or close can outrun them.
- Selection sync, existing-project inspection and palette/UI reconciliation are coalesced and run at `DispatcherPriority.ApplicationIdle` outside NETLOAD/document host callbacks.
- Document teardown stays synchronous: pending reconcile is cancelled and native handlers are detached before the document disappears.
- Existing explicit lifecycle/command refresh paths remain available.
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
  - The startup preflight requires the one-shot subscribe/unsubscribe contract and rejects the old permanent anonymous `Loaded` refresh handlers.
- `9c9216a67e16fa234b72fb6e175173f7dccdb194` — `fix(netload): defer document lifecycle reconciliation to idle`.
  - Concurrent implementation introduced coalesced `ApplicationIdle` lifecycle reconciliation and synchronous destroy-time cancellation/detach.
  - Review found it also deferred persistence and Undo observer subscriptions, which could allow higher-priority input/commands to run before those hooks exist.
- `31b47d780911673321d36dbc11b527b01e2cb891` — `test(netload): guard idle document lifecycle reconciliation`.
  - Added the first focused lifecycle-idle preflight coverage.
- `261635f88bdf42a6e4bc17915bf6e6d887daf83e` — `fix(netload): keep critical document hooks immediate`.
  - Keeps `AttachProjectPersistence`, `SourceReconcileUndoCoordinator.Attach`, and `CurtainWallUndoCoordinator.Attach` immediate.
  - Keeps `SelectionSyncCoordinator.Attach`, `EnsureProject`, selection refresh and palette/project UI work in the coalesced `ApplicationIdle` path.
  - Keeps pending-work cancellation and detach synchronous on document teardown.
- `df846111efbb1777babadeee4c312bdb4a58a4ba` — `test(netload): preserve immediate critical lifecycle hooks`.
  - Pins the critical-hook/immediate versus project-selection-UI/deferred boundary so future changes cannot silently move save/Undo hooks back behind idle scheduling.
- Superseded PR #1031 and duplicate PR #1049 remain closed/unmerged and must not be revived over current `main`.

### Remote audit result

- NETLOAD no longer constructs palettes or synchronously reconciles the full Ribbon tree.
- Workspace/RightPanel no longer repeat constructor-owned initial refresh merely because the same panel instance receives a later WPF `Loaded` event.
- Document lifecycle now has an explicit two-tier contract: critical save/close/Undo subscriptions are immediate; project/selection/palette reconciliation is deferred/coalesced at `ApplicationIdle`.
- `OnDocumentToBeDestroyed` cancels queued reconciliation before detaching persistence, Source Reconcile Undo, Curtain Undo, selection sync and project context, preventing a pending idle callback from resurrecting stale document work.
- The loaded-binary identity capture remains synchronous intentionally so stale-binary diagnostics retain load-time truth.
- Update checking crosses its asynchronous boundary rather than synchronously waiting on release-network work in NETLOAD.
- No further remote-safe startup patch is currently justified by the inspected source after `df846111...`; the remaining acceptance boundary is native V25 behavior on the exact candidate SHA.

### Validation status

- Post-push GitHub source readback at `df846111efbb1777babadeee4c312bdb4a58a4ba` confirms `DocumentLifecycleCoordinator` keeps `AttachCriticalServices` outside the idle reconcile path and keeps `SelectionSyncCoordinator.Attach`/`EnsureProject` inside `ReconcileDocument`.
- The focused preflight now guards palette laziness, no duplicate first-show refresh, one-shot Workspace/RightPanel load, deferred ApplicationIdle Ribbon reconciliation, immediate critical document hooks, deferred/coalesced project-selection-UI reconcile and synchronous teardown cancellation.
- The updated preflight content passed local Python syntax/static-boundary inspection during this remote pass; no licensed BricsCAD runtime PASS is inferred from that static evidence.
- Cloud V25 release run #129 / `31712690583` on SHA `e7318dc41bc04b26bee9f5b4f6b985d38144c9bd` completed `SUCCESS`, but it predates the lifecycle-idle follow-up and therefore does not validate `261635f...` / `df846111...`.
- Repository CI policy is manual-only. The owner request in this lane is source fix/update/commit/push, not an explicit Actions dispatch, so no new GitHub Actions run was started.
- `LOCAL-001` remains the authoritative existing local V25 build/load/NETLOAD/save-reopen/multi-DWG queue; no duplicate LOCAL_ONLY item was created.

### Native validation still required

Keep this claim `ACTIVE` until a clean exact intended SHA containing the full startup source contract is exercised through the reported sequence:

1. Fully close every BricsCAD process.
2. Reopen BricsCAD V25.
3. Open the existing project DWG that previously reproduced the hang, with its existing QS3D project/sidecar.
4. NETLOAD the exact candidate V25 DLL and verify the command prompt returns promptly.
5. Immediately exercise a benign command/save boundary before idle reconciliation and confirm persistence/Undo observers are already attached while NETLOAD remains responsive.
6. Run `QS3D` and verify the workspace opens without the previous freeze while preserving canonical existing-project identity.
7. Activate/switch documents and verify queued lifecycle reconciliation completes without losing persistence, Undo, selection sync or project refresh behavior.
8. Hide/show and dock/undock the palette once; the same Workspace/RightPanel instances must not repeat constructor-owned heavy initial refresh merely because WPF raises `Loaded` again.
9. Close a document with lifecycle work pending and verify pending work is cancelled/detached safely with no stale callback or handler leak.
10. Record the exact Git SHA/ProductVersion plus sanitized cleanup/process evidence.

Do not close this claim or report native PASS until that exact runtime path succeeds.

## Collision check

The current NETLOAD claim belongs to this same `chatgpt-web-gpt56sol` lane and was extended rather than duplicated. During implementation another concurrent writer landed `9c9216a...` and `31b47d...` on the same reserved surface; those commits were reviewed and reused instead of overwritten, then narrowed safely by `261635f...` and `df846111...`. Concurrent Floor Level, quantity, measurement, diagnostics/update UX and other changes remain outside this scope. This claim does not take LOCAL-003 Level geometry work; exact licensed native qualification remains under the existing LOCAL_ONLY queue.
