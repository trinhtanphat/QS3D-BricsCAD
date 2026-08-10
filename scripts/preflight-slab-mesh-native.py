#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Rebar/RectangularSlabMeshPlanner.cs",
    "tests/QS3D.Core.SmokeTests/SlabMeshRegressionSmoke.cs",
    "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing native slab-mesh file: " + rel)

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DSLABREBAR3D"', text, re.IGNORECASE):
        owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1:
    errors.append("QS3DSLABREBAR3D must have exactly one CommandMethod owner; found: " + ", ".join(owners))

builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs"
if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        "ElementCategory.Slab",
        "RectangularSlabMeshPlanner.Plan",
        'HandlesKey = "GeneratedSlabMeshHandles"',
        'Mode = "SlabMeshXY"',
        "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(handle, element, HandlesKey)",
        "MaxBarsPerBatch = 12000",
        "RebarSlabXNotation",
        "RebarSlabYNotation",
        "RebarSlabCoverM",
        "RebarSlabFaces",
        "RebarSlabXClosestToFace",
        "closed 4-vertex rectangular POLYLINE",
        "GeneratedSlabMeshXActualSpacingM",
        "GeneratedSlabMeshYActualSpacingM",
        '"GeneratedSlabMeshMode"] = Mode',
        "BooleanOperation" if False else "CreateFrustum",
    ):
        if needle not in text: errors.append("native slab-mesh builder guard missing: " + needle)

ownership_guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"
if ownership_guard.is_file() and 'Add(element, "GeneratedSlabMeshHandles", owners)' not in ownership_guard.read_text(encoding="utf-8"):
    errors.append("slab-mesh handles are missing from cross-set generated ownership")

invalidator = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
if invalidator.is_file() and 'Remove(element, "GeneratedSlabMeshHandles")' not in invalidator.read_text(encoding="utf-8"):
    errors.append("slab-mesh handles are not invalidated with dependent generated geometry")

health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in ('HandlesKey = "GeneratedSlabMeshHandles"', "BuildOwnershipIndex(project)", "SLAB_MESH_MODE_INVALID"):
        if needle not in text: errors.append("slab-mesh health guard missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DSLABREBAR3D"',
        "SlabMeshSolidBuilder.BuildSelected",
        "RebarSlabXNotation/RebarSlabYNotation",
    ):
        if needle not in text: errors.append("native slab-mesh command missing: " + needle)

print("QS3D native slab-mesh preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: rectangular Slab X/Y mesh planner-to-Solid3d wiring, ownership/mode isolation, limits and command registration are present.")
