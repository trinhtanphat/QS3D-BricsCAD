#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimRibbonMirrorAugmenter.cs"


def require(text, needle, label):
    if needle not in text:
        raise SystemExit("FAIL: " + label + " was not found in " + str(SOURCE.relative_to(ROOT)))


def main():
    text = SOURCE.read_text(encoding="utf-8")

    require(
        text,
        'new PanelMirrorSpec("QS3D_DRAW_BLT_IFC_PANEL_SOURCE", "QS3D_BIM_BLT_IFC_PANEL_SOURCE", true)',
        "IFC mirror rasterization opt-in",
    )
    require(text, 'CopyRasterizedImageProperty(source, target, "Image", 16);', "small IFC raster icon copy")
    require(text, 'CopyRasterizedImageProperty(source, target, "LargeImage", 32);', "large IFC raster icon copy")
    require(text, "new RenderTargetBitmap(pixels, pixels, 96.0, 96.0, PixelFormats.Pbgra32)", "WPF raster bitmap creation")
    require(text, "bitmap.Render(visual);", "raster rendering")
    require(text, "if (bitmap.CanFreeze)", "raster freeze guard")
    require(text, "bitmap.Freeze();", "frozen raster output")
    require(text, "CloneRibbonItem(sourceItem, ref buttonCount, rasterizeImages)", "nested IFC rasterization propagation")

    if '"Size", "Image", "LargeImage", "IsEnabled"' in text:
        raise SystemExit("FAIL: mirrored images are still blindly copied in the presentation-property loop")

    print("PASS: BIM IFC mirror rasterizes 16px/32px images and freezes the host-facing bitmap.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
