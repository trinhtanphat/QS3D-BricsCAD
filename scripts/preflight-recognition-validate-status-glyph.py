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


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL: {label} contains status-derived contract: {needle}")


def section(text: str, start: str, end: str, label: str) -> str:
    begin = text.find(start)
    if begin < 0:
        raise SystemExit(f"FAIL: {label} missing start marker: {start}")
    finish = text.find(end, begin + len(start))
    if finish < 0:
        raise SystemExit(f"FAIL: {label} missing end marker: {end}")
    return text[begin:finish]


def spec_block(text: str, button_id: str) -> str:
    marker = f'"{button_id}"'
    pos = text.find(marker)
    if pos < 0:
        raise SystemExit(f"FAIL: {button_id} missing from Recognition Ribbon surface")
    start = text.rfind("new RecognitionButtonSpec", 0, pos)
    if start < 0:
        raise SystemExit(f"FAIL: {button_id} missing RecognitionButtonSpec start")
    end = text.find(")", pos)
    if end < 0:
        raise SystemExit(f"FAIL: {button_id} missing RecognitionButtonSpec end")
    return text[start:end + 1]


def main() -> int:
    polisher = POLISHER.read_text(encoding="utf-8")
    ribbon = RIBBON.read_text(encoding="utf-8")
    finalizer = FINALIZER.read_text(encoding="utf-8")
    init = INIT.read_text(encoding="utf-8")

    # Validate returns to the neutral, disabled inspection/check semantic that existed before the
    # status-marker wording was incorrectly turned into product artwork.
    validate = section(polisher, "case IconKind.Validate:", "break;", "Validate icon block")
    for token in (
        "Neutral inspection sheet + check mark",
        "Fill(paper, new RectangleGeometry(new Rect(6, 5, 19, 22), 1.8, 1.8))",
        "Stroke(graphite, 1.8, new RectangleGeometry(new Rect(6, 5, 19, 22), 1.8, 1.8))",
        'Stroke(neutral, 1.5, Geometry.Parse("M10,10 H21 M10,14 H18"))',
        'Stroke(graphite, 2.4, Geometry.Parse("M10,20 L14,24 L23,14"))',
        "new EllipseGeometry(new Point(25, 7), 2.2, 2.2)",
    ):
        require(validate, token, "neutral Validate artwork")

    # Reject the historical status-derived geometry, identifiers, and their exact saturated RGB
    # values so a future refactor cannot reintroduce the same product cue merely by inlining colors.
    for stale in (
        "statusRed",
        "statusGreen",
        "Color.FromRgb(224, 62, 62)",
        "Color.FromRgb(55, 176, 90)",
        "M8,11 L14,17 M14,11 L8,17",
        "M17,18 L21,22 L27,12",
        "red X",
        "green V",
    ):
        forbid(polisher, stale, "Recognition semantic icon source")

    # Guard the initial/fallback Ribbon image as well as the semantic polisher. The fallback must
    # remain an ordinary neutral inspection/check glyph so initialization order or a future host
    # failure cannot re-expose saturated status-derived colors before the polisher/finalizer runs.
    ribbon_validate = section(ribbon, "case IconKind.Validate:", "break;", "Ribbon Validate fallback block")
    for token in (
        "Stroke(ink, 2.2, new RectangleGeometry(new System.Windows.Rect(5, 6, 21, 20), 2, 2))",
        'Stroke(neutral, 2.4, Geometry.Parse("M9,16 L14,21 23,11"))',
        "Fill(neutral, new EllipseGeometry(new System.Windows.Point(26, 7), 3.0, 3.0))",
    ):
        require(ribbon_validate, token, "neutral Validate Ribbon fallback")
    for stale in (
        "statusRed",
        "statusGreen",
        "M8,11 L14,17 M14,11 L8,17",
        "M17,18 L21,22 L27,12",
        "red X",
        "green V",
        "Stroke(accentDark",
        "Stroke(accent,",
        "Fill(warning",
        "Color.FromRgb(14, 79, 170)",
        "Color.FromRgb(32, 137, 245)",
        "Color.FromRgb(224, 72, 72)",
    ):
        forbid(ribbon_validate, stale, "neutral Validate Ribbon fallback")

    require(ribbon, "var neutral = FrozenBrush(Color.FromRgb(154, 164, 174));", "neutral fallback palette")

    validate_spec = spec_block(ribbon, "QS3D_RECOGNIZE_BLT_VALIDATE")
    require(validate_spec, "string.Empty", "Validate remains without a command")
    require(validate_spec, "enabled: false", "Validate executable-command authority stays disabled")
    forbid(ribbon, "PreserveSourceColorWhenNonInteractive", "Recognition Ribbon presentation")
    forbid(ribbon, "preserveSourceColorWhenNonInteractive", "Recognition Ribbon presentation")
    require(ribbon, 'SetProperty(button, "IsEnabled", spec.Enabled);', "host disabled presentation follows executable authority")
    require(ribbon, "if (spec.Enabled && !string.IsNullOrWhiteSpace(spec.Command))", "command routing remains gated by executable authority")

    for button_id in ("QS3D_RECOGNIZE_BLT_TEXT", "QS3D_RECOGNIZE_BLT_TABLE"):
        disabled_spec = spec_block(ribbon, button_id)
        require(disabled_spec, "enabled: false", f"{button_id} existing disabled hierarchy")

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
        "PASS: NHẬN DẠNG Validate uses neutral disabled inspection/check artwork in both fallback "
        "and semantic paths, remains without a command, follows ordinary host disabled presentation, "
        "and still reaches BricsCAD through the frozen 16px/32px bitmap finalization path."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
