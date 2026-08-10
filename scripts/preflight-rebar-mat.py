#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Rebar/OrthogonalRebarMatPlanner.cs",
    "src/QS3D.Core/Diagnostics/GeneratedRebarMatHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/RebarMatSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/RebarMatCommands.cs",
    "src/QS3D.BricsCAD.V25/RebarMatHealthCommands.cs",
    "tests/QS3D.Core.SmokeTests/OrthogonalRebarMatSmoke.cs",
    "tests/QS3D.Core.SmokeTests/OrthogonalRebarMatSmokeRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing rebar-mat file: " + relative)

checks = {
    "src/QS3D.Core/Rebar/OrthogonalRebarMatPlanner.cs": [
        "OrthogonalRebarMatPlanner", "LinearRebarLayoutPlanner.Plan", "BottomEnabled", "TopEnabled", "MaxBars",
        "Top and bottom orthogonal rebar mats overlap", "bar centers are closer than one bar diameter"
    ],
    "src/QS3D.BricsCAD.V25/Cad/RebarMatSolidBuilder.cs": [
        "GeneratedRebarMatHandles", "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "MaxBarsPerElement", "MaxBarsPerBatch",
        "ElementCategory.Slab", "ElementCategory.Foundation", "closed 4-vertex rectangle", "RebarMatFaces", "Dxx@spacing"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedRebarMatHealthService.cs": [
        "REBAR_MAT_GENERATED_OWNERSHIP_CONFLICT", "REBAR_MAT_GENERATED_SOLID_MISSING", "REBAR_MAT_CATEGORY_MISMATCH", "REBAR_MAT_GENERATED_STALE"
    ],
    "src/QS3D.BricsCAD.V25/RebarMatCommands.cs": ["QS3DREBARMAT3D"],
    "src/QS3D.BricsCAD.V25/RebarMatHealthCommands.cs": ["QS3DREBARMATHEALTH"],
    "tests/QS3D.Core.SmokeTests/OrthogonalRebarMatSmoke.cs": ["BottomMat", "BothFaces", "RejectsThinHost", "RejectsOvercrowdedSpacing"],
    "tests/QS3D.Core.SmokeTests/OrthogonalRebarMatSmokeRegistration.cs": ["OrthogonalRebarMatSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

ownership = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"
if ownership.is_file() and 'Add(element, "GeneratedRebarMatHandles", owners);' not in ownership.read_text(encoding="utf-8"):
    errors.append("GeneratedRebarOwnershipGuard does not reserve rebar-mat handles")

invalidation = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
if invalidation.is_file():
    text = invalidation.read_text(encoding="utf-8")
    if '"GeneratedRebarMatHandles"' not in text or 'Remove(element, "GeneratedRebarMatMode")' not in text:
        errors.append("generated geometry invalidation does not clear rebar-mat solids/metadata")

health_all = ROOT / "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs"
if health_all.is_file():
    text = health_all.read_text(encoding="utf-8")
    for needle in ("GeneratedBeamStirrupHealthService", "GeneratedRebarMatHealthService", "GeneratedBeamStirrupHandles", "GeneratedRebarMatHandles"):
        if needle not in text:
            errors.append("Rebar Health All missing: " + needle)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: orthogonal Slab/Foundation rebar mat planning, ownership, invalidation, health and Health-All integration are present.")
