#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonReferenceIconDecorator.cs"

EXPECTED_IDS = (
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


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    if not SOURCE.is_file():
        return fail("missing Draw ribbon icon decorator: " + str(SOURCE.relative_to(ROOT)))

    text = SOURCE.read_text(encoding="utf-8")

    missing_ids = [icon_id for icon_id in EXPECTED_IDS if text.count('"' + icon_id + '"') != 1]
    if missing_ids:
        return fail("Draw icon map must contain each expected command exactly once: " + ", ".join(missing_ids))

    if len(re.findall(r"new KeyValuePair<string, IconKind>\(\"QS3D_DRAW_BLT_[A-Z0-9_]+\"", text)) != len(EXPECTED_IDS):
        return fail("Draw icon map cardinality changed; review visual parity before changing the 18-command contract")

    required = (
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", CreateIcon(spec.Value, 16));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Value, 32));',
        'thread.CurrentCulture = CultureInfo.InvariantCulture;',
        'thread.CurrentCulture = previousCulture;',
        'new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32)',
        'drawing.PushTransform(new ScaleTransform(pixelSize / 32.0, pixelSize / 32.0));',
        'drawing.DrawDrawing(group);',
        'image.Render(visual);',
    )
    missing = [token for token in required if token not in text]
    if missing:
        return fail("Draw ribbon bitmap/icon safety contract is incomplete: " + " | ".join(missing))

    forbidden = (
        'SetProperty(button, "Image", CreateIcon(spec.Value));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Value));',
        'return new DrawingImage(group);',
    )
    present = [token for token in forbidden if token in text]
    if present:
        return fail("Draw ribbon regressed to raw DrawingImage host assignment: " + " | ".join(present))

    if "BLT3D-familiar" not in text or "clean-room" not in text:
        return fail("Draw icon source must retain the clean-room/reference-only product-boundary note")

    print("PASS: VẼ/Công cụ keeps 18 clean-room semantic glyphs, 16/32 px bitmap-backed BricsCAD images, and invariant-culture vector construction.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
