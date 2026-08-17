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
    runtime_layout = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs")
    family_workspace = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFamilyWorkspace.cs")
    right = read("src/QS3D.BricsCAD.V25/UI/RightPanel.Blt3dReferenceShell.cs")
    activation = read("src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs")
    init = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
    bim_ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/BltBimRibbonMirrorAugmenter.cs")
    modeling = read("src/QS3D.BricsCAD.V25/Ribbon/BltModelingRibbonAugmenter.cs")
    topbar = read("src/QS3D.BricsCAD.V25/Ribbon/BltTopbarTabContract.cs")

    # Default owner BIM shell: integrated two-column Workspace left, native BricsCAD viewport
    # center, Drawing/Layer Management right. Optional dedicated Properties/Quantity stay hidden.
    for token in (
        "public static bool ShowBimWorkspace()",
        "EnsureBimDockContract();",
        "SetVisibility(workspace: true, right: true, quantityInsight: false);",
        "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);",
        "_workspace.Dock = DockSides.Left",
        "_right.Dock = DockSides.Right",
        "viewport BricsCAD native ở giữa",
    ):
        require(palette, token, "PaletteCoordinator BIM shell")
    if "SetVisibility(workspace: true, right: true, quantityInsight: true);" in palette:
        fail("default BIM must not auto-open Quantity Insight")

    for token in (
        "Grid.SetColumn(modelPane, 0);",
        "Grid.SetColumn(columnSplitter, 1);",
        "Grid.SetColumn(familyPane, 2);",
        "columnSplitter.ResizeDirection = GridResizeDirection.Columns;",
        "familyPane.RowDefinitions[0].Height = new GridLength(56, GridUnitType.Star);",
        "familyPane.RowDefinitions[2].Height = new GridLength(44, GridUnitType.Star);",
    ):
        require(runtime_layout, token, "BLT3D side-by-side Workspace runtime layout")
    if "Grid.SetRow(familyPane, 2);" in runtime_layout:
        fail("default BIM must not stack Family/Properties below Model")

    # BLT3D parity is implemented inside BricsCAD. This runtime layout may rearrange QS3D-owned
    # palette content, but it must never create a replacement top-level window or fake CAD viewport.
    for forbidden in (
        "new Viewport",
        "Viewport3D",
        "new Window",
        "WindowStyle",
        "ShowDialog(",
        "Topmost =",
    ):
        if forbidden in runtime_layout:
            fail(
                "BricsCAD host UI/modelspace must remain host-owned; "
                f"runtime BIM layout contains forbidden standalone-host token: {forbidden}"
            )

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

    # Keep the already-qualified owner Ribbon: Vẽ / Công cụ / IFC.
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
        '"Vật\\nliệu"', '"Mặt cắt\\nthép"', '"Tạo chi\\ntiết"', '"Mặt\\nXY"',
        '"Đường"', '"Polyline"', '"Chữ nhật"', '"Tròn"', '"Cung"',
        '"Nối polyline"', '"Offset"', '"Di chuyển"', '"Sao chép"', '"Theo phương Z"',
        '"Extrude"', '"Sweep"', '"Loft"', '"Gắn vào Family"', '"Union"', '"Subtract"', '"Intersect"',
        '"Bricscad.Windows.RibbonRowPanel"', '"Bricscad.Windows.RibbonRowBreak"',
        'spec.Large ? "Large" : "Standard"', 'private const string OwnedPrefix = "QS3D_MODELING_";',
    ):
        require(modeling, token, "MODELING owner-reference surface")

    for token in (
        '"_.MATERIALS"', '"_.BIMPROFILES"', '"_.BIMCREATEDETAIL"', '"_.UCS _World"',
        '"_.LINE"', '"_.PLINE"', '"_.RECTANG"', '"_.CIRCLE"', '"_.ARC"', '"_.JOIN"',
        '"_.OFFSET"', '"_.MOVE"', '"_.COPY"', '"_.EXTRUDE"', '"_.SWEEP"', '"_.LOFT"',
        '"QS3DFAMILIES"', '"_.UNION"', '"_.SUBTRACT"', '"_.INTERSECT"',
    ):
        require(modeling, token, "MODELING command routing")

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

    print("PASS: BLT3D BIM owner-reference workspace + Ribbon + MODELING contract is intact without replacing BricsCAD host UI.")
    return 0


if __name__ == "__main__":
    sys.exit(main())