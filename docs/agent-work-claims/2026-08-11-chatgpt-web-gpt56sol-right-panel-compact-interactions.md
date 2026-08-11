# Work claim — Right Panel compact interactions

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-right-panel-compact-interactions-20260811-2118`
- Registered: `2026-08-11T21:18:00+07:00`
- Baseline main SHA: `d032fbb0699d6f03ec17f55304a59af88fd1af39`
- Priority: P0 source correctness + P1 screenshot parity

## Why this lane exists

The current `RightPanel.xaml` advertises `PreviewKeyDown="OnRightPanelPreviewKeyDown"` and a `Ctrl+F` layer-search shortcut, but the current `RightPanel.xaml.cs` has no `OnRightPanelPreviewKeyDown` implementation. That is a source-level WPF callback defect and also leaves the advertised keyboard workflow incomplete. The owner-provided BLT3D reference also favors a compact drawing/layer manager, so this lane adds presentation-only density without changing Xref/layer business behavior.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/RightPanel.Keyboard.cs` — new partial implementing the existing XAML keyboard callback only.
- `src/QS3D.BricsCAD.V25/UI/RightPanel.CompactShell.cs` — new idempotent presentation-only compact/density layer over existing named controls.
- `scripts/preflight-right-panel-compact-interactions.py` — focused auto-discovered source contract.
- `docs/UI-RIGHT-PANEL-COMPACT-INTERACTIONS-2026-08-11.md` — focused screenshot mapping/qualification note.
- this claim file for close-out.

`RightPanel.xaml` and `RightPanel.xaml.cs` are read/contract surfaces and should remain unchanged unless a source-proven integration blocker makes a direct edit unavoidable.

## Functional contract

- Implement the already-declared `OnRightPanelPreviewKeyDown` callback in the same partial class.
- `Ctrl+F` focuses/selects the real `LayerSearchBox`; `F5` calls the real existing refresh handler; `Esc` clears the layer search first, otherwise clears panel selections/CAD implied Xref selection through the existing real handlers.
- Do not create duplicate Xref/layer mutation code. Existing attach/reload/move/detach/window, visibility, lock and selection handlers remain the single behavior paths.
- Compact drawing/layer sections for a narrow right-docked palette, strengthen list minimum working space and section hierarchy, and surface shortcut hints without adding decorative/stub controls.
- Keep all styling presentation-only: no CAD command sender, project state, reporting, quantity formulas, Xref service or layer mutation service in the compact-shell partial.

## Coordination / exclusions

- Do not touch `PaletteCoordinator.cs`, `QuantityInsightPanel*`, `WallQuantityWindow*`, `QuantitySummaryWindow*`, `WorkspacePanel*`, Ribbon, Start Center, Project Tools, Core reporting/persistence/semantic mutation, updater/release/signing or GitHub Actions.
- The active quantity-description 3D locate and wall-quantity viewport-locate claims remain untouched.
- No native BricsCAD V25/WPF/HiDPI PASS claim from this remote connector session; existing local qualification boundaries remain authoritative.

## Validation plan

- Re-fetch current `main`, RightPanel XAML/code-behind and neighboring active claims before integration.
- Add a static gate proving the XAML callback has exactly one implementation, keyboard routes delegate to existing handlers, compact presentation remains mutation-free, and existing Xref/layer action bindings stay present.
- Source-review the focused gate and final diff. Do not dispatch GitHub Actions.
- Integrate by PR/fast-forward-safe merge on current `main`; never force push.

## Completion condition

The missing RightPanel keyboard callback is source-fixed, the narrow docked drawing/layer palette is compacted without duplicating behavior, the focused regression guard is on `main`, and this claim is closed with exact implementation/integration SHAs and truthful validation evidence.
