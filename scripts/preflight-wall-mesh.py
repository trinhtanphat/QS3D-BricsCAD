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
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
<<<<<<< HEAD
    "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
=======
    "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
>>>>>>> origin/main
    "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs",
    "src/QS3D.BricsCAD.V25/StructuralWallMeshHealthCommands.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing structural-wall mesh file: " + rel)

owners = []
health_owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DWALLREBAR3D"', text, re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
    if re.search(r'\[CommandMethod\("QS3DWALLREBARHEALTH"', text, re.IGNORECASE): health_owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DWALLREBAR3D must have exactly one CommandMethod owner; found: " + ", ".join(owners))
if len(health_owners) != 1: errors.append("QS3DWALLREBARHEALTH must have exactly one CommandMethod owner; found: " + ", ".join(health_owners))

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
        "ElementCategory.StructuralWall", "RectangularWallMeshPlanner.Plan", 'HandlesKey = "GeneratedWallMeshHandles"',
        'Mode = "StructuralWallMesh"', "GeneratedRebarOwnershipGuard.Build(project)",
<<<<<<< HEAD
        "ownership.EnsureOwned(handle, element, HandlesKey)",
        "MaxBarsPerBatch = 12000", "RebarWallHorizontalNotation", "RebarWallVerticalNotation",
        "RebarWallCoverM", "RebarWallFaces", "RebarWallHorizontalClosestToFace",
=======
        "ownership.EnsureOwned(handle, element, HandlesKey)", "MaxBarsPerBatch = 12000",
        "RebarWallHorizontalNotation", "RebarWallVerticalNotation", "RebarWallCoverM", "RebarWallFaces",
        "RebarWallHorizontalClosestToFace", "GeneratedWallMeshHorizontalDiameterMm", "GeneratedWallMeshVerticalDiameterMm",
        "GeneratedWallMeshHorizontalActualSpacingM", "GeneratedWallMeshVerticalActualSpacingM",
>>>>>>> origin/main
        '"GeneratedWallMeshMode"] = Mode', "CreateFrustum", "source LINE gần ngang",
    ):
        if needle not in text: errors.append("native StructuralWall mesh builder missing: " + needle)
    for obsolete in ('HandlesKey = "GeneratedRebarHandles"', "EnsureWallMeshOwnsGenericRebarSlot", "cùng đường kính"):
        if obsolete in text: errors.append("native StructuralWall mesh still contains obsolete generic ownership contract: " + obsolete)

ownership_guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"
if ownership_guard.is_file() and 'Add(element, "GeneratedWallMeshHandles", owners)' not in ownership_guard.read_text(encoding="utf-8"):
    errors.append("wall-mesh handles are missing from cross-set generated ownership")

invalidator = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
if invalidator.is_file() and 'Remove(element, "GeneratedWallMeshHandles")' not in invalidator.read_text(encoding="utf-8"):
    errors.append("wall-mesh handles are not invalidated with dependent generated geometry")

ownership_health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs"
if ownership_health.is_file() and '"GeneratedWallMeshHandles"' not in ownership_health.read_text(encoding="utf-8"):
    errors.append("wall-mesh handles are missing from cross-family ownership health")

command = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('CommandMethod("QS3DWALLREBAR3D"', "StructuralWallMeshSolidBuilder.BuildSelected", "RebarWallHorizontalNotation/RebarWallVerticalNotation"):
        if needle not in text: errors.append("StructuralWall mesh command missing: " + needle)

health_command = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshHealthCommands.cs"
if health_command.is_file():
    text = health_command.read_text(encoding="utf-8")
    for needle in ('CommandMethod("QS3DWALLREBARHEALTH"', "GeneratedWallMeshHealthService", "GeneratedWallMeshHandles"):
        if needle not in text: errors.append("StructuralWall mesh health command missing: " + needle)

for rel in ("src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs", "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"):
    path = ROOT / rel
    if path.is_file() and "GeneratedWallMeshHandles" not in path.read_text(encoding="utf-8"):
        errors.append(rel + " missing GeneratedWallMeshHandles")

health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in ("GeneratedWallMeshHorizontalDiameterMm", "GeneratedWallMeshVerticalDiameterMm", "GeneratedWallMeshFaces", "ElementCategory.StructuralWall"):
        if needle not in text: errors.append("StructuralWall mesh health missing: " + needle)

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
print("PASS: StructuralWall horizontal/vertical near/far mesh planning uses dedicated ownership, independent diameters, invalidation, health, limits and command registration; runtime remains V25-gated.")
