#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REFINER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonLayoutRefiner.cs"
FAIL_SAFE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonFailSafe.cs"


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
    if not REFINER.is_file():
        fail("missing BltDrawRibbonLayoutRefiner.cs")
    if not FAIL_SAFE.is_file():
        fail("missing BltDrawRibbonFailSafe.cs")

    refiner = REFINER.read_text(encoding="utf-8")
    fail_safe = FAIL_SAFE.read_text(encoding="utf-8")

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

    # IFC remains the qualified rich panel produced by BltDrawRibbonAugmenter. This refiner
    # is intentionally limited to layout of Vẽ/Công cụ and must not mutate IFC ownership.
    if "QS3D_DRAW_BLT_IFC_PANEL_SOURCE" in refiner:
        fail("compact Draw refiner must not take ownership of the IFC panel")

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
        "PASS: QS3D VẼ uses BLT3D-familiar compact three-row columns, keeps Biên dạng in Vẽ, "
        "preserves Công cụ order, leaves IFC ownership alone, and rolls back safely on host mismatch."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
