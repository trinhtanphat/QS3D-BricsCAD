#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs",
    "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallRegeneratorSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallRegeneratorRegistration.cs",
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
        "MaxGridDivisions = 10000",
        "MaxPanels = 250000",
        "is not positive after frame deductions",
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
        '"CurtainFrameLengthM"',
        '"CurtainClearGlassAreaM2"',
        '"CurtainNetGlassAreaM2"',
        '"CurtainFrameFaceAreaM2"',
        "SubtractFloorZero(curtain.ClearGlassAreaM2, openingArea",
    ],
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs": [
        "case ElementCategory.GlassWall:",
        'family.Properties["ThicknessM"] = "0.012"',
        'family.Properties["CurtainMaxPanelWidthM"] = "1.2"',
        'family.Properties["CurtainMaxPanelHeightM"] = "1.5"',
        'family.Properties["CurtainPerimeterFrameWidthM"] = "0.05"',
        'family.Properties["CurtainMullionWidthM"] = "0.05"',
        'family.Properties["CurtainTransomWidthM"] = "0.05"',
        'family.Properties["CurtainFrameMaterial"] = "Nhôm"',
        "case ElementCategory.WallPier:",
        'case ElementCategory.GlassWall: return "Vách Kính";',
        'case ElementCategory.WallPier: return "Trụ Tường";',
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
        "16.3875d",
        "33d",
    ],
    "tests/QS3D.Core.SmokeTests/CurtainWallRegeneratorSmoke.cs": [
        "GlassWallProducesCurtainQuantitiesAndOpeningDeduction",
        "ArchitecturalWallDoesNotProduceCurtainQuantities",
        'Q(wall, "CurtainNetGlassAreaM2")',
        'Q(wall, "OpeningAreaM2")',
    ],
    "tests/QS3D.Core.SmokeTests/CurtainWallRegeneratorRegistration.cs": [
        "CurtainWallRegeneratorSmoke.Run();",
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

# Layout smoke is intentionally registered once through the shared registration.
duplicate_registration = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallLayoutRegistration.cs"
if duplicate_registration.exists():
    errors.append("CurtainWallLayoutRegistration.cs duplicates the shared smoke registration")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain-wall panel/frame planning, generic and dedicated GlassWall family defaults, opening-aware semantic quantities and deterministic smoke coverage are present without duplicate planner/registration APIs.")
