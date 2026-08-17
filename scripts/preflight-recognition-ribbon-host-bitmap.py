#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionBitmapFinalizer.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
POLISHER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionIconPolisher.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def main() -> int:
    for path in (FINALIZER, COORDINATOR, POLISHER):
        if not path.is_file():
            raise SystemExit(f"FAIL: missing Recognition Ribbon source: {path.relative_to(ROOT)}")

    finalizer = FINALIZER.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")
    polisher = POLISHER.read_text(encoding="utf-8")

    # Keep the screenshot-derived semantic artwork in the clean-room polisher. The host
    # compatibility layer must rasterize that final result rather than replacing it with a
    # generic logo/placeholder or changing command ownership.
    require(polisher, "internal static class BltRecognitionIconPolisher", "semantic icon polisher")
    require(polisher, 'RecognitionTabId = "QS3D_RECOGNIZE"', "recognition tab ownership")

    for button_id in (
        "QS3D_RECOGNIZE_BLT_RESTORE",
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_OPTIONS",
        "QS3D_RECOGNIZE_BLT_TABLE",
        "QS3D_RECOGNIZE_BLT_BOUNDARY",
        "QS3D_RECOGNIZE_BLT_LABEL",
        "QS3D_RECOGNIZE_BLT_AUTO",
        "QS3D_RECOGNIZE_BLT_VALIDATE",
    ):
        require(finalizer, f'case "{button_id}":', f"host bitmap coverage {button_id}")

    for token in (
        "System.Windows.Media.Imaging",
        "RenderTargetBitmap",
        "PixelFormats.Pbgra32",
        "rendered.Freeze();",
        'SetProperty(item, "Image", smallBitmap)',
        'SetProperty(item, "LargeImage", largeBitmap)',
        'HasExactBitmap(item, "Image", 16)',
        'HasExactBitmap(item, "LargeImage", 32)',
        "finalized == ExpectedButtonCount",
    ):
        require(finalizer, token, "BricsCAD host-safe Recognition bitmap finalizer")

    polish_call = "ready = BltRecognitionIconPolisher.TryInitialize() && ready;"
    bitmap_call = "ready = BltRecognitionBitmapFinalizer.TryInitialize() && ready;"
    require(coordinator, polish_call, "Recognition semantic polish ordering")
    require(coordinator, bitmap_call, "Recognition bitmap finalization ordering")
    if coordinator.find(bitmap_call) <= coordinator.find(polish_call):
        raise SystemExit("FAIL: Recognition bitmap finalizer must run after semantic icon polish")

    if "new DrawingImage" in finalizer:
        raise SystemExit("FAIL: host-facing Recognition finalizer must not return raw DrawingImage")

    print(
        "PASS: all eight NHẬN DẠNG buttons preserve clean-room semantic artwork and finish as "
        "exact 16px/32px frozen BricsCAD Ribbon bitmaps after icon polish."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
