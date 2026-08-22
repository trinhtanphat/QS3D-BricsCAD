# QS3D UI spec — BLT3D-familiar clean-room workflow

The objective is workflow familiarity and modern CAD ergonomics, not copying proprietary BLT assets.

## Final layout target

- BricsCAD native viewport remains the center renderer.
- QS3D Ribbon tabs: `KHỞI ĐẦU`, `THIẾT LẬP DỰ ÁN`, `MÔ HÌNH BIM`, `NHẬN DẠNG`, `VẼ`, `TOOL`, `MODELING`, `XEM`, `ĐỊNH LƯỢNG`, `BẢN SỬA ĐỔI`.
- left/main palette: active Zone/Floor → semantic tree → Family list/search/Add/Delete/**Bóc chọn**/Vẽ 3D → grouped data-driven properties → selected CAD objects.
- right palette: live Drawing/Xref list and live Layer search/show/hide/multi-select.
- HT_Phòng has explicit create/untrack/select-room actions without hiding source CAD deletion behind a destructive label.
- Full Domain Hub provides a larger command-oriented entry point for Tường KT/Cửa, Room/Structure, recognition, rebar/BQ, revision, project/template and runtime checks.
- BQ: modeless quantity window with floor/category/search filters, column visibility, semantic Locate and Excel.
- Model Health: modeless issue grid, counts and Locate.

## BLT-familiar interaction rules

1. The semantic tree is the primary category navigator. Tường KT exposes Tường Gạch, Vách Kính and Trụ Tường; Cửa exposes Lỗ Mở Vách and Cửa Đi; HT_Phòng exposes its finish children.
2. A user selects a category/Family and CAD entities, then presses **Bóc chọn**. The palette dispatches the correct category command instead of requiring command-line knowledge.
3. Family properties use user-facing Vietnamese labels/groups such as HÌNH HỌC, VỊ TRÍ / CAO ĐỘ, CỐT THÉP and VẬT LIỆU / PHÂN LOẠI while preserving stable internal property keys in the domain model.
4. Numeric properties reject non-finite/non-numeric edits even when they do not display a unit suffix.
5. Selection inspection and semantic Locate/Zoom keep the palette and native CAD selection synchronized.
6. Native 3D operations must show a clear unsupported-source message rather than fake geometry.
7. Physical Door/Opening cutting is explicit (`Khoét Cửa/Lỗ`) after semantic capture + host linking; it is not silently performed during capture.
8. Ribbon and Full Domain Hub should expose the same major product workflows so discoverability does not depend on one UI surface.

## Visual language

- compact Segoe UI CAD density;
- neutral dark surfaces with blue accent, visible 1px separators and red destructive actions;
- short button labels in narrow palettes, with tooltips carrying the longer explanation;
- `WrapPanel` or responsive wrapping where action rows could otherwise clip;
- vector/native icons are preferred; Unicode placeholder icons are not part of the final commercial icon set;
- list/table virtualization where possible;
- explicit empty/error/status states instead of fake sample data.

## Implemented source state — 2026-08-10

Implemented in source:

- three-pane main QS3D workspace plus separate Drawing/Layer palette;
- semantic model tree and Family filtering/selection synchronization;
- BLT-style **Bóc chọn** category capture action;
- grouped Vietnamese property inspector with expanded unit handling and safer finite numeric validation;
- live selected-CAD review and Locate/Zoom flow;
- HT_Phòng actions;
- defensive native Ribbon bootstrapper;
- Full Domain Hub;
- Ribbon/Hub exposure for Tường Gạch, Vách Kính, Trụ Tường, Door/Opening host linking, physical cutting and column rebar 3D;
- BQ, BBS, recognition, revision, template, audit and Model Health windows/workflows.

## Remaining UI/product parity work

Source parity is not the same as visual/runtime parity. Remaining work after licensed V25 screenshots/testing should prioritize:

- real V25 Ribbon grouping, commercial icon set and spacing instead of text-only buttons;
- context menus for Family/element actions and faster keyboard workflows;
- transient highlight/isolate/restore/section-box behavior proven against V25;
- richer property editor types (checkbox/dropdown/material selector/level picker) instead of treating every editable value as text;
- disabled-state/compatibility messaging for Vẽ 3D when a category has no production native profile yet;
- persisted splitter/palette widths if V25 hosting allows it safely;
- large-list virtualization/performance proof;
- accessibility/focus order and high-DPI clipping fixes from real screenshots.

## Visual acceptance gate

A design render is only a target. Before release, screenshots must come from a compiled V25 plugin at 100%, 125%, 150% and 200% scaling and be compared against the approved layout for panel widths, wrapping/clipping, selected/hover/disabled/error state, Vietnamese Unicode, command discoverability and native viewport preservation.
