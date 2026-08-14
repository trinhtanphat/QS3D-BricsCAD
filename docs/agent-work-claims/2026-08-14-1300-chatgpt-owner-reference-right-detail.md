# Work claim — owner-reference RightPanel detail stack

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-owner-reference-followup`
- Registered: `2026-08-14T13:00:00+07:00`
- Baseline main SHA: `4da4caa528f9fb8614bd180a9e824612920699e2`
- Scope amended: `2026-08-14T13:02:00+07:00` from refreshed `main` `babe450f826d74922239dd15c44306d9a0af6067` after read-only inspection of the existing RightPanel XAML, view model, code-behind and compact-shell initializer.
- Priority: follow the integrated `docs/OWNER-REFERENCE-COMPLETION-PLAN-2026-08-14.md` Phase 1 item for richer right-detail inspection while continuing the owner's full-session/project review.

## Reserved scope

Complete a bounded, presentation-only RightPanel inspection/detail layer using data already exposed by the current drawing/Xref and layer lists. The owner-reference screenshots show a dense right-side management surface; current source has the actions and lists but selected-item metadata is visible only inside table columns/status text.

Add compact selected-Xref/drawing and selected-layer detail cards so operators can see the active target, kind/path/scale/lock/instance metadata and native layer visibility/lock/color state without changing CAD or QS3D semantic state.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.ReferenceDetail.cs` — new presentation-only partial that registers a second idempotent `RightPanel.Loaded` class handler through a static field initializer, then adds compact cards bound to existing list selection
- `scripts/preflight-owner-reference-right-detail.py`
- `docs/OWNER-REFERENCE-COMPLETION-PLAN-2026-08-14.md`
- this claim file for close-out

`RightPanel.xaml`, `RightPanel.xaml.cs`, `RightPanel.CompactShell.cs` and `RightPanelViewModel.cs` were inspected read-only to confirm available bindings and lifecycle boundaries; they are not implementation write surfaces in this lane. The new partial avoids editing active startup/lifecycle code or the existing compact-shell implementation.

## Excluded scope

- No edits to `RightPanel.xaml`, `RightPanel.xaml.cs`, `RightPanel.CompactShell.cs`, `RightPanelViewModel.cs`, `PluginEntry.cs`, `PaletteCoordinator.cs`, `DocumentLifecycleCoordinator.cs`, or `RibbonInitializationCoordinator.cs`; the active NETLOAD/startup lifecycle lane remains untouched.
- No new CAD mutation, semantic mutation, Xref/layer service behavior, context-menu command, or native API assumption.
- No `#73` physical junction, `#80` native grip/edit sync, Semantic Tag/Table/Sheet/Revision Cloud runtime work, Source Reconcile, Curtain, Model Health, release/signing, or LOCAL_ONLY qualification.
- No proprietary BLT assets/icons/code.
- No GitHub Actions dispatch; CI remains manual-only.

## Validation plan

- Re-fetch `main` before implementation and verify no concurrent RightPanel detail claim/change landed.
- Bind the detail cards only to existing `DrawingList.SelectedItem` / `LayerList.SelectedItem` properties; no command/event mutation path is introduced.
- Keep layouts compact, truncation-safe and useful at the current RightPanel minimum width.
- Add a deterministic static guard for selected-item bindings, labels, idempotent Loaded registration and the no-new-code-behind/no-native-mutation boundary.
- Read back the merged source; native Windows DPI/docking visual acceptance remains licensed V25/local evidence.

## Coordination

The active NETLOAD claim owns `RightPanel.xaml.cs` startup/Loaded behavior. This lane uses only a new presentation partial and a new standalone preflight/doc status. Recent quantity/ribbon-static/Source-Reconcile claims are non-overlapping, and comparison through `babe450f...` shows no intervening RightPanel surface change after this claim was registered.

## Completion condition

The compact selected-target detail stack is pushed to current `main` with a focused static guard and plan status update, without changing current RightPanel mutation behavior or overlapping active claims.
