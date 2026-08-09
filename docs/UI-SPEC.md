# QS3D UI spec — BLT3D-inspired, clean-room implementation

The goal is workflow familiarity, not pixel-copying proprietary assets.

## Layout
- BricsCAD native ribbon remains top-level; the QS3D ribbon is intentionally deferred until the first V25 adapter build passes.
- Left docked palette: zone/floor + semantic tree + family list/properties + current selection detail.
- Center: native BricsCAD viewport, never a custom renderer.
- Right docked palette: drawing/Xref manager + layer manager. Buttons are intentionally disabled until the first V25 runtime gate so the UI never pretends mock data is live CAD state.
- BQ: modeless quantity summary window with floor/category/search filters, column visibility toggles and Excel export.

## Visual language
- Dark neutral background, compact Segoe UI, blue selected state, red destructive action.
- Splitters are visible but narrow.
- Vietnamese labels match the project terminology: `HT_Phòng`, `Tường KT`, `Lỗ Mở Vách`, `Bảng Tổng Hợp Khối Lượng`.

## Current implementation status
- Docked left/right palette source: implemented.
- Selection inspection source: implemented.
- BQ floor/category/search filtering: implemented.
- BQ column show/hide controls: implemented.
- Real `.xlsx` export engine: implemented.
- Native QS3D ribbon: pending first successful V25 compile/runtime gate.
- Live Xref/layer manager: pending first successful V25 compile/runtime gate.
- Family Add/Delete/Vẽ 3D CAD transactions: pending runtime-gated implementation.

A rendered design preview is supplied separately during review; it is explicitly a mock/design preview, not a screenshot of a compiled BricsCAD plugin.
