# BLT-reference UI parity plan — 2026-08-14

## Goal and boundary

Use the four owner-supplied BLT3D screenshots only as a clean-room workflow/layout reference for QS3D's BricsCAD V25 hosted plugin. Do not copy BLT binaries, icons, assets, code, data formats, or proprietary implementation details. Preserve the canonical QS3D semantic model, BricsCAD-native viewport/database/editor, and existing NETLOAD/startup safety contracts.

## Screenshot inventory

The reference surface contains these major areas:

1. Top Ribbon tabs: KHỞI ĐẦU, THIẾT LẬP DỰ ÁN, MÔ HÌNH BIM, NHẬN DẠNG, VẼ, TOOL, MODELING, XEM, ĐỊNH LƯỢNG, BẢN SỬA ĐỔI.
2. VẼ groups: primitive/reference drafting, transform/edit tools, slab helpers, and IFC import/export.
3. Workspace left rail: Zone/Tầng selectors and a hierarchical model/category tree.
4. Family/Type pane: Add, Delete, Vẽ 3D, search, selection and properties.
5. Drawing/layer management and viewport controls on the right, plus model/BQ and context indicators in the footer.

## Audit against current QS3D source

Already present before this batch:

- All screenshot top-level functional areas already exist in `RibbonBootstrapper`; QS3D additionally keeps a dedicated `TẠO MỚI` authoring tab because direct semantic authoring is a first-class QS3D workflow.
- Workspace already has Zone/Tầng selectors, model tree, Family/Type Add/Delete/Bóc chọn/Vẽ 3D, search, property editor, selection inspection, status/footer, native view controls and BQ entry points.
- Right-side project/Xref/layer management is already implemented by `RightPanel` and its interaction/search/lock/scale partials.
- Existing QS3D commands already cover semantic rooms/finishes, walls/glass/curtain, beams/slabs/columns/structural walls/foundations, doors/openings, stairs/railings, earthwork, quantity/BQ/BBS, revisions, health and view/navigation workflows.
- `BasicDrawingCommands` already provides context-aware QS3D Line/Rectangle/Circle commands.

Confirmed screenshot-facing gaps before this batch:

- VẼ Ribbon did not expose `Theo nét CAD`, `Đường tròn`, `Biên dạng`, `Dốc sàn`, `Cắt sàn`, `Nối góc`, `Nối chữ T` or the four IFC buttons from the screenshots.
- Existing Line/Rectangle buttons used raw native commands instead of the context-aware QS3D Line/Rectangle implementations.
- The Workspace category tree had the main categories but not the screenshot's detailed child labels for grids, beams, slabs, canopies, foundations, earthwork and custom quantities.

## Implementation in this batch

### Ribbon parity

`QuickWorkflowRibbonAugmenter` now keeps the existing `TẠO MỚI > Tác vụ nhanh` panel and additionally reconciles the screenshot-facing VẼ surface without changing Ribbon initialization timing:

- Vẽ: Đường thẳng → `QS3DDRAWLINE`; Theo nét CAD → `QS3DDRAWBYCAD`; Chữ nhật → `QS3DDRAWRECT`; Đường tròn → `QS3DDRAWCIRCLE`; Biên dạng → `QS3DDRAWPROFILE`.
- Công cụ: Dốc sàn → `QS3DFLOORSLOPE`; Cắt sàn → `QS3DSLABCUT`; existing Move/Rotate/Mirror/Copy remain available.
- Kết nối & đo: Nối góc → `QS3DJOINCORNER`; Nối chữ T → `QS3DJOINTEE`; existing Break/Join/Distance/Section remain available.
- IFC: Nhập IFC → `QS3DIFCIMPORT`; Nhập IFC (nhẹ) → `QS3DIFCIMPORTLIGHT`; Xóa IFC → `QS3DIFCREMOVE`; Xuất IFC → `QS3DIFCEXPORT`.

The IFC adapters deliberately delegate to BricsCAD's native Import/Xref/IFCEXPORT workflows instead of implementing a second IFC engine in QS3D. The lightweight entry opens the same IFC Import settings path so the operator can choose the XRef/spatial-split profile supported by the installed BricsCAD edition.

### Workspace tree parity

`ReferenceWorkspaceTreeAugmenter` adds the screenshot's detailed presentation labels at `WorkspacePanel.Loaded` while reusing canonical existing QS3D category tags. It is idempotent and presentation-only: no ProjectState or CAD mutation occurs merely because the tree is expanded.

Added/ensured labels include:

- Lưới Trục → Lưới Thẳng, Lưới Cong.
- HT_Phòng → existing finish categories plus Trát Trần.
- Dầm → Dầm HCN, Giằng Tường, Lanh Tô.
- Sàn → Sàn Đặc, Đường Dốc, Lỗ Mở Sàn.
- Mái Hắt → Mái Hắt Diện Tích, Mái Hắt Biên Dạng.
- Cột/Vách/Cầu Thang detailed child rows.
- Móng → Cọc, Đài Cọc, Dầm Móng, Móng Băng, Móng Bè, Bê Tông Lót.
- Đào đắp → hố móng/khối đất/giao đào/sau trừ.
- KL Tùy chỉnh → chiều dài/diện tích/thể tích/biên dạng/mặt phẳng.
- Modeling top-level entry.

### Follow-up integration audit

A post-implementation whole-session review found that the original tree augmenter defined `EnsureRegistered()` but had no source caller. That made the detailed screenshot tree a dead-code risk even though the labels and handler were implemented correctly.

The follow-up fixes that integration boundary without touching `PluginEntry`, palette creation, document lifecycle or the active NETLOAD/startup lane:

- `WorkspacePanel.ReferenceTreeRegistration.cs` adds a presentation-only static field initializer on the existing `WorkspacePanel` partial type.
- The initializer calls `ReferenceWorkspaceTreeAugmenter.EnsureRegistered()` exactly once as part of `WorkspacePanel` type initialization, before the first panel instance is constructed.
- `EnsureRegistered()` remains idempotent and continues to install a `WorkspacePanel.Loaded` class handler; it does not construct the palette or mutate ProjectState/CAD.
- The focused preflight now fails if the augmenter becomes orphaned again or if registration drifts from type initialization into an instance/startup side effect.

## Validation strategy

Remote/source-safe checks:

1. Static guard asserts the screenshot-critical labels and command mappings stay present.
2. Static guard asserts the reference tree registration is reachable from `WorkspacePanel` type initialization.
3. Source readback confirms the VẼ augmenter remains additive and does not touch `RibbonInitializationCoordinator` or startup scheduling.
4. Source readback confirms the Workspace tree augmenter only mutates visual `TreeViewItem` structure and registration does not construct a palette.
5. `scripts/preflight-all.py` auto-discovers `preflight-blt-reference-ui-parity.py` through its `preflight-*.py` scan; no aggregator edit is required.
6. GitHub Actions remain idle unless the owner separately requests CI, per `CI_POLICY.md`.

Native/local acceptance still required for a true host UI PASS:

1. Build the exact V25 SHA against licensed BricsCAD V25.
2. NETLOAD/open QS3D and confirm no regression in the existing-project startup-hang lane.
3. Confirm every VẼ/IFC button is visible, clickable, and launches the intended command under the installed BricsCAD edition/license.
4. Confirm Workspace tree rows render correctly under Windows scaling/dark theme and selection routes to the intended canonical category.
5. Confirm native undo/cancel behavior for 3DROTATE/SUBTRACT/FILLET/EXTEND/IMPORT/XREF/IFCEXPORT.

## Visual fidelity policy

QS3D intentionally does not copy the reference product's proprietary icons. The current Ribbon keeps QS3D text-first controls; original QS3D iconography can be added later as independent assets without changing command identity. Functional layout/labels and workflow discoverability are the parity target, not pixel-for-pixel cloning.
