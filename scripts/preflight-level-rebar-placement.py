#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Level rebar-placement file: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + ": missing " + repr(token))


beam = read("src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs")
stirrups = read("src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs")
column = read("src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs")
ties = read("src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs")
slab_mesh = read("src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs")
foundation_mesh = read("src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs")
wall_mesh = read("src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs")
shape = read("src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs")
policy = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")

for label, text, source_base, height_key, snapshot_prefix in (
    ("beam longitudinal bars", beam, "line.StartPoint.Z", "HeightM", "GeneratedRebar"),
    ("beam stirrups", stirrups, "source.StartPoint.Z", "HeightM", "GeneratedBeamStirrup"),
    ("column longitudinal bars", column, "polyline.Elevation", "HeightM", "GeneratedRebar"),
    ("column ties", ties, "polyline.Elevation", "HeightM", "GeneratedTieRebar"),
    ("slab mesh", slab_mesh, "polyline.Elevation", "ThicknessM", "GeneratedSlabMesh"),
    ("foundation mesh", foundation_mesh, "polyline.Elevation", "ThicknessM", "GeneratedFoundationMesh"),
    ("structural-wall mesh", wall_mesh, "line.StartPoint.Z", "HeightM", "GeneratedWallMesh"),
):
    require(text, "CadElementVerticalPlacement.Resolve(", label)
    require(text, source_base, label)
    require(text, '"' + height_key + '"', label)
    require(text, 'CadElementVerticalPlacement.CommitSnapshot(update.Element, "' + snapshot_prefix + '"', label)
    resolve = text.find("CadElementVerticalPlacement.Resolve(")
    erase = text.find("ErasePrevious", resolve)
    if resolve < 0 or erase < 0 or resolve >= erase:
        errors.append(label + " must resolve and validate Level placement before erasing generated native output")

for label, text in (("beam longitudinal bars", beam), ("beam stirrups", stirrups)):
    require(text, "var centerZ = vertical.CenterDrawing;", label)

for token in (
    "var halfLength = length / 2d;",
    "var centerX = CadGeometryGuard.Add(startX, CadGeometryGuard.Multiply(unit.X, halfLength",
    "var centerY = CadGeometryGuard.Add(startY, CadGeometryGuard.Multiply(unit.Y, halfLength",
    "var centerZ = CadGeometryGuard.Add(startZ, CadGeometryGuard.Multiply(unit.Z, halfLength",
    "Matrix3d.Displacement(new Vector3d(centerX, centerY, centerZ))",
):
    require(stirrups, token, "beam stirrup native segment placement")
if "Matrix3d.Displacement(new Vector3d(startX, startY, startZ))" in stirrups:
    errors.append("beam stirrup must place each centered V25 frustum at the segment midpoint, not its start")

require(column, "var height = vertical.HeightDrawing;", "column longitudinal bars")
require(column, "var baseZ = vertical.BottomDrawing;", "column longitudinal bars")
require(ties, "var heightM = vertical.HeightM;", "column ties")
require(ties, "CadGeometryGuard.Add(vertical.BottomDrawing, elevation", "column ties")
for label, text, dimension_key in (
    ("slab mesh", slab_mesh, "thicknessM"),
    ("foundation mesh", foundation_mesh, "thicknessM"),
    ("structural-wall mesh", wall_mesh, "heightM"),
):
    require(text, "var " + dimension_key + " = verticalPlacement.HeightM;", label)
    require(text, "var centerZ = verticalPlacement.CenterDrawing;", label)

for label, text, forbidden in (
    ("beam stirrups", stirrups, "var baseZ = CadGeometryGuard.Add(source.StartPoint.Z"),
    ("column longitudinal bars", column, "var baseZ = CadGeometryGuard.Add(polyline.Elevation"),
    ("column ties", ties, "var z = CadGeometryGuard.Add(polyline.Elevation"),
):
    if forbidden in text:
        errors.append(label + " still contains a legacy-only Z path after shared placement resolution")

for token in (
    "CadElementVerticalPlacement.HasAnyLevelConfiguration(element)",
    "CadElementVerticalPlacement.Resolve(",
    "vertical.BottomDrawing",
    "extentsVertical.BottomDrawing",
    'CadElementVerticalPlacement.CommitSnapshot(item.Element, "GeneratedShapeRebar"',
    'CadElementVerticalPlacement.ClearSnapshot(item.Element, "GeneratedShapeRebar")',
):
    require(shape, token, "BBS shape rebar")
shape_resolve = shape.find("var placement = ResolvePlacement(")
shape_erase = shape.find("ErasePrevious", shape_resolve)
if shape_resolve < 0 or shape_erase < 0 or shape_resolve >= shape_erase:
    errors.append("BBS shape rebar must resolve Level-aware placement before erasing generated output")

require(policy, "return false;", "Level native integration policy must remain fail-closed")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Longitudinal/transverse reinforcement, Slab/Foundation/StructuralWall meshes and BBS shape origins use the shared branch-lazy Level placement before native replacement, persist vertical snapshots, and remain runtime-pending until exact V25 proof")
