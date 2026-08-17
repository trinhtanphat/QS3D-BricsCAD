#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / "assets" / "branding" / "qs3d-logo.svg"
FACTORY = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "Qs3dBrandIconFactory.cs"
HOME = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltHomeRibbonAugmenter.cs"
BOOTSTRAP = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapIconAugmenter.cs"


def fail(message: str) -> None:
    print("ERROR:", message)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        fail(f"{label}: forbidden source contract found: {needle}")


def main() -> int:
    for path in (ASSET, FACTORY, HOME, BOOTSTRAP):
        if not path.is_file():
            fail(f"missing required branding source: {path.relative_to(ROOT)}")

    asset = ASSET.read_text(encoding="utf-8")
    factory = FACTORY.read_text(encoding="utf-8")
    home = HOME.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")

    # Canonical repository/product branding is the independently-authored QS3D red-X / green-V
    # identity. Keep this guard strict: the two marks, their colors, and the product description
    # are all required so a future branding edit cannot silently regress to a generic placeholder.
    for token in (
        "QS3D CAD",
        "QS3D original red X and green V BIM/CAD product mark",
        "#E84A4A",
        "#52BE6C",
        'd="M108 122 222 282M222 122 108 282"',
        'd="M270 207 329 286 422 124"',
    ):
        require(asset, token, "repository red-X / green-V branding asset")

    # The compact ribbon fallback remains a separate repository-owned QS3D glyph. This contract is
    # intentionally preserved while the shell/repository product identity moves to red-X / green-V.
    for token in (
        "assets/branding/qs3d-logo.svg",
        "internal static class Qs3dBrandIconFactory",
        "Brush(Color.FromRgb(6, 19, 35))",
        "Brush(Color.FromRgb(234, 247, 255))",
        "Brush(Color.FromRgb(22, 139, 255))",
        "Brush(Color.FromRgb(51, 197, 255))",
        'Geometry.Parse("M16,5.75 L24.25,10.5 16,15.25 7.75,10.5 Z")',
        "image.Freeze();",
    ):
        require(factory, token, "QS3D compact ribbon fallback mark")

    for token in (
        'new HomeButtonSpec("QS3D_HOME_SYSTEM_OBJECTS", "Đối tượng\\nhệ thống"',
        "RibbonIconKind.Qs3dLogo",
        "? Qs3dBrandIconFactory.Create(16)",
        "? Qs3dBrandIconFactory.Create(32)",
        "BricsCAD's host/application icon is",
    ):
        require(home, token, "QS3D-owned system ribbon branding")

    # The late bootstrap decorator must never regress an unclassified QS3D action back to the
    # old generic four-dot Objects placeholder. Rich buttons keep their own semantic images and
    # truly unknown command-bearing buttons use the repository-owned compact QS3D fallback mark.
    for token in (
        "if (icon == RibbonIconKind.Qs3dLogo)",
        "return Qs3dBrandIconFactory.Create(pixelSize);",
        "return RibbonIconKind.Qs3dLogo;",
    ):
        require(bootstrap, token, "canonical Ribbon brand fallback")
    forbid(bootstrap, "return RibbonIconKind.Objects;", "canonical Ribbon brand fallback")

    for forbidden in (
        "ApplicationIcon",
        "SetApplicationIcon",
        "MainWindow.Icon",
        "RibbonPaletteSet.Icon",
    ):
        if forbidden in factory or forbidden in home:
            fail(f"QS3D ribbon branding must not replace BricsCAD host icon: {forbidden}")

    print(
        "PASS: canonical repository branding keeps the original QS3D red-X / green-V product mark, "
        "the system Ribbon action keeps its repository-owned compact fallback glyph, unknown QS3D "
        "command buttons cannot regress to the generic Objects placeholder, and BricsCAD host/icon "
        "ownership remains untouched."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
