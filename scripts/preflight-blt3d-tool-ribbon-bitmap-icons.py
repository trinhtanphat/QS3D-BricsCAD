#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltToolRibbonAugmenter.cs"
POLISHER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltToolRibbonIconPolisher.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def read_required(path: Path, errors: list[str]) -> str:
    if not path.is_file():
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, errors: list[str], label: str) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle}")


def forbid(text: str, needle: str, errors: list[str], label: str) -> None:
    if needle in text:
        errors.append(f"forbidden {label}: {needle}")


def require_order(text: str, needles: list[str], errors: list[str], label: str) -> None:
    positions = [text.find(needle) for needle in needles]
    if any(position < 0 for position in positions):
        errors.append(f"cannot validate {label}; at least one anchor is missing")
        return
    if positions != sorted(positions):
        errors.append(f"wrong {label}: {needles}")


def main() -> int:
    errors: list[str] = []
    ribbon = read_required(RIBBON, errors)
    polisher = read_required(POLISHER, errors)
    coordinator = read_required(COORDINATOR, errors)
    v26 = read_required(V26, errors)

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    # The existing clean-room TOOL artwork remains the semantic source. The regression is
    # specifically that host-facing Ribbon images must not stop at this raw DrawingImage form.
    require(ribbon, "private static ImageSource CreateIcon(IconKind kind)", errors, "TOOL vector icon factory")
    require(ribbon, "var image = new DrawingImage(group);", errors, "raw TOOL vector artwork")

    expected_icons = {
        'ReferencePrefix + "PILE_LOWER"': "PileDown",
        'ReferencePrefix + "LEAN_CONCRETE"': "LeanConcrete",
        'ReferencePrefix + "EXCAVATE_FOUNDATION"': "Excavation",
        'ReferencePrefix + "SLAB_OPENING"': "SlabOpening",
        'ReferencePrefix + "MCP_SETTINGS"': "McpSettings",
        'ReferencePrefix + "MCP_DOCS"': "McpDocs",
        'ReferencePrefix + "AI_DASHBOARD"': "AiDashboard",
        'ReferencePrefix + "MCP_CONNECTION"': "Connection",
        'ReferencePrefix + "CAD_TO_BLT"': "CadToBlt",
    }
    for button_id, kind in expected_icons.items():
        require(polisher, f'{{ {button_id}, "{kind}" }}', errors, f"TOOL bitmap mapping {button_id}")

    for needle, label in (
        ('private const int VectorViewportSize = 32;', "32-unit TOOL vector viewport"),
        ('typeof(BltToolRibbonAugmenter)', "reuse of canonical TOOL artwork"),
        ('GetNestedType("IconKind", BindingFlags.NonPublic)', "private TOOL icon-kind lookup"),
        ('GetMethod("CreateIcon", BindingFlags.NonPublic | BindingFlags.Static)', "canonical TOOL vector factory lookup"),
        ('thread.CurrentCulture = CultureInfo.InvariantCulture;', "culture-invariant TOOL vector construction"),
        ('thread.CurrentCulture = previousCulture;', "host culture restoration"),
        ('SetRequiredProperty(item, "Image", Rasterize(vector, 16));', "16px Ribbon Image"),
        ('SetRequiredProperty(item, "LargeImage", Rasterize(vector, 32));', "32px Ribbon LargeImage"),
        ('SetRequiredProperty(item, "ShowImage", true);', "visible TOOL bitmap icons"),
        ('private static ImageSource Rasterize(ImageSource vector, int pixelSize)', "bitmap-returning TOOL rasterizer"),
        ('var bitmap = new RenderTargetBitmap(', "bitmap rasterization"),
        ('PixelFormats.Pbgra32', "BricsCAD-compatible bitmap format"),
        ('bitmap.Freeze();', "frozen host-facing bitmap"),
        ('return bitmap;', "host-facing bitmap return contract"),
        ('if (polished.Count != IconKinds.Count)', "all-nine TOOL fail-closed count"),
        ('throw new InvalidOperationException("Duplicate TOOL ribbon button id: " + id);', "duplicate TOOL id rejection"),
    ):
        require(polisher, needle, errors, label)

    forbid(
        polisher,
        'SetRequiredProperty(item, "LargeImage", Rasterize(vector, 16));',
        errors,
        "16px image reused for LargeImage",
    )
    forbid(
        polisher,
        'SetRequiredProperty(item, "Image", Rasterize(vector, 32));',
        errors,
        "32px image reused for compact Image",
    )

    require(coordinator, "BltToolRibbonIconPolisher.Reset();", errors, "TOOL bitmap reset lifecycle")
    require(coordinator, "BltToolRibbonIconPolisher.TryInitialize()", errors, "TOOL bitmap initialization")
    require_order(
        coordinator,
        [
            "BltToolRibbonAugmenter.TryInitialize()",
            "RibbonBootstrapIconAugmenter.TryInitialize()",
            "BltToolRibbonIconPolisher.TryInitialize()",
            "BltRecognitionIconPolisher.TryInitialize()",
            "RibbonCommandParameterFallback.TryInitialize()",
        ],
        errors,
        "final TOOL bitmap pass after generic decoration",
    )

    # V26 intentionally compiles the shared V25 source tree; the fix must not fork icon behavior.
    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', errors, "V26 shared V25 Ribbon source")

    if errors:
        print("BLT3D TOOL bitmap icon preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: TOOL keeps the nine clean-room BLT3D-familiar semantic glyphs but presents them "
        "to BricsCAD as deterministic 16px/32px frozen bitmaps after generic Ribbon decoration."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
