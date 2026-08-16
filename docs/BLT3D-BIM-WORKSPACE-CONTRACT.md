# BLT3D MÔ HÌNH BIM workspace contract

This document is the implementation contract for the QS3D BricsCAD V25 **MÔ HÌNH BIM** experience. It translates the owner-provided BLT3D reference into production UI behavior; it is not a screenshot mock.

## 1. Topbar contract

QS3D-owned tabs appear as one contiguous group in this order:

1. KHỞI ĐẦU
2. THIẾT LẬP DỰ ÁN
3. MÔ HÌNH BIM
4. NHẬN DẠNG
5. VẼ
6. TOOL
7. MODELING
8. XEM
9. ĐỊNH LƯỢNG
10. BẢN SỬA ĐỔI

Legacy QS3D-owned tabs outside this contract, especially `QS3D_AUTHOR / TẠO MỚI`, are retired. Native and third-party BricsCAD tabs are not removed.

## 2. MÔ HÌNH BIM ribbon contract

The BIM tab exposes the qualified BLT3D surface in three panels, in order:

- **Vẽ** — Point, Arc, Line, Rectangle, CAD trace/polyline, Circle.
- **Công cụ** — Boundary, slab slope, slab opening/cut, Move, Rotate, Mirror, Copy, Break, Join, Distance, corner join and T-join.
- **IFC** — Import IFC, lightweight Import IFC, delete selected IFC entities and IFC export.

The BIM tab reuses existing QS3D/BricsCAD command routing, handlers and icons. It must not duplicate geometry or business logic.

## 3. Full BIM workspace contract

Entering `QS3D_BIM` activates the full workspace once per tab transition:

- **Left dock — Mô hình BIM**: working Zone and Floor, model category tree, Family/Type search/list, Add, Delete, guarded automatic import from current CAD selection, and editable properties.
- **Center — BricsCAD native viewport**: the real drawing/model viewport remains the preview and interaction surface. PAN, ZOOM, ORBIT, PICK, native visual styles and BricsCAD selection remain authoritative. No duplicate fake 3D renderer is introduced.
- **Right dock — Quản lý bản vẽ & lớp**: existing Xref/drawing and layer management remains fully functional, with BLT3D-facing labels.
- **Footer/status**: model/BQ/inspection modes, active floor/elevation and live semantic/viewport status remain available.

The user may resize palettes. The BIM entry contract reasserts left/right dock sides so the native viewport stays centered.

## 4. Left model/category presentation

The visible owner-reference category tail includes:

- Đào đắp
- Kết cấu thép
- Cấu kiện khác

QS3D does not currently expose a dedicated steel semantic category/builder. Therefore **Kết cấu thép** is a presentation-compatible grouping backed by `ElementCategory.CustomQuantity` until a real steel domain implementation exists. This explicitly avoids pretending that generic geometry is native steel BIM data.

## 5. Family actions

The owner-reference labels are:

- `+ Add`
- `Delete`
- `⚡ Nhập tự động`

`Nhập tự động` deliberately reuses the existing guarded capture-from-current-selection behavior. It must not silently scan or mutate an unbounded whole drawing.

## 6. Right manager presentation

The existing production handlers remain unchanged while labels align to BLT3D:

- **Quản lý bản vẽ** — Thêm, Nạp, Di chuyển, Xóa, Khoanh vùng and existing supported drawing actions.
- **Quản lý lớp** — search/filter plus Hiện, Ẩn, Đảo, Bỏ chọn.

Deleting/removing drawings continues to use the existing guarded Xref/drawing logic; the visual label does not change command safety semantics.

## 7. Activation and lifecycle

`BltBimWorkspaceActivationCoordinator` observes the active Ribbon tab on the UI idle dispatcher and opens the BIM shell on a transition into `QS3D_BIM`. It does not force palettes open on every timer tick, so a user who manually closes a palette while staying on BIM is respected. Ribbon teardown stops the coordinator.

## 8. Regression protection

`scripts/preflight-blt3d-bim-workspace.py` is auto-discovered by `scripts/preflight-all.py`. It locks the source-level contracts for:

- dual left/right BIM palette visibility and docking;
- owner-reference Family/category/footer labels;
- integration with the real Family workspace controls;
- drawing/layer manager labels;
- BIM-tab activation lifecycle;
- Vẽ / Công cụ / IFC ribbon ordering;
- ten-tab QS3D topbar contract.

Any future change that breaks these contracts must update both the implementation and this document intentionally.
