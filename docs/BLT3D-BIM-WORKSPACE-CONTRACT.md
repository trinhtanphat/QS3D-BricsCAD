# BLT3D BIM + MODELING workspace contract

This document is the implementation contract for the QS3D BricsCAD V25 **MÔ HÌNH BIM** and **MODELING** experiences. It translates owner-provided BLT3D references into production UI behavior; it is not a screenshot mock.

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

## 3. MODELING ribbon contract

The QS3D-owned `MODELING` tab matches the owner BLT3D reference as eight groups in this order:

1. **Vật liệu** — one large `Vật liệu` action.
2. **Kết cấu thép** — large `Mặt cắt thép` and `Tạo chi tiết` actions.
3. **Mặt phẳng** — one large `Mặt XY` action.
4. **Vẽ phác** — compact 3-row columns containing `Đường`, `Polyline`, `Chữ nhật`, `Tròn`, `Cung`.
5. **Chỉnh sửa** — compact 3-row columns containing `Nối polyline`, `Offset`, `Di chuyển`, `Sao chép`, `Theo phương Z`.
6. **Dựng 3D** — `Extrude`, `Sweep`, `Loft`.
7. **Cấu kiện** — `Gắn vào Family`.
8. **Cắt khối** — `Union`, `Subtract`, `Intersect`.

`BltModelingRibbonAugmenter` removes only QS3D-owned `QS3D_MODELING_*` panels, preserves native/third-party content, builds all replacement panels before mutating the live Ribbon, and rolls back to the prior QS3D panels if reconciliation fails.

The visible actions reuse native BricsCAD BIM/modeling commands where they are authoritative:

- `MATERIALS`, `BIMPROFILES`, `BIMCREATEDETAIL`, `UCS World`;
- `LINE`, `PLINE`, `RECTANG`, `CIRCLE`, `ARC`;
- `JOIN`, `OFFSET`, `MOVE`, `COPY`;
- `EXTRUDE`, `SWEEP`, `LOFT`;
- `UNION`, `SUBTRACT`, `INTERSECT`.

`Gắn vào Family` opens the existing `QS3DFAMILIES` workflow so Family/Type assignment stays in the production Family Manager rather than introducing a screenshot-only duplicate workflow. `Theo phương Z` intentionally routes through `MOVE`; its tooltip instructs the user to enter a displacement in `@0,0,<ΔZ>` form so the operation stays on the Z axis without inventing a second transform engine.

The icon artwork is generated as frozen WPF vector drawings in the plugin. This keeps the blue BLT3D visual language crisp at both standard and large Ribbon sizes without shipping bitmap captures from the reference screenshot.

## 4. Full BIM workspace contract

Entering `QS3D_BIM` activates the full workspace once per tab transition:

- **Left dock — Mô hình BIM**: working Zone and Floor, model category tree, Family/Type search/list, Add, Delete, guarded automatic import from current CAD selection, and editable properties.
- **Center — BricsCAD native viewport**: the real drawing/model viewport remains the preview and interaction surface. PAN, ZOOM, ORBIT, PICK, native visual styles and BricsCAD selection remain authoritative. No duplicate fake 3D renderer is introduced.
- **Right dock — Quản lý bản vẽ & lớp**: existing Xref/drawing and layer management remains fully functional, with BLT3D-facing labels.
- **Footer/status**: model/BQ/inspection modes, active floor/elevation and live semantic/viewport status remain available.

The user may resize palettes. The BIM entry contract reasserts left/right dock sides so the native viewport stays centered.

## 5. Left model/category presentation

The visible owner-reference category tail includes:

- Đào đắp
- Kết cấu thép
- Cấu kiện khác

QS3D does not currently expose a dedicated steel semantic category/builder. Therefore **Kết cấu thép** is a presentation-compatible grouping backed by `ElementCategory.CustomQuantity` until a real steel domain implementation exists. This explicitly avoids pretending that generic geometry is native steel BIM data.

## 6. Family actions

The owner-reference labels are:

- `+ Add`
- `Delete`
- `⚡ Nhập tự động`

`Nhập tự động` deliberately reuses the existing guarded capture-from-current-selection behavior. It must not silently scan or mutate an unbounded whole drawing.

## 7. Right manager presentation

The existing production handlers remain unchanged while labels align to BLT3D:

- **Quản lý bản vẽ** — Thêm, Nạp, Di chuyển, Xóa, Khoanh vùng and existing supported drawing actions.
- **Quản lý lớp** — search/filter plus Hiện, Ẩn, Đảo, Bỏ chọn.

Deleting/removing drawings continues to use the existing guarded Xref/drawing logic; the visual label does not change command safety semantics.

## 8. Activation and lifecycle

`BltBimWorkspaceActivationCoordinator` observes the active Ribbon tab on the UI idle dispatcher and opens the BIM shell on a transition into `QS3D_BIM`. It does not force palettes open on every timer tick, so a user who manually closes a palette while staying on BIM is respected. Ribbon teardown stops the coordinator.

`RibbonInitializationCoordinator` initializes and resets `BltModelingRibbonAugmenter` with the rest of the Ribbon lifecycle so NETLOAD/unload/retry cannot leave stale MODELING panel objects.

## 9. Regression protection

`scripts/preflight-blt3d-bim-workspace.py` is auto-discovered by `scripts/preflight-all.py`. It locks the source-level contracts for:

- dual left/right BIM palette visibility and docking;
- owner-reference Family/category/footer labels;
- integration with the real Family workspace controls;
- drawing/layer manager labels;
- BIM-tab activation lifecycle;
- Vẽ / Công cụ / IFC ribbon ordering;
- exact MODELING group and button ordering;
- MODELING command routing, compact `RibbonRowPanel` / `RibbonRowBreak` layout and lifecycle integration;
- ten-tab QS3D topbar contract.

Any future change that breaks these contracts must update both the implementation and this document intentionally.
