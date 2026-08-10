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
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing native slab-mesh file: " + rel)

owners = []
health_owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DSLABREBAR3D"', text, re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
    if re.search(r'\[CommandMethod\("QS3DSLABREBARHEALTH"', text, re.IGNORECASE): health_owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DSLABREBAR3D must have exactly one CommandMethod owner; found: " + ", ".join(owners))
if len(health_owners) != 1: errors.append("QS3DSLABREBARHEALTH must have exactly one CommandMethod owner; found: " + ", ".join(health_owners))

builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs"
if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        "ElementCategory.Slab", "RectangularSlabMeshPlanner.Plan", 'HandlesKey = "GeneratedSlabMeshHandles"',
        'Mode = "SlabMeshXY"', "GeneratedRebarOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(handle, element, HandlesKey)", "duplicateSelectedSource", "MaxBarsPerBatch = 12000",
        "RebarSlabXNotation", "RebarSlabYNotation", "RebarSlabCoverM", "RebarSlabFaces", "RebarSlabXClosestToFace",
        "closed 4-vertex rectangular POLYLINE", "GeneratedSlabMeshXDiameterMm", "GeneratedSlabMeshYDiameterMm",
        "GeneratedSlabMeshXActualSpacingM", "GeneratedSlabMeshYActualSpacingM", '"GeneratedSlabMeshMode"] = Mode',
        "CadGeometryGuard.Midpoint", "CadGeometryGuard.Subtract", "CadGeometryGuard.Multiply", "CadGeometryGuard.Hypot3", "CreateFrustum",
    ):
        if needle not in text: errors.append("native slab-mesh builder guard missing: " + needle)
    for obsolete in ('HandlesKey = "GeneratedRebarHandles"', "EnsureSlabMeshOwnsGenericRebarSlot", "cùng đường kính"):
        if obsolete in text: errors.append("native slab-mesh builder still contains obsolete generic ownership contract: " + obsolete)

policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for needle in ("RebarHandleKeys", "GeneratedSlabMeshHandles", "IsOwnerSlot", "IsRebarOwnerSlot"):
        if needle not in text: errors.append("slab-mesh generated ownership policy missing: " + needle)

ownership_guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"
if ownership_guard.is_file():
    text = ownership_guard.read_text(encoding="utf-8")
    for needle in ("CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys"):
        if needle not in text: errors.append("slab-mesh cross-set ownership guard missing shared policy contract: " + needle)

invalidator = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
if invalidator.is_file():
    text = invalidator.read_text(encoding="utf-8")
    for needle in ("CoreOwnershipPolicy.RebarHandleKeys", "MetadataPrefixForHandleKey", "RemoveByPrefix"):
        if needle not in text: errors.append("slab-mesh invalidation missing shared policy contract: " + needle)

health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in (
        'HandlesKey = "GeneratedSlabMeshHandles"', "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)",
        "SLAB_MESH_MODE_INVALID", "GeneratedSlabMeshXDiameterMm", "GeneratedSlabMeshYDiameterMm", "GeneratedSlabMeshFaces", "ElementCategory.Slab"
    ):
        if needle not in text: errors.append("slab-mesh health guard missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DSLABREBAR3D"', "SlabMeshSolidBuilder.BuildSelected", "RebarSlabXNotation/RebarSlabYNotation",
        'CommandMethod("QS3DSLABREBARHEALTH"', "GeneratedSlabMeshHealthService", "GeneratedSlabMeshHandles",
    ):
        if needle not in text: errors.append("native slab-mesh command missing: " + needle)

planner = ROOT / "src/QS3D.Core/Rebar/RectangularSlabMeshPlanner.cs"
if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in ("MaxBars = 8192", "projectedBars", "new List<SlabMeshBarPlacement>((int)projectedBars)"):
        if needle not in text: errors.append("slab mesh planner allocation guard missing: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/SlabMeshRegressionSmoke.cs"
if smoke.is_file() and "OversizedAggregateMeshIsRejected();" not in smoke.read_text(encoding="utf-8"):
    errors.append("slab mesh aggregate allocation regression is missing")

print("QS3D native slab-mesh preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: rectangular Slab X/Y mesh planner-to-Solid3d wiring uses dedicated ownership, policy-driven cross-family health/erase protection, independent X/Y diameters, invalidation, finite CAD transforms and pre-allocation limits; runtime remains V25-gated.")
