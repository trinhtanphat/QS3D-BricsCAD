# QS3D UI spec — BLT3D-inspired, clean-room implementation

The goal is workflow familiarity, not pixel-copying proprietary assets.

## Layout
- BricsCAD native ribbon remains top-level; future QS3D ribbon tabs mirror the requested work stages.
- Left docked palette: zone/floor + semantic tree + family list/properties + current selection detail.
- Center: native BricsCAD viewport, never a custom renderer.
- Right docked palette: drawing/Xref manager + layer manager.
- BQ: modeless quantity summary window with category filters, grouping, columns and Excel export.

## Visual language
- Dark neutral background, compact 12px Segoe UI, blue selected state, red destructive action.
- Splitters are visible but narrow.
- Vietnamese labels match the project terminology: `HT_Phòng`, `Tường KT`, `Lỗ Mở Vách`, `Bảng Tổng Hợp Khối Lượng`.

See `docs/ui-preview.png` for the current design target.
