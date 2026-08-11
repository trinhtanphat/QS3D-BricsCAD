# Right Panel compact interactions — 2026-08-11

## Goal

Bring QS3D's right-docked **Quản lý bản vẽ / Quản lý lớp** surface closer to the owner's BLT3D reference without replacing any native Xref/layer workflow. The reference is dense: drawing management occupies a short upper band, layer management gets the larger lower band, and frequent actions remain one click away.

## Keyboard ownership correction

The XAML route `PreviewKeyDown="OnRightPanelPreviewKeyDown"` was already backed by the pre-existing canonical partial `RightPanel.SearchShortcuts.cs`. A later compact-interactions change mistakenly added a second `RightPanel.Keyboard.cs` partial with the same method signature. Because all partials compile into the same `RightPanel` class, that duplicate member is invalid C# and has been removed.

`RightPanel.SearchShortcuts.cs` remains the single keyboard callback owner, matching the older `scripts/preflight-right-panel-layer-search.py` contract. The keyboard behavior is therefore:

- **Ctrl+F** — focus `LayerSearchBox` and select the current query.
- **F5** — call the canonical `Refresh()` path.
- **Esc** — when `LayerSearchBox` has keyboard focus and its filter is non-empty, clear that filter.

No parallel Xref or layer service implementation is introduced, and the compact lane does not create a second keyboard route.

## Screenshot mapping

The compact shell keeps the existing functional sections and makes them denser for a narrow right dock:

- **Quản lý bản vẽ** remains the upper section with Attach, Reload, Move, Zoom Window, Detach and clear selection.
- **Quản lý lớp** remains the lower, larger section with native visibility/lock state, color, search, multi-selection and bulk actions.
- The drawing region is reduced to a 238-DIP preferred height with a 145-DIP minimum; the layer region continues to consume remaining height.
- `DrawingList` and `LayerList` retain explicit minimum working areas so compacting chrome cannot collapse the actual data surfaces.
- The row splitter uses preview resizing, matching the interaction policy used by the compact Workspace shell.
- Section titles receive a stronger hierarchy while the existing premium dark theme remains authoritative.
- Existing `Ctrl+F`, `F5` and `Esc` hints are surfaced through tooltips rather than new decorative buttons.

## Behavior boundary

`RightPanel.CompactShell.cs` is intentionally presentation-only. It does not reference `XrefService`, `LayerVisibilityService`, project state, quantity/reporting code or `SendStringToExecute`. The existing `RightPanel.xaml.cs` remains the single implementation for real drawing/layer operations.

`RightPanel.SearchShortcuts.cs` is also a routing/presentation layer rather than a second business implementation. It owns the one XAML keyboard callback and does not duplicate Xref/layer mutation internals.

## Concurrency boundary

This lane does not edit `PaletteCoordinator.cs`, `QuantityInsightPanel*`, `WallQuantityWindow*`, `QuantitySummaryWindow*`, `WorkspacePanel*`, Ribbon, Start Center, Project Tools, Core reporting/persistence/semantic mutation, updater/release/signing or GitHub Actions. Quantity-description 3D-locate and wall-quantity viewport-locate work remain independent.

## Qualification

The compact source gate `scripts/preflight-right-panel-compact-interactions.py` now composes with the canonical `scripts/preflight-right-panel-layer-search.py` ownership model. Together they check:

1. the current RightPanel XAML remains well-formed and keeps all important real action bindings;
2. `OnRightPanelPreviewKeyDown` has exactly one implementation across all `RightPanel*.cs` partials and that implementation belongs to `RightPanel.SearchShortcuts.cs`;
3. Ctrl+F/F5/Esc keep the canonical keyboard behavior without a second registration or duplicate member;
4. the compact shell keeps density/minimum-size/section hierarchy behavior;
5. the presentation partial does not gain mutation/CAD-command dependencies.

Native BricsCAD V25 rendering, dock/undock behavior, physical keyboard focus and DPI behavior still require the repository's existing local WPF/palette qualification. This source lane does **not** claim a licensed BricsCAD runtime PASS.
