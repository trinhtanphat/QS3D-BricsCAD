# Right Panel compact interactions — 2026-08-11

## Goal

Bring QS3D's right-docked **Quản lý bản vẽ / Quản lý lớp** surface closer to the owner's BLT3D reference without replacing any native Xref/layer workflow. The reference is dense: drawing management occupies a short upper band, layer management gets the larger lower band, and frequent actions remain one click away.

## Source defect fixed

`RightPanel.xaml` already declares `PreviewKeyDown="OnRightPanelPreviewKeyDown"` and advertises `Ctrl+F` on the layer search box. The existing code-behind did not provide that callback. The dedicated `RightPanel.Keyboard.cs` partial restores that XAML contract and delegates only to controls/handlers that already exist.

Keyboard behavior:

- **Ctrl+F** — focus `LayerSearchBox` and select the current query.
- **F5** — execute the existing `OnRefreshClick` path.
- **Esc** — clear the layer filter first; when no filter exists, call the existing clear-layer-selection and clear-drawing/Xref-selection paths.

No parallel Xref or layer service implementation is introduced.

## Screenshot mapping

The compact shell keeps the existing functional sections and makes them denser for a narrow right dock:

- **Quản lý bản vẽ** remains the upper section with Attach, Reload, Move, Zoom Window, Detach and clear selection.
- **Quản lý lớp** remains the lower, larger section with native visibility/lock state, color, search, multi-selection and bulk actions.
- The drawing region is reduced to a 238-DIP preferred height with a 145-DIP minimum; the layer region continues to consume remaining height.
- `DrawingList` and `LayerList` retain explicit minimum working areas so compacting chrome cannot collapse the actual data surfaces.
- The row splitter uses preview resizing, matching the interaction policy used by the compact Workspace shell.
- Section titles receive a stronger hierarchy while the existing premium dark theme remains authoritative.
- Existing `Ctrl+F`, new visible `F5` hint and `Esc` filter hint are surfaced through tooltips rather than new decorative buttons.

## Behavior boundary

`RightPanel.CompactShell.cs` is intentionally presentation-only. It does not reference `XrefService`, `LayerVisibilityService`, project state, quantity/reporting code or `SendStringToExecute`. The existing `RightPanel.xaml.cs` remains the single implementation for real drawing/layer operations.

`RightPanel.Keyboard.cs` is also a routing layer, not a second implementation: it calls `OnRefreshClick`, `OnClearLayerSelectionClick` and `OnClearDrawingSelectionClick` instead of copying their logic.

## Concurrency boundary

This lane does not edit `PaletteCoordinator.cs`, `QuantityInsightPanel*`, `WallQuantityWindow*`, `QuantitySummaryWindow*`, `WorkspacePanel*`, Ribbon, Start Center, Project Tools, Core reporting/persistence/semantic mutation, updater/release/signing or GitHub Actions. In particular, the active quantity-description 3D-locate and wall-quantity viewport-locate work remain independent.

## Qualification

The focused source gate `scripts/preflight-right-panel-compact-interactions.py` checks:

1. the current RightPanel XAML remains well-formed and keeps all important real action bindings;
2. the declared `OnRightPanelPreviewKeyDown` callback has one dedicated implementation;
3. Ctrl+F/F5/Esc route to the intended existing controls/handlers;
4. the compact shell keeps density/minimum-size/section hierarchy behavior;
5. the presentation partial does not gain mutation/CAD-command dependencies.

Native BricsCAD V25 rendering, dock/undock behavior, physical keyboard focus and DPI behavior still require the repository's existing local WPF/palette qualification. This source lane does **not** claim a licensed BricsCAD runtime PASS.
