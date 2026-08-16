#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REFINER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonLayoutRefiner.cs"
FAIL_SAFE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonFailSafe.cs"
ICONS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonReferenceIconDecorator.cs"
FINALIZER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonReferenceFinalizer.cs"
INIT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"


def fail(message: str) -> None:
    print("ERROR:", message)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def require_order(text: str, needles: list[str], label: str) -> None:
    cursor = -1
    for needle in needles:
        index = text.find(needle, cursor + 1)
        if index < 0:
            fail(f"{label}: missing ordered token: {needle}")
        if index <= cursor:
            fail(f"{label}: token out of order: {needle}")
        cursor = index


def main() -> int:
    for path, label in (
        (REFINER, "BltDrawRibbonLayoutRefiner.cs"),
        (FAIL_SAFE, "BltDrawRibbonFailSafe.cs"),
        (ICONS, "BltDrawRibbonReferenceIconDecorator.cs"),
        (FINALIZER, "BltDrawRibbonReferenceFinalizer.cs"),
        (INIT, "RibbonInitializationCoordinator.cs"),
    ):
        if not path.is_file():
            fail(f"missing {label}")

    refiner = REFINER.read_text(encoding="utf-8")
    fail_safe = FAIL_SAFE.read_text(encoding="utf-8")
    icons = ICONS.read_text(encoding="utf-8")
    finalizer = FINALIZER.read_text(encoding="utf-8")
    init = INIT.read_text(encoding="utf-8")

    require(refiner, 'private const string DrawTabId = "QS3D_DRAW";', "Draw tab ownership")
    require(refiner, 'private const string DrawPanelSourceId = "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE";', "Vẽ panel ownership")
    require(refiner, 'private const string ToolsPanelSourceId = "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE";', "Công cụ panel ownership")
    require(refiner, 'Create("Bricscad.Windows.RibbonRowPanel")', "compact row layout")
    require(refiner, 'Create("Bricscad.Windows.RibbonRowBreak")', "compact row breaks")
    require(refiner, "MatchesLayout(drawItems, DrawColumns)", "idempotent retry")
    require(refiner, "MatchesLayout(toolsItems, ToolsColumns)", "idempotent retry")
    require(refiner, "TryRestore(drawItems, originalDrawItems)", "Vẽ rollback")
    require(refiner, "TryRestore(toolsItems, originalToolsItems)", "Công cụ rollback")

    draw_start = refiner.find("private static readonly string[][] DrawColumns")
    tools_start = refiner.find("private static readonly string[][] ToolsColumns")
    initialize_start = refiner.find("public static bool TryInitialize()")
    if draw_start < 0 or tools_start < 0 or initialize_start < 0 or not (draw_start < tools_start < initialize_start):
        fail("could not isolate Draw/Công cụ layout contracts")

    draw_contract = refiner[draw_start:tools_start]
    tools_contract = refiner[tools_start:initialize_start]

    require_order(
        draw_contract,
        [
            '"QS3D_DRAW_BLT_POINT"',
            '"QS3D_DRAW_BLT_LINE"',
            '"QS3D_DRAW_BLT_TRACE"',
            '"QS3D_DRAW_BLT_ARC"',
            '"QS3D_DRAW_BLT_RECTANGLE"',
            '"QS3D_DRAW_BLT_CIRCLE"',
            '"QS3D_DRAW_BLT_BOUNDARY"',
        ],
        "owner-reference Vẽ compact columns",
    )
    if '"QS3D_DRAW_BLT_BOUNDARY"' in tools_contract:
        fail("Biên dạng must live in the Vẽ group, not Công cụ")

    require_order(
        tools_contract,
        [
            '"QS3D_DRAW_BLT_SLAB_SLOPE"',
            '"QS3D_DRAW_BLT_SLAB_CUT"',
            '"QS3D_DRAW_BLT_MOVE"',
            '"QS3D_DRAW_BLT_ROTATE"',
            '"QS3D_DRAW_BLT_MIRROR"',
            '"QS3D_DRAW_BLT_COPY"',
            '"QS3D_DRAW_BLT_BREAK"',
            '"QS3D_DRAW_BLT_JOIN"',
            '"QS3D_DRAW_BLT_DISTANCE"',
            '"QS3D_DRAW_BLT_CORNER"',
            '"QS3D_DRAW_BLT_TEE"',
        ],
        "owner-reference Công cụ compact columns",
    )

    # Every visible owner-reference button must carry an explicit semantic glyph. The icon
    # decorator is clean-room vector presentation only; it cannot alter commands or CAD logic.
    for button_id in (
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
    ):
        require(icons, f'"{button_id}"', "VẼ button icon coverage")

    for token in (
        'private const string DrawPanelSourceId = "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE";',
        'private const string ToolsPanelSourceId = "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE";',
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", CreateIcon(spec.Value));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Value));',
        'string.Equals(typeName, "RibbonRowPanel", StringComparison.Ordinal)',
        'new RectangleGeometry(new Rect(0, 0, 32, 32))',
    ):
        require(icons, token, "BLT3D-familiar vector icon contract")

    # The compact refiner remains presentation-only for Vẽ/Công cụ. IFC is a staging source
    # created by the rich augmenter so MÔ HÌNH BIM can mirror it, then the finalizer removes
    # that panel from the visible VẼ tab to match the owner screenshot's blank ribbon tail.
    if "QS3D_DRAW_BLT_IFC_PANEL_SOURCE" in refiner:
        fail("compact Draw refiner must not take ownership of the IFC staging panel")

    for token in (
        'private const string DrawTabId = "QS3D_DRAW";',
        'private const string BimTabId = "QS3D_BIM";',
        'private const string DrawIfcPanelSourceId = "QS3D_DRAW_BLT_IFC_PANEL_SOURCE";',
        'private const string BimIfcPanelSourceId = "QS3D_BIM_BLT_IFC_PANEL_SOURCE";',
        "if (FindPanelBySourceId(bimPanels, BimIfcPanelSourceId) == null)",
        "var ifcPanel = FindPanelBySourceId(drawPanels, DrawIfcPanelSourceId);",
        "if (ifcPanel == null)",
        "return true;",
        "Remove(drawPanels, ifcPanel);",
        "return FindPanelBySourceId(drawPanels, DrawIfcPanelSourceId) == null;",
    ):
        require(finalizer, token, "final visible VẼ panel contract")

    require_order(
        init,
        (
            "BltDrawRibbonFailSafe.TryInitialize()",
            "BltDrawRibbonReferenceIconDecorator.TryInitialize()",
            "BltBimRibbonMirrorAugmenter.TryInitialize()",
            "BltDrawRibbonReferenceFinalizer.TryInitialize()",
        ),
        "VẼ layout/icon/BIM/finalization lifecycle",
    )

    require(
        fail_safe,
        "if (BltDrawRibbonLayoutRefiner.TryInitialize())",
        "Draw fail-safe integration",
    )
    require(
        fail_safe,
        "BltDrawRibbonAugmenter.Reset();",
        "retry after compact-layout failure",
    )
    require_order(
        fail_safe,
        [
            "BltDrawRibbonAugmenter.TryInitialize()",
            "BltDrawRibbonLayoutRefiner.TryInitialize()",
            "BltDrawRibbonAugmenter.Reset();",
            "RestoreFallback(fallback);",
        ],
        "Draw refinement fail-safe lifecycle",
    )

    print(
        "PASS: QS3D VẼ matches the BLT3D compact Vẽ/Công cụ source contract, gives every visible "
        "button an explicit vector icon, removes the IFC staging panel only after BIM mirroring, "
        "and preserves fail-safe recovery."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())