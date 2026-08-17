#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def require_order(text, needles, label):
    cursor = -1
    for needle in needles:
        index = text.find(needle, cursor + 1)
        if index < 0:
            fail(f"{label}: missing ordered token: {needle}")
        if index <= cursor:
            fail(f"{label}: token out of order: {needle}")
        cursor = index


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    palette = read("src/QS3D.BricsCAD.V25/PaletteCoordinator.cs")
    workspace = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dBimReferenceShell.cs")
    family_workspace = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFamilyWorkspace.cs")
    right = read("src/QS3D.BricsCAD.V25/UI/RightPanel.Blt3dReferenceShell.cs")
    activation = read("src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs")
    init = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
    bim_ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/BltBimRibbonMirrorAugmenter.cs")
    modeling = read("src/QS3D.BricsCAD.V25/Ribbon/BltModelingRibbonAugmenter.cs")
    topbar = read("src/QS3D.BricsCAD.V25/Ribbon/BltTopbarTabContract.cs")

    # Full BIM workspace: real QS3D workspace left, native BricsCAD viewport centre,
    # production drawing/layer manager and quantity explanation palettes right.
    for token in (
        "public static bool ShowBimWorkspace()",
        "EnsureBimDockContract();",
        "SetVisibility(workspace: true, right: true, quantityInsight: true);",
        "_workspace.Dock = DockSides.Left",
        "_right.Dock = DockSides.Right",
        "_quantityInsight.Dock = DockSides.Right",
        "_quantityInsightPanel?.RefreshQuantityInsights();",
        "viewport BricsCAD native ở giữa",
    ):
        require(palette, token, "PaletteCoordinator BIM shell")

    # Owner-reference family/model chrome while preserving the guarded production handlers.
    # Import remains explicitly selection-bounded; the label must not imply a whole-DWG scan.
    for token in (
        "⚡ Nhập từ chọn",
        "+ Add",
        "Delete",
        "Kết cấu thép",
        "Cấu kiện khác",
        "ElementCategory.CustomQuantity",
        "BLT3D • VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK",
    ):
        require(workspace, token, "Workspace BLT3D shell")

    # Guard the actual controls and production hooks consumed by the BLT3D family-workspace partial.
    # Historical aliases such as FamilySearchBox/PropertyGrid/FloorCombo are not members of this
    # implementation and made the source guard fail even while the real workspace integration held.
    for token in (
        "WorkspaceContentRoot",
        "WorkspaceOverflow",
        "FamilySearch",
        "FamilyList",
        "PropertyList",
        "ZoneCombo",
        "ModelTree",
        "AttachFamilySubtypeInteractions();",
    ):
        require(family_workspace, token, "Family workspace integration")

    for token in (
        "Quản lý bản vẽ",
        "Quản lý lớp",
        'RenameRightButton("Nạp lại", "Nạp")',
        'RenameRightButton("Gỡ Xref", "Xóa")',
        'RenameRightButton("Đảo chọn", "Đảo")',
    ):
        require(right, token, "Right manager BLT3D shell")

    for token in (
        'private const string BimTabId = "QS3D_BIM";',
        "PaletteCoordinator.ShowBimWorkspace();",
        "DispatcherPriority.ApplicationIdle",
    ):
        require(activation, token, "BIM tab activation")

    require(init, "BltBimWorkspaceActivationCoordinator.Start();", "Ribbon initialization")
    require(init, "BltBimWorkspaceActivationCoordinator.Stop();", "Ribbon teardown")
    require(init, "BltModelingRibbonAugmenter.TryInitialize()", "MODELING ribbon initialization")
    require(init, "BltModelingRibbonAugmenter.Reset();", "MODELING ribbon teardown")

    # BIM ribbon mirrors the exact qualified Vẽ / Công cụ / IFC source panels in owner order.
    # The Draw surface can now contain compact RibbonRowPanel/RibbonRowBreak columns, so the
    # mirror must clone nested row items instead of assuming a flat list of RibbonButton objects.
    require_order(
        bim_ribbon,
        (
            'new PanelMirrorSpec("QS3D_DRAW_BLT_DRAW_PANEL_SOURCE", "QS3D_BIM_BLT_DRAW_PANEL_SOURCE")',
            'new PanelMirrorSpec("QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE", "QS3D_BIM_BLT_TOOLS_PANEL_SOURCE")',
            'new PanelMirrorSpec("QS3D_DRAW_BLT_IFC_PANEL_SOURCE", "QS3D_BIM_BLT_IFC_PANEL_SOURCE")',
        ),
        "BIM ribbon panel order",
    )
    for token in (
        'string.Equals(typeName, "RibbonButton", StringComparison.Ordinal)',
        'string.Equals(typeName, "RibbonRowBreak", StringComparison.Ordinal)',
        'string.Equals(typeName, "RibbonRowPanel", StringComparison.Ordinal)',
        "CloneRibbonItem(sourceItem, ref buttonCount)",
    ):
        require(bim_ribbon, token, "BIM compact Draw mirror")

    # MODELING mirrors the owner screenshot: three lead groups followed by compact stacked groups.
    require_order(
        modeling,
        (
            '"Vật liệu"',
            '"Kết cấu thép"',
            '"Mặt phẳng"',
            '"Vẽ phác"',
            '"Chỉnh sửa"',
            '"Dựng 3D"',
            '"Cấu kiện"',
            '"Cắt khối"',
        ),
        "MODELING ribbon panel order",
    )

    for token in (
        '"Vật\\nliệu"',
        '"Mặt cắt\\nthép"',
        '"Tạo chi\\ntiết"',
        '"Mặt\\nXY"',
        '"Đường"',
        '"Polyline"',
        '"Chữ nhật"',
        '"Tròn"',
        '"Cung"',
        '"Nối polyline"',
        '"Offset"',
        '"Di chuyển"',
        '"Sao chép"',
        '"Theo phương Z"',
        '"Extrude"',
        '"Sweep"',
        '"Loft"',
        '"Gắn vào Family"',
        '"Union"',
        '"Subtract"',
        '"Intersect"',
        '"Bricscad.Windows.RibbonRowPanel"',
        '"Bricscad.Windows.RibbonRowBreak"',
        'spec.Large ? "Large" : "Standard"',
        'private const string OwnedPrefix = "QS3D_MODELING_";',
    ):
        require(modeling, token, "MODELING owner-reference surface")

    for token in (
        '"_.MATERIALS"',
        '"_.BIMPROFILES"',
        '"_.BIMCREATEDETAIL"',
        '"_.UCS _World"',
        '"_.LINE"',
        '"_.PLINE"',
        '"_.RECTANG"',
        '"_.CIRCLE"',
        '"_.ARC"',
        '"_.JOIN"',
        '"_.OFFSET"',
        '"_.MOVE"',
        '"_.COPY"',
        '"_.EXTRUDE"',
        '"_.SWEEP"',
        '"_.LOFT"',
        '"QS3DFAMILIES"',
        '"_.UNION"',
        '"_.SUBTRACT"',
        '"_.INTERSECT"',
    ):
        require(modeling, token, "MODELING command routing")

    # Topbar remains the exact ten-tab IDs emitted by RibbonBootstrapper and may not silently
    # resurrect QS3D_AUTHOR. Guard the production IDs, not stale aliases from older prototypes.
    require_order(
        topbar,
        (
            'new TabSpec("QS3D_HOME", "KHỞI ĐẦU")',
            'new TabSpec("QS3D_PROJECT", "THIẾT LẬP DỰ ÁN")',
            'new TabSpec("QS3D_BIM", "MÔ HÌNH BIM")',
            'new TabSpec("QS3D_RECOGNIZE", "NHẬN DẠNG")',
            'new TabSpec("QS3D_DRAW", "VẼ")',
            'new TabSpec("QS3D_TOOL", "TOOL")',
            'new TabSpec("QS3D_MODELING", "MODELING")',
            'new TabSpec("QS3D_VIEW", "XEM")',
            'new TabSpec("QS3D_QTY", "ĐỊNH LƯỢNG")',
            'new TabSpec("QS3D_REV", "BẢN SỬA ĐỔI")',
        ),
        "Topbar owner contract",
    )
    if 'new TabSpec("QS3D_AUTHOR"' in topbar:
        fail("Topbar owner contract must not resurrect QS3D_AUTHOR")

    print("PASS: BLT3D BIM + MODELING owner-reference source contract is intact.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
