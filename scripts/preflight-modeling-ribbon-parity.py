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
    modeling = read("src/QS3D.BricsCAD.V25/Ribbon/BltModelingRibbonAugmenter.cs")
    visual = read("src/QS3D.BricsCAD.V25/Ribbon/BltModelingRibbonVisualRefiner.cs")
    init = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
    v26 = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")

    # Owner-reference topbar content: exactly the eight visible MODELING groups in screenshot order.
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
        "MODELING panel order",
    )

    # Large lead actions and compact stacked actions must keep the exact visible captions.
    for token in (
        '"Vật\\nliệu"',
        '"Mặt cắt\\nthép"',
        '"Tạo chi\\ntiết"',
        '"Mặt\\nXY"',
        'Standard("LINE", "Đường"',
        'Standard("POLYLINE", "Polyline"',
        'Standard("RECTANGLE", "Chữ nhật"',
        'Standard("CIRCLE", "Tròn"',
        'Standard("ARC", "Cung"',
        'Standard("JOIN_POLYLINE", "Nối polyline"',
        'Standard("OFFSET", "Offset"',
        'Standard("MOVE", "Di chuyển"',
        'Standard("COPY", "Sao chép"',
        'Standard("MOVE_Z", "Theo phương Z"',
        'Standard("EXTRUDE", "Extrude"',
        'Standard("SWEEP", "Sweep"',
        'Standard("LOFT", "Loft"',
        'Standard("ATTACH_FAMILY", "Gắn vào Family"',
        'Standard("UNION", "Union"',
        'Standard("SUBTRACT", "Subtract"',
        'Standard("INTERSECT", "Intersect"',
        '"Bricscad.Windows.RibbonRowPanel"',
        '"Bricscad.Windows.RibbonRowBreak"',
    ):
        require(modeling, token, "MODELING visible layout")

    # The base augmenter must already expose artwork; the final refiner is not allowed to rescue
    # missing command wiring or synthesize extra buttons.
    for token in (
        'SetProperty(button, "ShowText", true);',
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", icon);',
        'SetProperty(button, "LargeImage", icon);',
        'spec.Large ? "Large" : "Standard"',
    ):
        require(modeling, token, "MODELING base icon contract")

    expected_ids = (
        "MATERIAL",
        "STEEL_PROFILE",
        "CREATE_DETAIL",
        "PLANE_XY",
        "LINE",
        "POLYLINE",
        "RECTANGLE",
        "CIRCLE",
        "ARC",
        "JOIN_POLYLINE",
        "OFFSET",
        "MOVE",
        "COPY",
        "MOVE_Z",
        "EXTRUDE",
        "SWEEP",
        "LOFT",
        "ATTACH_FAMILY",
        "UNION",
        "SUBTRACT",
        "INTERSECT",
    )
    for suffix in expected_ids:
        require(visual, f'ButtonPrefix + "{suffix}"', f"MODELING icon mapping {suffix}")

    # There are 21 visible reference actions. Initialization must fail closed if even one is absent
    # or duplicated, so the final surface cannot silently regress to text-only/fallback artwork.
    for token in (
        'private const string ModelingTabId = "QS3D_MODELING";',
        'private const string ButtonPrefix = "QS3D_MODELING_BLT_";',
        'if (buttons.Count != ExpectedIcons.Count)',
        'if (!buttons.TryGetValue(expected.Key, out var button))',
        'if (result.ContainsKey(id))',
        'SetProperty(button, "ShowText", true);',
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", icon);',
        'SetProperty(button, "LargeImage", icon);',
        'if (!(GetProperty(button, "Image") is ImageSource)',
        '|| !(GetProperty(button, "LargeImage") is ImageSource))',
    ):
        require(visual, token, "MODELING final image contract")

    # The screenshot uses four large lead buttons; every remaining action is compact/standard.
    for suffix in ("MATERIAL", "STEEL_PROFILE", "CREATE_DETAIL", "PLANE_XY"):
        require(visual, f'ButtonPrefix + "{suffix}"', f"MODELING large action {suffix}")
    require(visual, 'LargeButtons.Contains(expected.Key) ? "Large" : "Standard"', "MODELING size split")

    # Vector artwork is intentionally optimized for the dark Ribbon: fixed 32x32 logical bounds,
    # blue + light-outline palette, no copied bitmap/binary BLT3D asset, and no red subtract glyph.
    for token in (
        'Color.FromRgb(35, 132, 242)',
        'Color.FromRgb(15, 82, 178)',
        'Color.FromRgb(111, 184, 255)',
        'Color.FromRgb(205, 214, 225)',
        'new RectangleGeometry(new Rect(0, 0, 32, 32))',
        'var image = new DrawingImage(group);',
        'image.Freeze();',
        'BooleanMode.Subtract',
        'BooleanMode.Intersect',
    ):
        require(visual, token, "MODELING vector artwork")
    if "Color.FromRgb(224, 69, 69)" in visual:
        fail("MODELING final artwork must not reintroduce the old red subtract accent")

    # Lifecycle order is important: build the command surface first, then replace only its visual
    # artwork, then let the generic fallback icon pass fill unrelated genuine gaps.
    require_order(
        init,
        (
            "BltModelingRibbonAugmenter.TryInitialize()",
            "BltModelingRibbonVisualRefiner.TryInitialize()",
            "BltBimRibbonMirrorAugmenter.TryInitialize()",
            "RibbonBootstrapIconAugmenter.TryInitialize()",
        ),
        "MODELING initialization order",
    )
    require(init, "BltModelingRibbonVisualRefiner.Reset();", "MODELING visual teardown")

    # V26 links every V25 C# source file, so this same visual contract is compiled into both host lanes.
    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared Ribbon source")

    print("PASS: MODELING BLT3D topbar layout + 21-button vector icon contract is intact.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
