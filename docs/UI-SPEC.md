# QS3D UI spec — BLT3D-familiar clean-room workflow

The objective is workflow familiarity and modern CAD ergonomics, not copying proprietary BLT assets.

## Final layout target

- BricsCAD native viewport remains the center renderer.
- QS3D Ribbon tabs: `KHỞI ĐẦU`, `THIẾT LẬP DỰ ÁN`, `MÔ HÌNH BIM`, `NHẬN DẠNG`, `VẼ`, `TOOL`, `MODELING`, `XEM`, `ĐỊNH LƯỢNG`, `BẢN SỬA ĐỔI`.
- left palette: active Zone/Floor → semantic tree → Family list/search/Add/Delete/Vẽ 3D → data-driven properties → selected CAD objects.
- right palette: live drawing/Xref list and live layer search/show/hide/multi-select.
- BQ: modeless quantity window with floor/category/search filters, column visibility, semantic Locate and Excel.
- Model Health: modeless issue grid, counts and Locate.

## Visual language

- compact Segoe UI CAD density;
- neutral dark surfaces with blue accent, visible 1px separators and red destructive actions;
- vector/native icons are preferred; Unicode placeholder icons are not part of the final commercial icon set;
- list/table virtualization where possible;
- explicit empty/error/status states instead of fake sample data.

## Implemented source state

The palettes, design tokens, semantic property flow, live Layer/Xref data path, BQ and Health windows are implemented in source. Native Ribbon is implemented through a defensive runtime bootstrapper so a ribbon API mismatch does not prevent palette use.

## Visual acceptance gate

A design render is only a target. Before release, screenshots must come from a compiled V25 plugin at 100%, 125%, 150% and 200% scaling and be compared against the approved layout for panel widths, clipping, selected/hover/error state, Vietnamese Unicode and native viewport preservation.
