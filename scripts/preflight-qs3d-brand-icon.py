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

    # The repository/product asset returns to the independent pre-#2610 QS3D cube identity. Agent
    # progress/status shorthand must not define product pixels.
    for token in (
        "QS3D CAD",
        "QS3D product family isometric precision cube mark",
        "#061323",
        "#0A2340",
        "#00C8FF",
        "#168BFF",
        "#2457FF",
        "#33C5FF",
        "#83DAFF",
        'd="M256 92 388 168 256 244 124 168Z"',
        'd="M124 168v152l132 78V244M388 168v152l-132 78"',
    ):
        require(asset, token, "repository QS3D cube branding asset")
    for stale in (
        "QS3D original red X and green V BIM/CAD product mark",
        "#E84A4A",
        "#52BE6C",
        'd="M108 122 222 282M222 122 108 282"',
        'd="M270 207 329 286 422 124"',
    ):
        forbid(asset, stale, "repository QS3D cube branding asset")

    # Preserve the separate repository-owned compact Ribbon fallback glyph.
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
        "PASS: repository branding uses the independent QS3D cube mark, the system Ribbon action "
        "keeps its repository-owned compact fallback glyph, unknown QS3D command buttons cannot "
        "regress to the generic Objects placeholder, and BricsCAD host/icon ownership remains untouched."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
