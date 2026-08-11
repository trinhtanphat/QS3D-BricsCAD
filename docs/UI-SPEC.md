# QS3D UI spec — BLT3D-familiar clean-room workflow

The objective is workflow familiarity and modern CAD ergonomics, not copying proprietary BLT assets.

## Plugin hosting boundary

This UI spec describes interfaces **hosted by the QS3D BricsCAD V25 plugin**. BricsCAD's native viewport remains the CAD canvas; QS3D adds Ribbon content, palettes and modeless WPF windows from inside the BricsCAD process. There is **no separate QS3D desktop shell, standalone EXE or QS3D-owned CAD viewport** in the current product target.

`BLT3D-familiar` and `BLT-style` below mean workflow/UX familiarity only and do not imply that QS3D copies or matches BLT's packaging/application form. See `docs/PRODUCT-BOUNDARY.md`.

## Final layout target

- BricsCAD native viewport remains the center renderer.
- QS3D Ribbon tabs: `KHỞI ĐẦU`, `THIẾT LẬP DỰ ÁN`, `MÔ HÌNH BIM`, `NHẬN DẠNG`, `VẼ`, `TOOL`, `MODELING`, `XEM`, `ĐỊNH LƯỢNG`, `BẢN SỬA ĐỔI`.
- main palette: active Zone/Floor → semantic tree → Family list/search/Add/Delete/**Bóc chọn**/Vẽ 3D + wall/host helpers → typed Family/Instance properties → selected CAD review.
- right palette: live Drawing/Xref list and live Layer search/show/hide/multi-select.
- HT_Phòng has explicit create/untrack/select-room actions without hiding source CAD deletion behind a destructive label.
- Full Domain Hub provides a larger command-oriented entry point for Tường KT/Cửa/Curtain, Room/Structure, recognition, generated rebar/BQ, revision, project/template and health/runtime checks.
- Curtain Wall Hub provides a focused GlassWall Family/grid/frame workflow rather than forcing curtain-specific controls into the generic property pane.
- Geometry Extensions groups wall topology/snap, straight/curved opening booleans and generated-rebar build/health workflows that would otherwise overcrowd the main Workspace.
- BQ: modeless quantity window with floor/category/search filters, column visibility, semantic Locate and Excel.
- Model Health / Full Health: modeless issue grid, counts and Locate routed to the correct source/generated handle family.

## BLT-familiar interaction rules

1. The semantic tree is the primary category navigator. Tường KT exposes Tường Gạch, Vách Kính and Trụ Tường; Cửa exposes Lỗ Mở Vách and Cửa Đi; HT_Phòng exposes its finish children.
2. A user selects a category/Family and CAD entities, then presses **Bóc chọn**. The palette dispatches the correct category command instead of requiring command-line knowledge.
3. The property pane explicitly separates **Family / Type** and **Đối tượng / Instance**. Exactly one semantic selection may switch to Instance scope; ambiguous/multi-element matches must not silently edit an instance.
4. Family edits propagate only to values that still inherit the previous Family value. A true instance override is preserved. Instance scope exposes a reset action to return a property to the current Family value.
5. Common booleans use checkbox editors; mode/material/classification-like values use editable choices; Floor/Level uses the semantic project hierarchy rather than a free-form typo-prone field; other values use text/numeric editors. Numeric properties reject non-finite/non-numeric edits.
6. Selection inspection and semantic reference handles keep palette selection synchronized with native CAD, including Auto Room boundary provenance.
7. Review actions should be available where the user is already working: Focus, Cô lập/Khôi phục, Locate/Zoom and view controls.
8. Tường KT helper actions are explicit: **Giao tường** analyzes L/T/X topology; **Snap xem** previews endpoint cleanup; **Snap áp** applies only a still-valid reviewed plan.
9. Door/Opening automatic host matching is explicit **Auto Host**. It may link a compatible host but must not silently perform the physical boolean cut.
10. Native 3D or boolean operations must fail closed with a clear unsupported-source message rather than inventing geometry.
11. Vách Kính keeps semantic/quantity controls separate from native host/frame state: the Curtain Hub may build one backing host plus dedicated frame overlays, but it must not imply that Door/Opening cuts already trim mullions/transoms when they do not.
12. Generated rebar UI must expose dedicated slab/wall mesh workflows without reusing labels that suggest generic longitudinal bars; unified health should be the easiest route for a user who does not know which generated family is stale.
13. Ribbon, Workspace, Full Domain Hub and focused hubs/extensions should expose the same **major** workflows while keeping the main Workspace compact rather than duplicating every expert command everywhere.
14. Workspace keyboard/context actions must reuse the same guarded handlers as visible buttons. They must not create a second raw `SendStringToExecute` path with different selection or mutation semantics.
15. Workspace/native-build compatibility must come from one shared category capability so UI messaging cannot drift from `QS3DBUILD3D` support.

## Workspace fast interaction contract

The main Workspace currently exposes these source-level shortcuts:

- `Ctrl+S` → existing QS3D Save handler;
- `Ctrl+F` → focus/select-all in Family search;
- `Ctrl+B` → existing BQ handler;
- `F5` → existing Workspace/CAD refresh handler;
- `Delete` → delete the selected Family **only while keyboard focus is inside the Family list**.

Right-click on a Family row selects that row first, then exposes: **Nhân bản Family**, **Xóa Family**, **Bóc đối tượng CAD đang chọn**, **Vẽ / Cập nhật 3D**.

Right-click in selected-object review exposes the existing **Focus**, **Cô lập**, **Khôi phục cô lập**, **Định vị / Zoom chọn** and **Mặt bằng** handlers.

These context/keyboard surfaces are workflow accelerators only. Existing validation, active-DWG state, Family/category selection, CAD selection restoration and command handlers remain authoritative.

For **Vẽ / Cập nhật 3D**, Workspace reads `NativeBuildCapability` before command dispatch. An unsupported category is reported in the Workspace status bar and is not sent to `QS3DBUILD3D`; the command still keeps its deeper source-type, Model-Space and transaction guards.

## Visual language

- compact Segoe UI CAD density;
- neutral dark surfaces with blue accent, visible separators and red destructive actions;
- short button labels in narrow palettes, with tooltips carrying the longer explanation;
- `WrapPanel` or responsive wrapping where action rows could otherwise clip;
- vector/native icons are preferred; Unicode placeholder icons such as the current reset glyph are acceptable only during source-development, not as the final commercial icon set;
- list/table virtualization where possible;
- explicit keyboard-focus states;
- `UseLayoutRounding`, device-pixel snapping and display text formatting for HiDPI source hardening;
- explicit empty/error/status states instead of fake sample data;
- generated/runtime-gated actions should communicate unsupported source or stale/rebuild requirements instead of silently no-oping.

## Per-user layout persistence

Palette/splitter layout is a user preference, not BIM/project data.

- QS3D stores the layout under the current user's `LocalApplicationData/QS3D/BricsCAD-V25/ui-layout-v1.txt` path; it does not write widths/heights into `.qsdb` or `ProjectState.Metadata`.
- Workspace and right-palette dimensions use BricsCAD `DeviceIndependentSize` rather than the obsolete `PaletteSet.Size` property.
- Workspace restores model-tree width, Family/property width, Family-list height and HT_Phòng top-panel height.
- Splitter state is written only on `GridSplitter.DragCompleted`, not every layout/size event.
- malformed/non-finite/out-of-range values are clamped/fallback safely; preference persistence is best-effort and cannot block plugin teardown.
- writes use a same-directory temporary file and replacement/fallback path so a failed preference write does not corrupt project data.

## Implemented source state — 2026-08-10

Implemented in source:

- three-pane main QS3D workspace plus separate Drawing/Layer palette;
- semantic model tree and Family filtering/selection synchronization;
- BLT-style **Bóc chọn** category capture action;
- grouped Vietnamese property inspector with typed text/boolean/choice controls and semantic Floor/Level selection;
- **Family / Type** vs **Đối tượng / Instance** scope, instance override preservation and reset-to-Family action;
- semantic-reference selection matching with ambiguous-instance protection;
- live selected-CAD review with Locate/Zoom, Focus, Cô lập and Khôi phục;
- Workspace Family/inspection right-click actions and `Ctrl+S`, `Ctrl+F`, `Ctrl+B`, `F5`, focus-scoped `Delete` shortcuts routed through existing handlers;
- shared `NativeBuildCapability` used by Workspace and `QS3DBUILD3D`, with UI pre-check messaging for unsupported Vẽ 3D categories;
- per-user palette and internal splitter layout persistence outside `.qsdb`;
- workspace wall helper actions: Giao tường, Snap xem, Snap áp;
- workspace Auto Host action for Door/Opening host matching without automatic physical cut;
- HT_Phòng actions;
- defensive native Ribbon bootstrapper, Full Domain Hub, Curtain Wall Hub and Geometry Extensions;
- Ribbon/Hub exposure for Tường KT, **Vách Kính Hub / Curtain 3D / frame health**, wall snap, Auto/Manual Host, straight/curved Door/Opening cuts, review commands and Full Health;
- generated-rebar UI for column/beam longitudinal bars, BBS shape, beam stirrups, column ties, **Slab X/Y mesh** and **StructuralWall horizontal/vertical mesh**, including dedicated health commands and `QS3DREBARHEALTHALL`;
- Curtain Hub Family controls for panel width/height, perimeter/mullion/transom widths, frame material and native frame depth; LINE and guarded open/bulged WCS-XY GlassWall paths keep a backing host and build dedicated frame overlays without replacing opening-host ownership;
- live right-palette Layer color/lock state from the actual DWG rather than decorative sample state;
- premium dark theme source guards for focus, HiDPI layout rounding and recycling/row/column virtualization;
- BQ, BBS, recognition, revision, template, audit, Model Health and Full Health windows/workflows;
- static BLT workspace, Workspace-interaction, native-build capability, per-user layout and HiDPI preflights verify key source contracts, while dedicated curtain/mesh/health preflights protect focused workflows.

## Remaining UI/product parity work

Source parity is not the same as visual/runtime parity. Remaining work after licensed V25 screenshots/testing should prioritize:

- real V25 Ribbon grouping, commercial icon set and spacing instead of the current dense text-first buttons;
- specialized context menus for wall/door/curtain/rebar expert actions where they materially reduce clicks; do not duplicate the generic Workspace actions already implemented;
- section-box and deeper transient review workflows proven against V25;
- richer material/catalog/classification pickers and searchable project-level catalogs beyond the current semantic Floor/Level selector and editable choices;
- compatibility/disabled-state messaging for Curtain 3D, Snap áp and straight/curved Khoét Cửa/Lỗ when the UI has enough live source data to do so without guessing;
- curtain opening-aware frame visualization so users do not mistake backing-host cuts for completed mullion/transom interruption;
- large-list performance proof on representative projects even though selection sync is debounced and source virtualization is enabled;
- accessibility/focus order and high-DPI clipping fixes from real screenshots.

## Visual acceptance gate

A design render is only a target. Before release, screenshots must come from a compiled V25 **plugin** at 100%, 125%, 150% and 200% scaling and be compared against the approved layout for panel widths, wrapping/clipping, Family/Instance/Floor-Level scope, typed controls, context menus/shortcuts, per-user splitter restoration, Curtain Hub/Geometry Extensions, selected/hover/disabled/error state, Vietnamese Unicode, command discoverability and native viewport preservation.
