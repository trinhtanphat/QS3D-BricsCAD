# QS3D Premium UI/UX Progress

**Updated:** 2026-08-12 (UTC+7)
**Scope:** BricsCAD V25 hosted WPF palettes and modeless windows.

## Current direction

QS3D uses a restrained CAD-first dark system: neutral surfaces, high-contrast primary text, blue action/focus accent, semantic success/warning/danger colors and a rare warm luxury accent. The BricsCAD viewport remains dominant; the plugin must not introduce glow, blur, acrylic, animated gradients or other effects that reduce engineering readability or palette performance.

The root contrast regression from the reference screenshot is guarded in `Theme.xaml`: keyed `PanelTitle` explicitly uses `TextBrush`, so labels such as `ĐỐI TƯỢNG ĐANG CHỌN` do not inherit a black host foreground.

## Completed

### P0 — Theme / contrast foundation

- Premium neutral dark token hierarchy for canvas, panel, elevated, hover, selected and pressed surfaces.
- Explicit primary, secondary and disabled text hierarchy.
- Blue action/focus accent plus success/warning/danger semantics and a restrained luxury accent.
- Shared focus/hover/pressed/disabled behavior remains lightweight and HiDPI-friendly.
- `PanelTitle.Foreground` is explicitly locked to `TextBrush` to prevent BricsCAD host foreground leakage.
- `preflight-wpf-theme.py` guards theme resource correctness and the panel-title contrast contract.

### P1 — Workspace palette

- Premium three-pane hierarchy without changing existing command handlers or semantic bindings.
- Compact top command bar with QS3D/V25/BIM identity, centered status and stable 3D/zoom/save controls.
- Work-scope card for Zone/Floor and clearer model-tree hierarchy.
- Family/Type count badge, separated create/delete/capture/build actions, dedicated wall-review toolbar band and labeled search.
- Property inspector separates selected Family context from edit scope more clearly; grouped properties use elevated section surfaces while CAD/source read-only behavior remains controlled by the existing view model.
- HT_Phòng has a compact selected-count badge and stable action hierarchy.
- Selected-object inspector keeps `Focus`, isolate/unisolate, locate and top-view workflows while adding a clear `CAD + SEMANTIC` marker.
- Persistent bottom navigation remains tied to the native BricsCAD viewport.
- Layout structure remains compatible with `WorkspacePanel.LayoutPersistence.cs`: root row 1 still exposes the 5-column main grid; Family remains column 2 and selected/room remains column 4 with split rows.
- Compact host sizing is separate from content sizing: BricsCAD may dock/restore the Workspace at `460 x 420`, while the three-column surface retains its `560`-DIP design width behind an explicit horizontal overflow viewport. The vertical axis stays constrained by the host so TreeView/ListView virtualization and native scrolling are preserved.
- Property editing commits on Enter as well as the existing focus-loss path, with a static regression guard.
- Palette minimum sizes are centralized and enforced from layout policy, avoiding drift between persisted layout and host palette constraints.
- Responsive compact-header breakpoints collapse only decorative badges/shorten display labels when space is tight, preserving handlers/tooltips and keeping the status column ellipsis-safe.
- The `MÔ HÌNH` section reserves independent space for its title/caption and `Làm mới` action so narrow docking does not cause overlap.

### P2 — Drawing / Xref / Layer palette

- Drawing and layer sections use consistent title/subtitle/action hierarchy.
- Live counts for drawings and layers are visible without replacing current collections or handlers.
- Xref destructive detach remains visually separated from normal actions.
- Layer search is labeled and the multi-select action band groups show/hide/lock/unlock/invert/clear operations.
- Native layer color and lock state remain live (`ColorBrush`, `IsLocked`); no fake accent swatch was reintroduced.
- Long layer names remain ellipsized with tooltip.
- Status footer has a compact persistent state indicator.
- Layer filtering operates on the cached view instead of rebuilding layer rows for every search update, with regression coverage preventing allocation-heavy refresh behavior.
- Search feedback reports filtered and total layer counts so the result scope is visible without additional dialogs.
- Empty-document and vanished-Xref refresh paths clear stale visual selection/state rather than leaving a misleading prior-DWG context.
- Luxury hierarchy uses shared `PremiumCard` / `StatusPill` primitives and restrained champagne section markers while preserving all current Xref/layer handlers, context menus, keyboard routing and the `Tỉ lệ` / `ScaleText` surface.

### P3 — Modeless-window consistency, source pass complete

All modeless WPF windows currently present under `src/QS3D.BricsCAD.V25/UI` consume the shared premium theme and follow the same CAD-first hierarchy. Existing command handlers, x:Names and command tags are preserved.

Upgraded surfaces include:

- `DomainHubWindow` — workflow cards for Direct Draw, wall/opening, structural, project/template, recognition, rebar/quantity, section/revision and release workflows.
- `FamilyManagerWindow` — catalog/detail cards, active-family context, explicit destructive actions and semantic assignment flow.
- `DoorOpeningScheduleWindow` — KPI cards, labeled search, read-only schedule card and export footer.
- `ProjectToolsWindow` — project snapshot metrics plus project data, module and maintenance control cards.
- `ScheduleHubWindow` — schedule-safe snapshot metrics and grouped BQ/finish/material/curtain/opening/rebar workflows.
- `QuantitySummaryWindow` — BQ review workspace with floor/search filters, category list, quantity grid, column visibility inspector and locate/recalculate/export actions.
- `Rebar3DHubWindow` — grouped column/beam, slab/wall/foundation, BBS and fail-closed health workflows.
- `RebarScheduleWindow` — premium BBS review grid with locate/export flow.
- `RebarMeshSetupWindow` — explicit-input form cards, validation surface and no-design-inference footer.
- `CurtainWallWindow` — Family/host inputs, panel/frame grid, live schedule metrics and gated native geometry workflow.
- `FloorLevelWindow` — level catalog, edit form, active/reference metrics and semantic assignment workflow.
- `ZoneManagerWindow` — zone catalog, edit form, active/reference metrics and semantic scope assignment.
- `MaterialCatalogWindow` — project catalog, custom material form, usage context and fail-closed assignment semantics.
- `RecognitionWindow` — review-gated recognition table and confidence-sensitive apply actions.
- `RevisionWindow` — premium read-only quantity/semantic review with CAD locate flow.
- `ModelHealthWindow` — premium issue-review table with CAD locate flow.
- `AuditLogWindow` — searchable premium audit trail using the same table/status language.
- `RoomFinishScheduleWindow` — search + KPI cards + finish schedule + XLSX export flow.
- `GeometryExtensionsWindow` — review-gated wall topology, opening boolean, rebar 3D and rebar-health cards.

Legacy hardcoded `#17191C` modeless-window backgrounds are removed from the premium source pass so `Theme.xaml` remains the visual source of truth.

### P4 — Interaction / feedback foundation

- Existing shared theme focus, hover, pressed and disabled states remain in force.
- Primary, secondary and destructive actions are visually distinct.
- Review-gated/fail-closed workflows use explicit warning or status language instead of decorative effects.
- Modeless windows use persistent status/footer regions where the workflow benefits from state visibility.
- Long engineering tables stay DataGrid-based and virtualization remains owned by the shared theme.
- No blur, acrylic, animated gradients, large shadow effects or continuous animation were introduced.

## 2026-08-12 reconciliation audit

The original screenshot defect and the broad source-side premium pass are no longer open remote work. The exact heading from the report is still rendered through `Style="{StaticResource PanelTitle}"` in `WorkspacePanel.xaml`, while `Theme.xaml` explicitly sets `PanelTitle.Foreground` to `TextBrush`; the source contract therefore remains fail-safe against the host/system black foreground leak.

The later screenshot-visible UI refinement is also on `main`: responsive Workspace header work, premium diagnostics/revision surfaces and the luxury Right Panel hierarchy are integrated alongside the earlier broad modeless-window pass. The canonical premium plan has been reconciled so it no longer describes the completed Workspace and Right Panel UI lanes as active neighboring reservations.

At this checkpoint there is no additional remote-only cosmetic rewrite required merely to satisfy the original premium/professional/luxury request. Future remote work should be evidence-driven correctness/performance hardening or a new explicit owner UI requirement, not another speculative full visual rewrite.

## Regression guard

`scripts/preflight-ui-premium-layout.py` is auto-discovered by `scripts/preflight-all.py` and validates the broad V25 modeless-window premium source contract. Focused theme/Workspace/RightPanel/diagnostics/revision guards add narrower protection for their presentation and behavior boundaries.

The source/static guard set covers, among other things:

- XAML/XML well-formedness for premium core palettes/windows;
- `Theme.xaml` adoption across modeless windows;
- absence of legacy `Background="#17191C"` and explicit black foregrounds in dark-host surfaces;
- premium layout primitives and status/badge semantics;
- preservation of critical workflow handlers and command tags;
- live layer/Xref state contracts in `RightPanel`;
- required premium shared theme tokens, including explicit `PanelTitle` foreground ownership;
- no heavy WPF effects or business/CAD logic leaking into presentation-only XAML.

## Remaining qualification work

The design-system source pass is broad, but production visual qualification is not complete until these real-host checks pass:

1. BricsCAD V25 x64 at 100%, 125%, 150% and 200% DPI.
2. Workspace and RightPanel at narrow, normal and wide docked widths.
3. Vietnamese labels/diacritics and long project/family/layer names.
4. ComboBox popup foreground/background under the real BricsCAD host.
5. Keyboard focus across buttons, tree/list/table, text inputs and editable combos.
6. Disabled/read-only/selected/hover/warning/error states.
7. Representative private DWG flows for Family/Instance editing, Direct Draw, selection/focus/isolate, Xref/layer, BQ/Schedule and native geometry review.
8. Before/after screenshots from the exact release SHA.

These runtime items are already represented by the canonical local queue and remain `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until a compatible local agent has licensed BricsCAD V25 and the required real-host environment.

Do not change CAD transaction semantics merely to make a screen look more polished. UI synchronization must remain isolated from valid post-commit CAD work.

## Runtime qualification boundary

This is source/static UI work. It does **not** claim licensed BricsCAD V25 NETLOAD/runtime visual qualification.

GitHub Actions remain owner-controlled/manual-only; do not dispatch CI solely for cosmetic UI iteration unless explicitly requested.
