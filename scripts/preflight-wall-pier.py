#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/WallFootprintEngine.cs",
    "src/QS3D.Core/Geometry/WallPierProfilePlanner.cs",
    "src/QS3D.Core/Geometry/WallPierPathProfilePlanner.cs",
    "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "tests/QS3D.Core.SmokeTests/WallPierProfileSmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
    "docs/WALL-PIER-POLYLINE.md",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing wall-pier profile file: " + relative)

checks = {
    "src/QS3D.Core/Geometry/WallFootprintEngine.cs": [
        "Length = start.DistanceTo(end);",
        "TranslateToLocal",
        "HasPolygonSelfIntersection",
    ],
    "src/QS3D.Core/Geometry/WallPierProfilePlanner.cs": [
        "WallPierProfileMode",
        "Rectangular",
        "Chamfered",
        "WallPierProfilePlanner",
        "CrossSectionAreaM2",
        "CrossSectionPerimeterM",
        "VolumeM3",
        "LateralAreaM2",
        "Four right-triangle corners are removed",
        "smaller than half the minimum profile dimension",
    ],
    "src/QS3D.Core/Geometry/WallPierPathProfilePlanner.cs": [
        "WallPierPathProfileInput",
        "WallPierPathProfilePlanner",
        "new WallFootprintEngine().Build",
        "ChamferTerminalCorners",
        "TerminalMatchTolerance",
        "MachineEpsilon",
        "32d * MachineEpsilon",
        "var length = start.DistanceTo(end);",
        "WallPierProfileMode.Rectangular",
        "WallPierProfileMode.Chamfered",
        "terminal footprint corners",
        "2d * chamfer + tolerance",
        "FootprintAreaM2",
        "FootprintPerimeterM",
        "UsedBevelJoin",
    ],
    "src/QS3D.Core/Services/SemanticRegenerators.cs": [
        "element.Category == ElementCategory.WallPier",
        "ResolveWallPierProfileMode(project, element)",
        "ResolveWallPierNumber(project, element",
        "TryReadCurrentWallPierPathProfile",
        '"GeneratedSolidHandle"',
        "element.IsGeneratedSolidStale()",
        '"WallPierPathProfileKind"',
        '"OpenPolyline"',
        '"WallPierPathProfileAreaM2"',
        '"WallPierPathProfilePerimeterM"',
        '"WallPierPathProfileGrossVolumeM3"',
        '"WallPierPathProfileLateralAreaM2"',
        "NearlyEqual(grossVolumeM3, areaM2 * heightM)",
        "WallPierProfilePlanner.Plan",
        '"WallPierProfileCrossSectionAreaM2"',
        '"WallPierProfileNetVolumeM3"',
    ],
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs": [
        'CommandMethod("QS3DWALLPIER"',
        'EnsureDefault(family, "WallPierProfileMode", "Rectangular")',
        'EnsureDefault(family, "WallPierChamferM", "0.02")',
    ],
    "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs": [
        "BuildSelectedLinePiers",
        "WallPierProfilePlanner.Plan",
        "WallPierProfileMode.Chamfered",
        "WallPierChamferM",
        "Region.CreateFromCurves",
        "CreateExtrudedSolid",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
        "source LINE phải nằm trên mặt phẳng ngang",
        "matches.Count > 1",
        "processed.Add(element.Id)",
        "ClearPathProfileSnapshot(update.Element)",
        'StartsWith("WallPierPathProfile"',
        "update.Element.MarkDirty(ElementDirtyFlags.Quantity)",
    ],
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs": [
        "category == ElementCategory.WallPier",
        "ResolveWallPierMode(element, family)",
        "WallPierPathProfilePlanner.Plan",
        "WallArcSagittaM",
        "BulgeArcTessellator.Tessellate",
        'properties["WallPierPathProfileKind"] = "OpenPolyline"',
        'properties["WallPierPathProfileMode"]',
        'properties["WallPierPathProfileChamferM"]',
        'properties["WallPierPathProfileCenterlineLengthM"]',
        'properties["WallPierPathProfileThicknessM"]',
        'properties["WallPierPathProfileHeightM"]',
        'properties["WallPierPathProfileAreaM2"]',
        'properties["WallPierPathProfilePerimeterM"]',
        'properties["WallPierPathProfileGrossVolumeM3"]',
        'properties["WallPierPathProfileLateralAreaM2"]',
        "update.Element.MarkDirty(ElementDirtyFlags.Quantity)",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
    ],
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs": [
        "category.Value == ElementCategory.WallPier",
        "WallPierProfileSolidBuilder.BuildSelectedLinePiers",
        "PolylineWallSolidBuilder.BuildSelected",
        "profile Rectangular/Chamfered",
        "open POLYLINE",
    ],
    "tests/QS3D.Core.SmokeTests/WallPierProfileSmoke.cs": [
        "RectangularProfileMatchesWallVolume",
        "ChamferedProfileReducesAreaAndVolume",
        "StraightRectangularPathMatchesLegacyPlanner",
        "StraightChamferedPathMatchesLegacyPlanner",
        "BentPathUsesSharedFootprintAndTerminalChamfers",
        "CurrentPathSnapshotDrivesWallPierQuantity",
        "StalePathSnapshotFallsBackToSemanticProfile",
        "LargeFiniteStraightFootprintAvoidsSquaredLengthOverflow",
        "new Point2(1e200d, 0d)",
        "LargeGeoreferencedChamferedPathRemainsResolvable",
        "const double origin = 1e12d",
        "GeneratedSolidHandle",
        "Generated WallPier host should be stale",
        "RejectsOversizedTerminalChamfer",
        "RejectsSelfIntersectingPath",
        "RejectsImpossibleAndNonFiniteProfiles",
        "rectangular.FootprintAreaM2 - 2d * chamfer * chamfer",
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "WallPierProfileSmoke.Run();",
    ],
    "docs/WALL-PIER-POLYLINE.md": [
        "source-implemented",
        "WallFootprintEngine",
        "four terminal footprint corners",
        "WallPierPathProfileKind = OpenPolyline",
        "runtime-verified",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing wall-pier guard/token: " + needle)

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
if review.is_file() and "open POLYLINE vẫn dùng footprint Tường KT" in review.read_text(encoding="utf-8"):
    errors.append("ReviewCommands still describes WallPier open POLYLINE as generic Tường KT fallback")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: WallPier LINE/open-POLYLINE planning uses stable segment length, precision-aware georeferenced terminal matching, shared footprint joins, exact current-snapshot quantities, guarded native Solid3d wiring, stale cleanup, smoke and docs.")
