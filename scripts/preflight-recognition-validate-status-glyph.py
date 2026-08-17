#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
POLISHER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionIconPolisher.cs"
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionRibbonAugmenter.cs"
FINALIZER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionBitmapFinalizer.cs"
INIT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def section(text: str, start: str, end: str, label: str) -> str:
    begin = text.find(start)
    if begin < 0:
        raise SystemExit(f"FAIL: {label} missing start marker: {start}")
    finish = text.find(end, begin + len(start))
    if finish < 0:
        raise SystemExit(f"FAIL: {label} missing end marker: {end}")
    return text[begin:finish]


def spec_line(text: str, button_id: str) -> str:
    marker = f'"{button_id}"'
    pos = text.find(marker)
    if pos < 0:
        raise SystemExit(f"FAIL: {button_id} missing from Recognition Ribbon surface")
    start = text.rfind("\n", 0, pos) + 1
    end = text.find("\n", pos)
    if end < 0:
        end = len(text)
    return text[start:end]


def main() -> int:
    polisher = POLISHER.read_text(encoding="utf-8")
    ribbon = RIBBON.read_text(encoding="utf-8")
    finalizer = FINALIZER.read_text(encoding="utf-8")
    init = INIT.read_text(encoding="utf-8")

    require(polisher, "var statusRed = FrozenBrush(Color.FromRgb(224, 62, 62));", "red validation status brush")
    require(polisher, "var statusGreen = FrozenBrush(Color.FromRgb(55, 176, 90));", "green validation status brush")

    validate = section(polisher, "case IconKind.Validate:", "break;", "Validate icon block")
    require(validate, 'Stroke(statusRed, 3.0, Geometry.Parse(', "red X stroke")
    require(validate, '"M8,11 L14,17 M14,11 L8,17"', "red X geometry")
    require(validate, 'Stroke(statusGreen, 3.0, Geometry.Parse(', "green V/check stroke")
    require(validate, '"M17,18 L21,22 L27,12"', "green V/check geometry")
    require(validate, 'Stroke(neutral, 1.6, Geometry.Parse(', "Recognition target-frame cue")

    for legacy in (
        "Neutral inspection sheet + check mark",
        'Stroke(graphite, 2.4, Geometry.Parse("M10,20 L14,24 L23,14"))',
        "new EllipseGeometry(new Point(25, 7), 2.2, 2.2)",
    ):
        if legacy in validate:
            raise SystemExit(f"FAIL: Validate icon retained legacy neutral-only artwork: {legacy}")

    disabled_line = spec_line(ribbon, "QS3D_RECOGNIZE_BLT_VALIDATE")
    if "enabled: false" not in disabled_line:
        raise SystemExit("FAIL: owner-requested artwork fix must not invent a clickable validation workflow")

    require(finalizer, "private const int ExpectedButtonCount = 8;", "eight-button host finalizer")
    require(finalizer, "var smallBitmap = Rasterize(source, 16);", "16px host bitmap")
    require(finalizer, "var largeBitmap = Rasterize(source, 32);", "32px host bitmap")
    require(finalizer, "PixelFormats.Pbgra32", "BricsCAD bitmap pixel format")
    require(finalizer, "rendered.Freeze();", "frozen host bitmap")

    polish_pos = init.find("BltRecognitionIconPolisher.TryInitialize()")
    finalize_pos = init.find("BltRecognitionBitmapFinalizer.TryInitialize()")
    if polish_pos < 0 or finalize_pos < 0 or finalize_pos <= polish_pos:
        raise SystemExit("FAIL: Recognition bitmap finalizer must run after semantic icon polish")

    print(
        "PASS: NHẬN DẠNG validation artwork carries the owner-requested red X + green V/check pair, "
        "stays non-clickable, and still reaches BricsCAD as exact frozen 16px/32px bitmaps."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
