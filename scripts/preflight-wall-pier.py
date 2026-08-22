#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/WallPierProfilePlanner.cs",
    "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "tests/QS3D.Core.SmokeTests/WallPierProfileSmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing wall-pier profile file: " + relative)

checks = {
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
    "src/QS3D.Core/Services/SemanticRegenerators.cs": [
        "element.Category == ElementCategory.WallPier",
        "ResolveWallPierProfileMode",
        "WallPierProfilePlanner.Plan",
        '"WallPierChamferM"',
        '"WallPierProfileCrossSectionAreaM2"',
        '"WallPierProfilePerimeterM"',
        '"WallPierProfileLateralAreaM2"',
        '"WallPierProfileGrossVolumeM3"',
        '"WallPierProfileNetVolumeM3"',
        'element.SetQuantity("GrossVolumeM3", profile.VolumeM3)',
        'element.SetQuantity("NetVolumeM3", profileNetVolumeM3)',
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
    ],
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs": [
        "category.Value == ElementCategory.WallPier",
        "WallPierProfileSolidBuilder.BuildSelectedLinePiers",
        "PolylineWallSolidBuilder.BuildSelected",
        "profile Rectangular/Chamfered",
    ],
    "tests/QS3D.Core.SmokeTests/WallPierProfileSmoke.cs": [
        "RectangularProfileMatchesWallVolume",
        "ChamferedProfileReducesAreaAndVolume",
        "RejectsImpossibleAndNonFiniteProfiles",
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "WallPierProfileSmoke.Run();",
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

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: WallPier rectangular/chamfered profile planning, profile-aware quantities, family defaults and guarded LINE-source native Solid3d dispatch are present.")
