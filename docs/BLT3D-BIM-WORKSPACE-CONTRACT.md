# BLT3D Ribbon + BIM workspace contract

This document is the implementation contract for the QS3D BricsCAD **VẼ**, **MÔ HÌNH BIM** and **MODELING** experiences. It translates owner-provided BLT3D references into production UI behavior; it is not a screenshot mock. The V26 adapter links the V25 Ribbon source, so source-level Ribbon changes are shared, while licensed host rendering still requires local validation in each supported BricsCAD version.

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

## 2. VẼ ribbon contract

The owner-reference VẼ tab keeps the qualified QS3D/BricsCAD commands but uses compact three-row columns instead of a flat button strip.

**Vẽ** is arranged as:

- column 1: `Điểm` → `Đường thẳng` → `Theo nét CAD`;
- column 2: `Cung` → `Chữ nhật` → `Đường tròn`;
- column 3: `Biên dạng`.

`Biên dạng` belongs to the Vẽ group, not Công cụ.

**Công cụ** is arranged as:

- column 1: `Dốc sàn` → `Cắt sàn` → `Di chuyển`;
- column 2: `Xoay` → `Đối xứng` → `Sao chép`;
- column 3: `Chia cấu kiện` → `Nối liền` → `Đo khoảng cách`;
- column 4: `Nối góc` → `Nối chữ T`.

The existing **IFC** panel remains after Vẽ/Công cụ with the qualified import, lightweight import, selective delete and export actions. The compact layout does not take ownership of IFC behavior.

`BltDrawRibbonAugmenter` remains the source of command routing, handlers and icons. `BltDrawRibbonLayoutRefiner` only re-packs those already-created buttons into `RibbonRowPanel` / `RibbonRowBreak` columns. `BltDrawRibbonFailSafe` resets the rich augmenter and restores the captured bootstrap panels if the host cannot apply the compact layout, so a failed presentation upgrade cannot strand the user with a half-built Draw tab.

## 3. MÔ HÌNH BIM ribbon contract

The BIM tab exposes the same qualified BLT3D surface in three panels, in order:

- **Vẽ** — the compact VẼ arrangement above, including `Biên dạng` in the Vẽ group;
- **Công cụ** — the compact tool arrangement above;
- **IFC** — Import IFC, lightweight Import IFC, delete selected IFC entities and IFC export.

`BltBimRibbonMirrorAugmenter` creates independent BIM Ribbon objects while reusing the source command handlers, command parameters, images and sizing. It mirrors `RibbonButton`, `RibbonRowPanel` and `RibbonRowBreak` recursively so BIM does not regress to a flat layout when VẼ is compacted. It must not duplicate geometry or business logic.

## 4. MODELING ribbon contract

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

### 4.1 Button function contract

`BltModelingRibbonFunctionRefiner` runs after the reference panels are built and pins the exact route of all 21 visible actions. Initialization fails closed if a reference button is missing, duplicated, lacks its command handler, or cannot retain its required `CommandParameter`.

The production route matrix is:

- `Vật liệu` → `MATERIALS`;
- `Mặt cắt thép` → `BIMPROFILES`;
- `Tạo chi tiết` → `BIMCREATEDETAIL`;
- `Mặt XY` → `UCS World`;
- `Đường` → `LINE`;
- `Polyline` → `PLINE`;
- `Chữ nhật` → `RECTANG`;
- `Tròn` → `CIRCLE`;
- `Cung` → `ARC`;
- `Nối polyline` → `JOIN`;
- `Offset` → `OFFSET`;
- `Di chuyển` → `MOVE`;
- `Sao chép` → `COPY`;
- `Theo phương Z` → `QS3DMOVEZ`;
- `Extrude` → `EXTRUDE`;
- `Sweep` → `SWEEP`;
- `Loft` → `LOFT`;
- `Gắn vào Family` → `QS3DFAMILIES`;
- `Union` → `UNION`;
- `Subtract` → `SUBTRACT`;
- `Intersect` → `INTERSECT`.

Native BricsCAD routes use underscore-prefixed English command/option tokens in source so localized BricsCAD installations do not depend on translated command names.

`Gắn vào Family` opens the existing production `QS3DFAMILIES` Family Manager, where the document-bound workflow owns Family/Type CRUD, inheritance-safe property behavior and semantic assignment for the current drawing rather than introducing a screenshot-only duplicate.

`Theo phương Z` no longer exposes unrestricted `MOVE` and asks the user to manually key `@0,0,<ΔZ>`. `QS3DMOVEZ` preserves PICKFIRST selection, falls back to an interactive selection when necessary, accepts only a finite non-zero signed Z distance, re-checks the active document after the prompt, then delegates the actual mutation to native `MOVE` with its `Displacement` option and an `(0,0,ΔZ)` vector in the current UCS. BricsCAD therefore remains authoritative for native move/locked-layer/Undo behavior while the button semantics are genuinely constrained to Z.

### 4.2 Icon / sizing contract

`BltModelingRibbonVisualRefiner` runs after function pinning. It requires the same 21 exact button IDs, gives every action both `Image` and `LargeImage`, keeps `ShowImage=true`, and rejects a missing/text-only final reference surface.

The first four lead actions (`Vật liệu`, `Mặt cắt thép`, `Tạo chi tiết`, `Mặt XY`) remain large; all remaining actions stay standard-sized inside compact rows. Icons are frozen WPF `DrawingImage` vectors on a fixed 32×32 logical canvas with a blue/dark-blue/light-outline palette tuned for the dark native Ribbon. This keeps standard/large icons visually consistent without shipping bitmap captures or proprietary BLT3D assets.

## 5. Full BIM workspace contract

Entering `QS3D_BIM` activates the full workspace once per tab transition:

- **Left dock — Mô hình BIM**: working Zone and Floor, model category tree, Family/Type search/list, Add, Delete, guarded import from the current CAD selection, and editable properties.
- **Center — BricsCAD native viewport**: the real drawing/model viewport remains the preview and interaction surface. PAN, ZOOM, ORBIT, PICK, native visual styles and BricsCAD selection remain authoritative. No duplicate fake 3D renderer is introduced.
- **Right dock — Quản lý bản vẽ & lớp**: existing Xref/drawing and layer management remains fully functional, with BLT3D-facing labels.
- **Footer/status**: model/BQ/inspection modes, active floor/elevation and live semantic/viewport status remain available.

The user may resize palettes. The BIM entry contract reasserts left/right dock sides so the native viewport stays centered.

## 6. Left model/category presentation

The visible owner-reference category tail includes:

- Đào đắp
- Kết cấu thép
- Cấu kiện khác

QS3D does not currently expose a dedicated steel semantic category/builder. Therefore **Kết cấu thép** is a presentation-compatible grouping backed by `ElementCategory.CustomQuantity` until a real steel domain implementation exists. This explicitly avoids pretending that generic geometry is native steel BIM data.

## 7. Family actions

The production labels are:

- `+ Add`
- `Delete`
- `⚡ Nhập từ chọn`

`Nhập từ chọn` deliberately reuses the existing guarded capture-from-current-selection behavior. It must not silently scan or mutate an unbounded whole drawing.

## 8. Right manager presentation

The existing production handlers remain unchanged while labels align to BLT3D:

- **Quản lý bản vẽ** — Thêm, Nạp, Di chuyển, Xóa, Khoanh vùng and existing supported drawing actions.
- **Quản lý lớp** — search/filter plus Hiện, Ẩn, Đảo, Bỏ chọn.

Deleting/removing drawings continues to use the existing guarded Xref/drawing logic; the visual label does not change command safety semantics.

## 9. Activation and lifecycle

`BltBimWorkspaceActivationCoordinator` observes the active Ribbon tab on the UI idle dispatcher and opens the BIM shell on a transition into `QS3D_BIM`. It does not force palettes open on every timer tick, so a user who manually closes a palette while staying on BIM is respected. Ribbon teardown stops the coordinator.

`RibbonInitializationCoordinator` builds `BltModelingRibbonAugmenter`, then applies `BltModelingRibbonFunctionRefiner`, then `BltModelingRibbonVisualRefiner` before generic icon/command-parameter fallback. It resets all three on teardown so NETLOAD/unload/retry cannot leave stale MODELING panels, routes or artwork. Draw compacting remains inside `BltDrawRibbonFailSafe`, so the existing coordinator retry boundary also owns layout recovery.

## 10. Regression protection

`scripts/preflight-blt3d-bim-workspace.py`, `scripts/preflight-blt3d-draw-layout.py`, `scripts/preflight-modeling-ribbon-parity.py` and `scripts/preflight-modeling-ribbon-functions.py` are auto-discovered by `scripts/preflight-all.py`. Together they lock the source-level contracts for:

- dual left/right BIM palette visibility and docking;
- owner-reference Family/category/footer labels;
- integration with the real Family workspace controls;
- drawing/layer manager labels;
- BIM-tab activation lifecycle;
- Vẽ / Công cụ / IFC ribbon ordering;
- exact VẼ and Công cụ compact column/button ordering, including `Biên dạng` ownership;
- recursive compact-row mirroring from VẼ into MÔ HÌNH BIM;
- exact MODELING group and button ordering;
- exact 21-button MODELING route table and non-null command handlers;
- true Z-only `QS3DMOVEZ` selection/prompt/native-MOVE delegation contract;
- exact 21-button vector icon coverage, normalized logical icon bounds and large/standard sizing;
- MODELING function/visual lifecycle ordering and V26 shared-source coverage;
- ten-tab QS3D topbar contract using the production tab IDs.

Any future change that breaks these contracts must update both the implementation and this document intentionally.
