#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ICONS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionIconPolisher.cs"
INIT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def main() -> int:
    icons = ICONS.read_text(encoding="utf-8")
    init = INIT.read_text(encoding="utf-8")
    v26 = V26.read_text(encoding="utf-8")

    require(icons, 'RecognitionTabId = "QS3D_RECOGNIZE"', "recognition tab ownership")
    require(icons, "if (polished != 8)", "exact eight-button icon coverage")
    require(icons, 'SetProperty(item, "Image", image)', "compact Image assignment")
    require(icons, 'SetProperty(item, "LargeImage", image)', "LargeImage fallback assignment")
    require(icons, 'SetProperty(item, "ShowImage", true)', "icon visibility")
    require(icons, "ClipGeometry = new RectangleGeometry(new Rect(0, 0, 32, 32))", "bounded transparent vector canvas")
    require(icons, "PenLineCap.Round", "16px-friendly rounded stroke caps")
    require(icons, "PenLineJoin.Round", "16px-friendly rounded stroke joins")

    button_ids = (
        "QS3D_RECOGNIZE_BLT_RESTORE",
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_OPTIONS",
        "QS3D_RECOGNIZE_BLT_TABLE",
        "QS3D_RECOGNIZE_BLT_BOUNDARY",
        "QS3D_RECOGNIZE_BLT_LABEL",
        "QS3D_RECOGNIZE_BLT_AUTO",
        "QS3D_RECOGNIZE_BLT_VALIDATE",
    )
    for button_id in button_ids:
        require(icons, f'case "{button_id}":', f"semantic icon target {button_id}")

    for kind in (
        "Restore",
        "Text",
        "Options",
        "Table",
        "Boundary",
        "Label",
        "Auto",
        "Validate",
    ):
        require(icons, f"case IconKind.{kind}:", f"distinct {kind} artwork")

    # Keep each semantic glyph visibly distinct instead of regressing to one generic icon.
    for marker, label in (
        ("M4,10 V4 H10", "restore selection frame"),
        ("M9,24 L15,8 H18 L24,24", "text recognition glyph"),
        ("M6,9 H26 M6,16 H26 M6,23 H26", "options sliders"),
        ("new Rect(5, 6, 22, 20)", "element table grid"),
        ("M6,23 L7,10 L16,6 L27,12 L24,25 Z", "boundary polyline"),
        ("M5,9 L18,9 L27,16 L18,24 L5,24 Z", "label tag"),
        ("M25,3 L27,8 L31,10", "auto recognition sparkle"),
        ("M10,20 L14,24 L23,14", "validation check"),
    ):
        require(icons, marker, label)

    require(init, "BltRecognitionIconPolisher.Reset()", "icon-polisher lifecycle reset")
    require(init, "BltRecognitionIconPolisher.TryInitialize()", "icon-polisher initialization")

    bootstrap_pos = init.find("RibbonBootstrapIconAugmenter.TryInitialize()")
    polish_pos = init.find("BltRecognitionIconPolisher.TryInitialize()")
    command_pos = init.find("RibbonCommandParameterFallback.TryInitialize()")
    if min(bootstrap_pos, polish_pos, command_pos) < 0 or not (bootstrap_pos < polish_pos < command_pos):
        raise SystemExit(
            "FAIL: Recognition icon polish must run after generic icon decoration and before command fallback"
        )

    require(v26, r'<Compile Include="..\QS3D.BricsCAD.V25\**\*.cs"', "V26 linked V25 Ribbon source")

    print(
        "PASS: NHẬN DẠNG keeps eight distinct BLT3D-familiar semantic vector icons, "
        "assigns Image/LargeImage after generic decoration, and shares the source with V25/V26."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
