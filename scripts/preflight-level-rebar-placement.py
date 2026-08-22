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

for label, text, source_base, legacy_height in (
    ("beam longitudinal bars", beam, "line.StartPoint.Z", "legacyHeightM"),
    ("beam stirrups", stirrups, "source.StartPoint.Z", "legacyHeightM"),
    ("column longitudinal bars", column, "polyline.Elevation", "legacyHeightM"),
    ("column ties", ties, "polyline.Elevation", "legacyHeightM"),
    ("slab mesh", slab_mesh, "polyline.Elevation", "legacyThicknessM"),
    ("foundation mesh", foundation_mesh, "polyline.Elevation", "legacyThicknessM"),
    ("structural-wall mesh", wall_mesh, "line.StartPoint.Z", "legacyHeightM"),
):
    require(text, "CadVerticalPlacementResolver.Resolve(", label)
    require(text, source_base, label)
    require(text, legacy_height, label)
    resolve = text.find("CadVerticalPlacementResolver.Resolve(")
    erase = text.find("ErasePrevious", resolve)
    if resolve < 0 or erase < 0 or resolve >= erase:
        errors.append(label + " must resolve and validate Level placement before erasing generated native output")

for label, text in (("beam longitudinal bars", beam), ("beam stirrups", stirrups)):
    require(text, "placement.BottomDrawingUnits", label)
    require(text, "placement.HeightDrawingUnits / 2d", label)

require(column, "var height = placement.HeightDrawingUnits;", "column longitudinal bars")
require(column, "var baseZ = placement.BottomDrawingUnits;", "column longitudinal bars")
require(ties, "var heightM = placement.HeightM;", "column ties")
require(ties, "CadGeometryGuard.Add(placement.BottomDrawingUnits, elevation", "column ties")
for label, text, placement_name in (
    ("slab mesh", slab_mesh, "placement"),
    ("foundation mesh", foundation_mesh, "placement"),
    ("structural-wall mesh", wall_mesh, "hostPlacement"),
):
    require(text, "var heightM = " + placement_name + ".HeightM;" if label == "structural-wall mesh" else "var thicknessM = " + placement_name + ".HeightM;", label)
    require(text, placement_name + ".BottomDrawingUnits", label)
    require(text, placement_name + ".HeightDrawingUnits / 2d", label)

for label, text, forbidden in (
    ("beam stirrups", stirrups, "var baseZ = CadGeometryGuard.Add(source.StartPoint.Z"),
    ("column longitudinal bars", column, "var baseZ = CadGeometryGuard.Add(polyline.Elevation"),
    ("column ties", ties, "var z = CadGeometryGuard.Add(polyline.Elevation"),
):
    if forbidden in text:
        errors.append(label + " still contains a legacy-only Z path after shared placement resolution")

for token in (
    "CadVerticalPlacementResolver.HasConfiguredLevel(element)",
    "CadVerticalPlacementResolver.Resolve(",
    ").BottomDrawingUnits;",
    "LegacyPlacementHeightM(element, family)",
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

print("[PASS] Longitudinal/transverse reinforcement, Slab/Foundation/StructuralWall meshes and BBS shape origins share host Level placement before native replacement; policy remains fail-closed pending Stair/UI and V25 proof")
