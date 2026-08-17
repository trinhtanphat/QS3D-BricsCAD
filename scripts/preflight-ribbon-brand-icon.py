#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FACTORY = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "Qs3dBrandIconFactory.cs"
BOOTSTRAP = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapIconAugmenter.cs"
MASTER = ROOT / "assets" / "branding" / "qs3d-logo.svg"


def require(text, needle, label):
    if needle not in text:
        raise RuntimeError("missing " + label + ": " + needle)


def forbid(text, needle, label):
    if needle in text:
        raise RuntimeError("stale " + label + " remains: " + needle)


def main():
    factory = FACTORY.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
    master = MASTER.read_text(encoding="utf-8")

    # Canonical master identity and exact factory palette must stay aligned.
    require(master, 'stroke="#E84A4A"', "master red-X color")
    require(master, 'stroke="#52BE6C"', "master green-V color")
    require(factory, "Color.FromRgb(232, 74, 74)", "Ribbon red-X color")
    require(factory, "Color.FromRgb(82, 190, 108)", "Ribbon green-V color")
    require(factory, 'Geometry.Parse("M6.5,8 L13.5,19 M13.5,8 L6.5,19")', "red-X geometry")
    require(factory, 'Geometry.Parse("M17,14 L21,19 L27,8")', "green-V geometry")

    # Preserve the runtime-safe bitmap path and locale guard that BricsCAD depends on.
    require(factory, "Thread.CurrentThread", "host culture preservation")
    require(factory, "CultureInfo.InvariantCulture", "invariant geometry parsing")
    require(factory, "RenderTargetBitmap", "BricsCAD bitmap rendering")
    require(factory, "image.Freeze();", "frozen Ribbon bitmap")

    # Prove the fallback/start-center brand route still reaches this factory.
    require(bootstrap, "return Qs3dBrandIconFactory.Create(pixelSize);", "fallback brand factory wiring")
    require(bootstrap, "return RibbonIconKind.Qs3dLogo;", "branded fallback selection")

    # The previous blue/cyan cube palette is the defect this lane removes from the brand factory.
    forbid(factory, "Color.FromRgb(22, 139, 255)", "blue cube brand palette")
    forbid(factory, "Color.FromRgb(51, 197, 255)", "cyan cube brand palette")

    print("PASS: Ribbon fallback brand icon matches the QS3D red-X / green-V identity.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
