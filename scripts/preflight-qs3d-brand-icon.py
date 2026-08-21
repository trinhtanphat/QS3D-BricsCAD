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

    # Preserve the separate repository-owned compact Ribbon brand glyph for explicit identity.
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
        require(factory, token, "QS3D compact ribbon brand mark")

    # Functional Home actions must use semantic glyphs; the product mark is not a substitute for
    # a missing icon. System Objects intentionally gets the distinct 3D/model glyph rather than
    # the generic Objects fallback called out in the owner screenshots.
    require(
        home,
        'new HomeButtonSpec("QS3D_HOME_SYSTEM_OBJECTS", "Đối tượng\\nhệ thống", () => new FamilyManagerCommands().ShowFamilyManager(), RibbonIconKind.Model3d)',
        "QS3D system-object semantic icon",
    )
    for token in (
        "RibbonIconKind.Qs3dLogo",
        "? Qs3dBrandIconFactory.Create(16)",
        "? Qs3dBrandIconFactory.Create(32)",
        "Functional actions such as System Objects must select a semantic icon instead.",
    ):
        require(home, token, "explicit-only Home brand rendering")

    for token in (
        "if (icon == RibbonIconKind.Qs3dLogo)",
        "return Qs3dBrandIconFactory.Create(pixelSize);",
        '"QS3DSTART"',
        "return RibbonIconKind.Qs3dLogo;",
        "return RibbonIconKind.Objects;",
        "Do not turn a missing mapping into product branding.",
        "ApplySemanticIcon(item, RibbonIconKind.Model3d, makeLarge: true);",
        "ApplySemanticIcon(item, RibbonIconKind.Inspect, makeLarge: true);",
        "CreateIfcRemoveIcon(32)",
    ):
        require(bootstrap, token, "canonical Ribbon brand/fallback separation")

    # Exactly one resolver outcome may select the product mark: the explicit QS3D start/product
    # identity surface. This prevents a second branded functional fallback from silently returning.
    brand_outcome = "return RibbonIconKind.Qs3dLogo;"
    if bootstrap.count(brand_outcome) != 1:
        fail(
            "canonical Ribbon bootstrap must contain exactly one Qs3dLogo resolver outcome "
            "for explicit product identity"
        )
    if bootstrap.rfind("return RibbonIconKind.Objects;") < bootstrap.rfind("return RibbonIconKind.Draw;"):
        fail("canonical Ribbon neutral Objects fallback must remain after semantic mappings")

    for forbidden in (
        "ApplicationIcon",
        "SetApplicationIcon",
        "MainWindow.Icon",
        "RibbonPaletteSet.Icon",
    ):
        if forbidden in factory or forbidden in home:
            fail(f"QS3D ribbon branding must not replace BricsCAD host icon: {forbidden}")

    print(
        "PASS: repository branding keeps the independent QS3D cube mark for explicit identity, "
        "System Objects uses a distinct Model3d glyph, Project Info uses Inspect, IFC remove has a destructive glyph, "
        "unknown functional Ribbon commands use a neutral fallback instead of product branding, and BricsCAD host/icon ownership is untouched."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())