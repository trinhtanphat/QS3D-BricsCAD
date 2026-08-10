#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs",
    "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs",
    "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallDetailSmoke.cs",
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
    "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs": [
        "CurtainWallDetailPlanner",
        "CurtainWallLayoutPlanner.Plan(input)",
        "VerticalFrames",
        "HorizontalFrames",
        "Panels",
        "MaxDetailSolids = 20000",
        "panel area does not match the layout clear-glass area",
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
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs": [
        "CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT",
        "CURTAIN_FRAME_GENERATED_SOLID_MISSING",
        "CURTAIN_FRAME_GRID_COUNT_MISMATCH",
        "element.IsGeneratedCurtainFrameStale()",
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
        'EnsureDefault(family, "CurtainFrameDepthM", "0.05")',
        'EnsureDefault(family, "CurtainFrameMaterial", "Nhôm")',
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs": [
        'HandlesKey = "GeneratedCurtainFrameHandles"',
        "EnsureOwned",
        "Refusing destructive erase",
        'ReserveProperty(owners, element, "GeneratedSlabMeshHandles")',
        'ReserveProperty(owners, element, "GeneratedWallMeshHandles")',
    ],
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs": [
        "CurtainWallDetailPlanner.Plan",
        "MaxFramesPerElement = 4096",
        "MaxFramesPerBatch = 8192",
        "GeneratedCurtainFrameOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(handle, element)",
        'GeneratedCurtainFrameMode"] = Mode',
        "ClearGeneratedCurtainFrameStale",
        "CreateBox",
        "GetObject(ids[0], OpenMode.ForWrite",
        "Refusing destructive erase",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs": [
        'CommandMethod("QS3DCURTAINFRAMES3D"',
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs": [
        'CommandMethod("QS3DCURTAIN3D"',
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
    ],
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs": [
        'CommandMethod("QS3DBUILD3D"',
        "category.Value == ElementCategory.GlassWall",
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
        "detailSolids = curtain.Frames",
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
    "tests/QS3D.Core.SmokeTests/CurtainWallDetailSmoke.cs": [
        "DetailGridMatchesClearGlassArea",
        "SinglePanelKeepsOnlyPerimeterFrames",
        "NativeDetailCapRejectsHugeGrid",
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
        "CurtainWallDetailSmoke.Run();",
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

print("PASS: curtain-wall panel/frame planning, GlassWall defaults/quantities, guarded mullion/transom Solid3d ownership+health and both dedicated/common Build3D workflows are present without duplicate planner APIs.")
