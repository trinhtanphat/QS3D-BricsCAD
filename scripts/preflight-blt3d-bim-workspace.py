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

    # Full three-zone BIM workspace: real QS3D workspace left, native BricsCAD viewport centre,
    # production drawing/layer manager right. Quantity insight is intentionally not forced open.
    for token in (
        "public static void ShowBimWorkspace()",
        "EnsureBimDockContract();",
        "SetVisibility(workspace: true, right: true, quantityInsight: false);",
        "_workspace.Dock = DockSides.Left",
        "_right.Dock = DockSides.Right",
        "viewport BricsCAD native ở giữa",
    ):
        require(palette, token, "PaletteCoordinator BIM shell")

    # Owner-reference family/model chrome while preserving the guarded production handlers.
    for token in (
        "⚡ Nhập tự động",
        "+ Add",
        "Delete",
        "Kết cấu thép",
        "Cấu kiện khác",
        "ElementCategory.CustomQuantity",
        "BLT3D • VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK",
    ):
        require(workspace, token, "Workspace BLT3D shell")

    # The pre-existing family workspace is the actual implementation, not a screenshot-only facade.
    for token in (
        "FamilySearchBox",
        "FamilyList",
        "PropertyGrid",
        "ZoneCombo",
        "FloorCombo",
        "ModelTree",
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

    # BIM ribbon is required to stay the exact qualified Vẽ / Công cụ / IFC reference surface.
    require_order(
        bim_ribbon,
        ('"Vẽ"', '"Công cụ"', '"IFC"'),
        "BIM ribbon panel order",
    )

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

    # Topbar remains the ten-tab owner contract and may not silently resurrect QS3D_AUTHOR.
    for token in (
        '"QS3D_HOME"',
        '"QS3D_PROJECT"',
        '"QS3D_BIM"',
        '"QS3D_RECOGNITION"',
        '"QS3D_DRAW"',
        '"QS3D_TOOLS"',
        '"QS3D_MODELING"',
        '"QS3D_VIEW"',
        '"QS3D_QUANTITY"',
        '"QS3D_REVIEW"',
    ):
        require(topbar, token, "Topbar owner contract")

    print("PASS: BLT3D BIM + MODELING owner-reference source contract is intact.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
