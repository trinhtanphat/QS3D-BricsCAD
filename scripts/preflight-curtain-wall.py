#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs",
    "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing curtain-wall file: " + relative)

checks = {
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs": [
        "CurtainWallLayoutInput",
        "CurtainWallLayoutPlanner",
        "MaxPanelWidthM",
        "MaxPanelHeightM",
        "PerimeterFrameWidthM",
        "MullionWidthM",
        "TransomWidthM",
        "PanelCount",
        "ClearGlassAreaM2",
        "FrameFaceAreaM2",
        "VerticalFrameLengthM",
        "HorizontalFrameLengthM",
        "MaxPanels",
        "Value must be finite",
    ],
    "src/QS3D.Core/Services/SemanticRegenerators.cs": [
        "element.Category == ElementCategory.GlassWall",
        "CurtainWallLayoutPlanner.Plan",
        '"CurtainMaxPanelWidthM"',
        '"CurtainMaxPanelHeightM"',
        '"CurtainPerimeterFrameWidthM"',
        '"CurtainMullionWidthM"',
        '"CurtainTransomWidthM"',
        '"CurtainPanelCount"',
        '"CurtainNetGlassAreaM2"',
        '"CurtainFrameFaceAreaM2"',
        "SubtractFloorZero(curtain.ClearGlassAreaM2, openingArea",
    ],
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs": [
        'CommandMethod("QS3DGLASSWALL"',
        'EnsureDefault(family, "CurtainMaxPanelWidthM", "1.2")',
        'EnsureDefault(family, "CurtainMaxPanelHeightM", "1.5")',
        'EnsureDefault(family, "CurtainPerimeterFrameWidthM", "0.05")',
        'EnsureDefault(family, "CurtainMullionWidthM", "0.05")',
        'EnsureDefault(family, "CurtainTransomWidthM", "0.05")',
        'EnsureDefault(family, "CurtainFrameMaterial", "Nhôm")',
    ],
    "tests/QS3D.Core.SmokeTests/CurtainWallLayoutSmoke.cs": [
        "UniformGridProducesStableQuantities",
        "SinglePanelUsesPerimeterFramesOnly",
        "RejectsImpossibleFramesAndExcessiveGrid",
        "ClearGlassAreaM2",
        "FrameFaceAreaM2",
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "CurtainWallLayoutSmoke.Run();",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing curtain-wall guard/token: " + needle)

# Do not reintroduce the short-lived duplicate planner/API.
duplicate = ROOT / "src/QS3D.Core/Geometry/GlassWallLayoutPlanner.cs"
if duplicate.exists():
    errors.append("duplicate GlassWallLayoutPlanner.cs must not coexist with CurtainWallLayoutPlanner.cs")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain-wall panel/frame planning, GlassWall family defaults, opening-aware semantic quantities and deterministic smoke registration are present without duplicate planner APIs.")
