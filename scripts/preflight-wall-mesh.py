#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Rebar/RectangularWallMeshPlanner.cs",
    "tests/QS3D.Core.SmokeTests/WallMeshRegressionSmoke.cs",
    "src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing structural-wall mesh file: " + rel)

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DWALLREBAR3D"', text, re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DWALLREBAR3D must have exactly one CommandMethod owner; found: " + ", ".join(owners))

planner = ROOT / "src/QS3D.Core/Rebar/RectangularWallMeshPlanner.cs"
if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "WallMeshFace", "WallMeshDirection", "LinearRebarLayoutPlanner.Plan", "IncludeNear", "IncludeFar",
        "HorizontalClosestToFace", "two-face mesh", "MaxBars = 8192", "HorizontalActualSpacingM", "VerticalActualSpacingM",
    ):
        if needle not in text: errors.append("structural-wall mesh planner missing: " + needle)

builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs"
if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        "ElementCategory.StructuralWall", "RectangularWallMeshPlanner.Plan", 'HandlesKey = "GeneratedRebarHandles"',
        'Mode = "StructuralWallMesh"', "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(handle, element, HandlesKey)", "EnsureWallMeshOwnsGenericRebarSlot",
        "MaxBarsPerBatch = 12000", "RebarWallHorizontalNotation", "RebarWallVerticalNotation",
        "RebarWallCoverM", "RebarWallFaces", "RebarWallHorizontalClosestToFace",
        '"GeneratedRebarMode"] = Mode', "CreateFrustum", "source LINE gần ngang",
    ):
        if needle not in text: errors.append("native StructuralWall mesh builder missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('CommandMethod("QS3DWALLREBAR3D"', "StructuralWallMeshSolidBuilder.BuildSelected", "RebarWallHorizontalNotation/RebarWallVerticalNotation"):
        if needle not in text: errors.append("StructuralWall mesh command missing: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/WallMeshRegressionSmoke.cs"
if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in ("TwoFaceMeshUsesBothDirections();", "SingleFaceCountModeIsDeterministic();", "ThinWallIsRejected();", "AmbiguousDistributionIsRejected();", "ModuleInitializer"):
        if needle not in text: errors.append("StructuralWall mesh regression missing: " + needle)

print("QS3D StructuralWall mesh preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: StructuralWall horizontal/vertical near/far mesh planning, Solid3d adapter, ownership isolation, limits and command registration are present.")
