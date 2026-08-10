# QS3D Premium UI/UX Progress

**Updated:** 2026-08-10 (UTC+7)  
**Scope:** BricsCAD V25 hosted WPF palettes and modeless windows.

## Current direction

QS3D uses a restrained CAD-first dark system: neutral surfaces, high-contrast primary text, blue action/focus accent, semantic success/warning/danger colors and a rare warm luxury accent. The BricsCAD viewport remains dominant; the plugin must not introduce glow, blur, acrylic, animated gradients or other effects that reduce engineering readability or palette performance.

The root contrast regression from the reference screenshot is already guarded in `Theme.xaml`: keyed `PanelTitle` explicitly uses `TextBrush`, so labels such as `ĐỐI TƯỢNG ĐANG CHỌN` do not inherit a black host foreground.

## Completed in the current continuation batch

### P1 — Workspace palette

- Premium three-pane hierarchy without changing existing command handlers or semantic bindings.
- Compact top command bar with QS3D/V25/BIM identity, centered status and stable 3D/zoom/save controls.
- Work-scope card for Zone/Floor and clearer model-tree hierarchy.
- Family/Type count badge, separated create/delete/capture/build actions, dedicated wall-review toolbar band and labeled search.
- Property inspector now separates selected Family context from edit scope more clearly; grouped properties receive an elevated section surface while CAD/source read-only behavior remains controlled by the existing view model.
- HT_Phòng receives a compact selected-count badge and stable action hierarchy.
- Selected-object inspector keeps `Focus`, isolate/unisolate, locate and top-view workflows while adding a clear `CAD + SEMANTIC` identity marker.
- Persistent bottom navigation remains tied to the native BricsCAD viewport.
- Layout structure intentionally remains compatible with `WorkspacePanel.LayoutPersistence.cs`: root row 1 still exposes the 5-column main grid; Family remains column 2 and selected/room remains column 4 with split rows.

### P2 — Drawing / Xref / Layer palette

- Drawing and layer sections now have consistent title/subtitle/action hierarchy.
- Live counts for drawings and layers are visible without replacing current collections or handlers.
- Xref destructive detach remains visually separated from normal actions.
- Layer search is labeled and the multi-select action band groups show/hide/lock/unlock/invert/clear operations.
- Native layer color and lock state remain live (`ColorBrush`, `IsLocked`); no fake accent swatch was reintroduced.
- Long layer names remain ellipsized with tooltip.
- Status footer gets a compact persistent state indicator.

### P3 — Modeless-window consistency, first production pass

- `DomainHubWindow` is converted from a flat command wall into two-column workflow cards.
- Direct Draw, wall/opening, structural, project/template, recognition, rebar/quantity, section/revision and release groups keep their existing command tags and `OnCommandClick` routing.
- `FamilyManagerWindow` now uses catalog/detail cards, explicit active-family context, clearer destructive actions and a consistent status footer.
- `DoorOpeningScheduleWindow` now uses KPI cards, labeled search, card-contained read-only DataGrid and an explicit export-oriented footer.
- Legacy hardcoded `#17191C` window backgrounds were removed from these upgraded windows so the shared theme remains the source of truth.

### P4 — Interaction and feedback foundation

- Existing shared theme focus/hover/pressed/disabled states remain in force.
- Primary and destructive actions are visually separated in the upgraded surfaces.
- Status footers are persistent instead of transient decorative banners.
- No expensive visual effects or animation were added.

## Regression guard

`scripts/preflight-ui-premium-layout.py` is auto-discovered by `scripts/preflight-all.py` and validates:

- XAML well-formedness for the upgraded Workspace, RightPanel, Domain Hub, Family Manager and Door/Opening Schedule;
- presence of the premium layout primitives;
- preservation of important workflow handlers and command tags;
- live layer color/lock contracts;
- absence of legacy hardcoded dark-window backgrounds on upgraded modeless windows;
- absence of explicit black foregrounds in dark hosted palettes;
- required premium shared theme tokens, including the explicit `PanelTitle` foreground guard.

## Next continuation targets

Continue P3/P4 using the same visual contract, in this order:

1. Project Tools and Schedule Hub.
2. Quantity/BQ and BBS/Rebar review windows.
3. Curtain Wall and mesh/setup editors.
4. Floor/Zone/Material editors.
5. Recognition/Revision/Health/Release/Audit windows.
6. Empty/loading/error/disabled feedback audit across all modeless windows.
7. Keyboard-focus and narrow-width review for each upgraded window.

Do not change command semantics merely to make a screen look more polished. UI changes must stay source-safe and keep BricsCAD/CAD transactions isolated from post-commit UI failures.

## Runtime qualification boundary

This batch is source/static UI work. It does **not** claim licensed BricsCAD V25 NETLOAD/runtime visual qualification.

Before calling the premium UI production-qualified, validate the exact release SHA on real BricsCAD V25 x64 at 100%, 125%, 150% and 200% DPI, including narrow/normal/wide palette widths, Vietnamese labels, ComboBox popups, disabled/read-only controls, selection/focus states and representative private DWG workflows.

GitHub Actions should remain owner-controlled/manual-only; do not dispatch CI solely for cosmetic UI iteration unless explicitly requested.
