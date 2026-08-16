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
        fail(f"{label}: missing source contract: {needle}")


def require_once(text, needle, label):
    count = text.count(needle)
    if count != 1:
        fail(f"{label}: expected exactly one {needle!r}, found {count}")


def require_order(text, tokens, label):
    cursor = -1
    for token in tokens:
        position = text.find(token)
        if position < 0:
            fail(f"{label}: missing {token}")
        if position <= cursor:
            fail(f"{label}: incorrect order around {token}")
        cursor = position


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    augmenter = read("src/QS3D.BricsCAD.V25/Ribbon/BltDrawRibbonAugmenter.cs")
    layout = read("src/QS3D.BricsCAD.V25/Ribbon/BltDrawRibbonLayoutRefiner.cs")
    icons = read("src/QS3D.BricsCAD.V25/Ribbon/BltDrawRibbonReferenceIconDecorator.cs")
    bim_mirror = read("src/QS3D.BricsCAD.V25/Ribbon/BltBimRibbonMirrorAugmenter.cs")
    finalizer = read("src/QS3D.BricsCAD.V25/Ribbon/BltDrawRibbonReferenceFinalizer.cs")
    coordinator = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
    topbar = read("src/QS3D.BricsCAD.V25/Ribbon/BltTopbarTabContract.cs")

    visible_button_ids = (
        "QS3D_DRAW_BLT_POINT",
        "QS3D_DRAW_BLT_LINE",
        "QS3D_DRAW_BLT_TRACE",
        "QS3D_DRAW_BLT_ARC",
        "QS3D_DRAW_BLT_RECTANGLE",
        "QS3D_DRAW_BLT_CIRCLE",
        "QS3D_DRAW_BLT_BOUNDARY",
        "QS3D_DRAW_BLT_SLAB_SLOPE",
        "QS3D_DRAW_BLT_SLAB_CUT",
        "QS3D_DRAW_BLT_MOVE",
        "QS3D_DRAW_BLT_ROTATE",
        "QS3D_DRAW_BLT_MIRROR",
        "QS3D_DRAW_BLT_COPY",
        "QS3D_DRAW_BLT_BREAK",
        "QS3D_DRAW_BLT_JOIN",
        "QS3D_DRAW_BLT_DISTANCE",
        "QS3D_DRAW_BLT_CORNER",
        "QS3D_DRAW_BLT_TEE",
    )

    # Every owner-reference button must exist in the command surface, compact layout, and
    # dedicated semantic icon decorator exactly once. This prevents text-only fallback drift.
    for button_id in visible_button_ids:
        token = f'"{button_id}"'
        require_once(augmenter, token, "Draw button creation")
        require_once(layout, token, "Draw compact layout")
        require_once(icons, token, "Draw icon map")

    for token in (
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", CreateIcon(spec.Value));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Value));',
        'new RectangleGeometry(new Rect(0, 0, 32, 32))',
        "PenLineCap.Round",
        "PenLineJoin.Round",
    ):
        require(icons, token, "BLT3D-style vector icon contract")

    # The supplied VẼ reference ends after Công cụ. IFC remains a staging panel only long
    # enough for MÔ HÌNH BIM to clone its independent qualified IFC surface.
    require(
        bim_mirror,
        'new PanelMirrorSpec("QS3D_DRAW_BLT_IFC_PANEL_SOURCE", "QS3D_BIM_BLT_IFC_PANEL_SOURCE")',
        "BIM IFC mirror",
    )
    for token in (
        'private const string DrawIfcPanelSourceId = "QS3D_DRAW_BLT_IFC_PANEL_SOURCE";',
        'private const string BimIfcPanelSourceId = "QS3D_BIM_BLT_IFC_PANEL_SOURCE";',
        'if (FindPanelBySourceId(bimPanels, BimIfcPanelSourceId) == null)',
        'Remove(drawPanels, ifcPanel);',
    ):
        require(finalizer, token, "VẼ final IFC removal")

    require_order(
        coordinator,
        (
            "BltDrawRibbonFailSafe.TryInitialize()",
            "BltDrawRibbonReferenceIconDecorator.TryInitialize()",
            "BltBimRibbonMirrorAugmenter.TryInitialize()",
            "BltDrawRibbonReferenceFinalizer.TryInitialize()",
        ),
        "Draw/BIM initialization sequence",
    )

    # Keep the owner-supplied ten-tab shell stable while this lane changes only VẼ presentation.
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
        "BLT3D topbar tab order",
    )

    print("PASS: VẼ keeps the BLT3D compact topbar, semantic vector icons, and no final IFC panel while BIM retains IFC.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
