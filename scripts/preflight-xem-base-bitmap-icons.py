from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltViewRibbonAugmenter.cs"

text = SOURCE.read_text(encoding="utf-8")

required = (
    "using System.Windows.Media.Imaging;",
    'SetProperty(button, "Image", CreateIcon(spec.Icon, 16));',
    'SetProperty(button, "LargeImage", CreateIcon(spec.Icon, 32));',
    "new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32)",
    "bitmap.Render(visual);",
    "bitmap.Freeze();",
)

missing = [token for token in required if token not in text]
if missing:
    raise SystemExit(
        "XEM base ribbon bitmap icon preflight failed; missing: "
        + ", ".join(missing)
    )

if "return new DrawingImage(" in text:
    raise SystemExit(
        "XEM base ribbon bitmap icon preflight failed; direct DrawingImage return "
        "can regress to BricsCAD question-mark placeholders."
    )

print("XEM base ribbon bitmap icon preflight OK")
