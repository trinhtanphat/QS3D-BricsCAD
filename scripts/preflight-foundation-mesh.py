#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs",
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs",
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmoke.cs",
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmokeRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing foundation-mesh file: " + relative)

checks = {
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs": [
        "RectangularSlabMeshPlanner.Plan", "ElementCategory.Foundation", "GeneratedFoundationMeshHandles",
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "RebarFoundationXNotation", "RebarFoundationYNotation",
        "RebarFoundationFaces", "MaxBarsPerBatch", "FoundationMeshXY"
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs": ["QS3DFOUNDATIONREBAR3D"],
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs": [
        "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT", "FOUNDATION_MESH_GENERATED_SOLID_MISSING",
        "FOUNDATION_MESH_CATEGORY_MISMATCH", "FOUNDATION_MESH_GENERATED_STALE"
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs": ["QS3DFOUNDATIONREBARHEALTH"],
    "src/QS3D.BricsCAD.V25/Rebar3DHubCommands.cs": ["ElementCategory.Foundation", "QS3DFOUNDATIONREBAR3D"],
    "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs": [
        "ElementCategory.Foundation", "RebarFoundationXNotation", "RebarFoundationYNotation", "RebarFoundationCoverM", "RebarFoundationFaces"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": ["GeneratedFoundationMeshHandles"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": ["GeneratedFoundationMeshHandles", "GeneratedFoundationMeshMode"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs": ["GeneratedFoundationMeshHandles"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs": ["FoundationMeshXY", "GeneratedFoundationMeshHandles"],
    "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs": ["GeneratedFoundationMeshHealthService", "FoundationMeshSolidBuilder.HandlesKey"],
    "src/QS3D.BricsCAD.V25/HealthAllCommands.cs": ["GeneratedFoundationMeshHealthService", "FoundationMeshSolidBuilder.HandlesKey"],
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmokeRegistration.cs": ["FoundationMeshHealthSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing checked file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Foundation mesh reuses the Slab mesh planner while preserving separate ownership, invalidation, setup, health and Hub dispatch contracts.")
