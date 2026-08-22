#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs",
    "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs",
    "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs",
    "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs",
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallDetailSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainFrameOpeningSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallRegeneratorSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallRegeneratorRegistration.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing curtain-wall file: " + relative)

checks = {
    "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs": [
        "CurtainWallLayoutPlanner", "MaxPanelWidthM", "MaxPanelHeightM", "PerimeterFrameWidthM",
        "MullionWidthM", "TransomWidthM", "PanelCount", "ClearGlassAreaM2", "FrameFaceAreaM2",
        "MaxGridDivisions = 10000", "MaxPanels = 250000"
    ],
    "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs": [
        "CurtainWallDetailPlanner", "CurtainWallLayoutPlanner.Plan(input)", "VerticalFrames", "HorizontalFrames",
        "Panels", "MaxDetailSolids = 20000", "panel area does not match the layout clear-glass area"
    ],
    "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs": [
        "CurtainOpeningRect", "CurtainFrameOpeningPlanner", "Interrupt", "MaxOpenings = 4096", "MaxOutputFragments = 20000",
        "ClearanceM", "Subtract", "Curtain frame opening interruption exceeds fragment safety limit"
    ],
    "src/QS3D.Core/Services/SemanticRegenerators.cs": [
        "element.Category == ElementCategory.GlassWall", "CurtainWallLayoutPlanner.Plan",
        '"CurtainNetGlassAreaM2"', "SubtractFloorZero(curtain.ClearGlassAreaM2, openingArea",
        "host.MarkGeneratedCurtainFrameStale", "Linked opening "
    ],
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs": [
        "CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT", "CURTAIN_FRAME_GENERATED_SOLID_MISSING",
        "GeneratedCurtainFrameBaseCount", "GeneratedCurtainFrameOpeningCount",
        "LineFrameOverlay.OpeningAware", "CURTAIN_FRAME_OPENING_MODE_MISMATCH",
        "element.IsGeneratedCurtainFrameStale()"
    ],
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs": [
        "case ElementCategory.GlassWall:", 'family.Properties["ThicknessM"] = "0.012"',
        'family.Properties["CurtainMaxPanelWidthM"] = "1.2"', 'family.Properties["CurtainFrameMaterial"] = "Nhôm"'
    ],
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs": [
        'CommandMethod("QS3DGLASSWALL"', 'EnsureDefault(family, "CurtainMaxPanelWidthM", "1.2")',
        'EnsureDefault(family, "CurtainFrameDepthM", "0.05")', 'EnsureDefault(family, "CurtainFrameMaterial", "Nhôm")'
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs": [
        'HandlesKey = "GeneratedCurtainFrameHandles"', "EnsureOwned", "Refusing destructive erase",
        'CoreOwnershipPolicy.IsOwnerSlot(property.Key)',
        'string.Equals(property.Key, HandlesKey, StringComparison.OrdinalIgnoreCase)'
    ],
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs": [
        "CurtainWallDetailPlanner.Plan",
        "CurtainFrameOpeningPlanner.Interrupt",
        "ReadLinkedOpenings",
        "OpeningCutPlanner.Plan",
        "hostThicknessM / 2d + 0.25d",
        "BooleanClearanceM",
        "GeneratedCurtainFrameBaseCount",
        "GeneratedCurtainFrameOpeningCount",
        "OpeningAwareMode",
        "MaxFramesPerElement = 4096",
        "MaxFramesPerBatch = 8192",
        "GeneratedCurtainFrameOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(original, element)",
        "GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership",
        'GeneratedCurtainFrameMode"] = update.OpeningCount > 0 ? OpeningAwareMode : Mode',
        "ClearGeneratedCurtainFrameStale",
        "CreateBox",
        "GetObject(item.Value, OpenMode.ForWrite",
        "Refusing destructive replacement before any frame is erased",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs": [
        'CommandMethod("QS3DCURTAINFRAMES3D"', "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls"
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs": [
        'CommandMethod("QS3DCURTAIN3D"', "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected", "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls"
    ],
    "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs": [
        "NativeBuildCapability", "ElementCategory.GlassWall", "IsWallCategory(ElementCategory category)"
    ],
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs": [
        'CommandMethod("QS3DBUILD3D"', "NativeBuildCapability.IsWallCategory(category)",
        "WallSolidBuilder.BuildSelectedLineWalls", "PolylineWallSolidBuilder.BuildSelected"
    ],
    "tests/QS3D.Core.SmokeTests/CurtainFrameOpeningSmoke.cs": [
        "DoorInterruptsVerticalAndHorizontalFrames", "NonIntersectingOpeningLeavesFrameIntact",
        "ClearanceExpandsInterruptedRegion", "MultipleOpeningsRemainDeterministic"
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "CurtainWallLayoutSmoke.Run();", "CurtainWallDetailSmoke.Run();", "CurtainFrameOpeningSmoke.Run();"
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

if (ROOT / "src/QS3D.Core/Geometry/GlassWallLayoutPlanner.cs").exists():
    errors.append("duplicate GlassWallLayoutPlanner.cs must not coexist with CurtainWallLayoutPlanner.cs")
if (ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallLayoutRegistration.cs").exists():
    errors.append("CurtainWallLayoutRegistration.cs duplicates the shared smoke registration")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain layout/detail, opening interruption, linked-opening stale lifecycle, opening-aware native frame fragments, ownership/health, canonical host Build3D wiring and dedicated curtain-frame orchestration are present.")
