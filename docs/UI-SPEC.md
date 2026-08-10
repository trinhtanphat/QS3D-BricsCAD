# QS3D UI spec — BLT3D-familiar clean-room workflow

The objective is workflow familiarity and modern CAD ergonomics, not copying proprietary BLT assets.

## Final layout target

- BricsCAD native viewport remains the center renderer.
- QS3D Ribbon tabs: `KHỞI ĐẦU`, `THIẾT LẬP DỰ ÁN`, `MÔ HÌNH BIM`, `NHẬN DẠNG`, `VẼ`, `TOOL`, `MODELING`, `XEM`, `ĐỊNH LƯỢNG`, `BẢN SỬA ĐỔI`.
- main palette: active Zone/Floor → semantic tree → Family list/search/Add/Delete/**Bóc chọn**/Vẽ 3D + wall/host helpers → typed Family/Instance properties → selected CAD review.
- right palette: live Drawing/Xref list and live Layer search/show/hide/multi-select.
- HT_Phòng has explicit create/untrack/select-room actions without hiding source CAD deletion behind a destructive label.
- Full Domain Hub provides a larger command-oriented entry point for Tường KT/Cửa, Room/Structure, recognition, rebar/BQ, revision, project/template and runtime checks.
- BQ: modeless quantity window with floor/category/search filters, column visibility, semantic Locate and Excel.
- Model Health: modeless issue grid, counts and Locate.

## BLT-familiar interaction rules

1. The semantic tree is the primary category navigator. Tường KT exposes Tường Gạch, Vách Kính and Trụ Tường; Cửa exposes Lỗ Mở Vách and Cửa Đi; HT_Phòng exposes its finish children.
2. A user selects a category/Family and CAD entities, then presses **Bóc chọn**. The palette dispatches the correct category command instead of requiring command-line knowledge.
3. The property pane explicitly separates **Family / Type** and **Đối tượng / Instance**. Exactly one semantic selection may switch to Instance scope; ambiguous/multi-element matches must not silently edit an instance.
4. Family edits propagate only to values that still inherit the previous Family value. A true instance override is preserved. Instance scope exposes a reset action to return a property to the current Family value.
5. Common booleans use checkbox editors; mode/material/classification-like values use editable choices; other values use text/numeric editors. Numeric properties reject non-finite/non-numeric edits.
6. Selection inspection and semantic reference handles keep palette selection synchronized with native CAD, including Auto Room boundary provenance.
7. Review actions should be available where the user is already working: Focus, Cô lập/Khôi phục, Locate/Zoom and view controls.
8. Tường KT helper actions are explicit: **Giao tường** analyzes L/T/X topology; **Snap xem** previews endpoint cleanup; **Snap áp** applies only a still-valid reviewed plan.
9. Door/Opening automatic host matching is explicit **Auto Host**. It may link a compatible host but must not silently perform the physical boolean cut.
10. Native 3D or boolean operations must fail closed with a clear unsupported-source message rather than inventing geometry.
11. Ribbon, Workspace and Full Domain Hub should expose the same major workflows so discoverability does not depend on one UI surface.

## Visual language

- compact Segoe UI CAD density;
- neutral dark surfaces with blue accent, visible 1px separators and red destructive actions;
- short button labels in narrow palettes, with tooltips carrying the longer explanation;
- `WrapPanel` or responsive wrapping where action rows could otherwise clip;
- vector/native icons are preferred; Unicode placeholder icons such as the current reset glyph are acceptable only during source-development, not as the final commercial icon set;
- list/table virtualization where possible;
- explicit empty/error/status states instead of fake sample data.

## Implemented source state — 2026-08-10

Implemented in source:

- three-pane main QS3D workspace plus separate Drawing/Layer palette;
- semantic model tree and Family filtering/selection synchronization;
- BLT-style **Bóc chọn** category capture action;
- grouped Vietnamese property inspector with typed text/boolean/choice controls;
- **Family / Type** vs **Đối tượng / Instance** scope, instance override preservation and reset-to-Family action;
- semantic-reference selection matching with ambiguous-instance protection;
- live selected-CAD review with Locate/Zoom, Focus, Cô lập and Khôi phục;
- workspace wall helper actions: Giao tường, Snap xem, Snap áp;
- workspace Auto Host action for Door/Opening host matching without automatic physical cut;
- HT_Phòng actions;
- defensive native Ribbon bootstrapper and Full Domain Hub;
- Ribbon/Hub exposure for Tường KT, wall snap, Auto/Manual Host, Door/Opening physical cutting, review commands, column rebar and BBS-shape rebar;
- BQ, BBS, recognition, revision, template, audit and Model Health windows/workflows;
- static BLT workspace preflight verifies key XAML well-formedness and entry-point parity.

## Remaining UI/product parity work

Source parity is not the same as visual/runtime parity. Remaining work after licensed V25 screenshots/testing should prioritize:

- real V25 Ribbon grouping, commercial icon set and spacing instead of text-only buttons;
- context menus for Family/element/wall/door actions and faster keyboard workflows;
- section-box and deeper transient review workflows proven against V25;
- specialized material catalog/level picker/classification picker instead of generic editable choices;
- disabled-state/compatibility messaging for Vẽ 3D, Snap áp and Khoét Cửa/Lỗ before a user attempts an unsupported operation;
- persisted splitter/palette widths if V25 hosting allows it safely;
- large-list virtualization/performance proof;
- accessibility/focus order and high-DPI clipping fixes from real screenshots.

## Visual acceptance gate

A design render is only a target. Before release, screenshots must come from a compiled V25 plugin at 100%, 125%, 150% and 200% scaling and be compared against the approved layout for panel widths, wrapping/clipping, Family/Instance scope, typed controls, selected/hover/disabled/error state, Vietnamese Unicode, command discoverability and native viewport preservation.
