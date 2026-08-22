#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Level native host file: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


resolver = read("src/QS3D.BricsCAD.V25/Cad/CadVerticalPlacementResolver.cs")
wall = read("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs")
path_wall = read("src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs")
wall_pier = read("src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs")
structural = read("src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs")
policy = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")

for token in (
    "ElementVerticalPlacementService.Resolve(project, element, sourceBaseM, legacyHeightM, legacyBottomOffsetM)",
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Native 3D Level placement")',
    "CadGeometryGuard.ToMeters(document, sourceBaseDrawingUnits",
    "semantic.UsesBottomLevel",
    "sourceBaseDrawingUnits,",
    "legacyBottomOffsetM",
    "semantic.BottomElevationM",
    "semantic.HeightM",
):
    if token not in resolver:
        errors.append("CAD Level resolver is missing canonical Core-placement token: " + token)

semantic_resolve = resolver.find("var semantic = ElementVerticalPlacementService.Resolve(")
qualification_guard = resolver.find("LevelReferenceNativeIntegrationPolicy.EnsureQualified(element", semantic_resolve)
bottom_conversion = resolver.find("var bottomDrawing =", qualification_guard)
if min(semantic_resolve, qualification_guard, bottom_conversion) < 0 or not semantic_resolve < qualification_guard < bottom_conversion:
    errors.append("CAD Level resolver must validate semantic references, then refuse unqualified use, before native coordinate conversion.")

for label, text, minimum_calls in (
    ("LINE wall", wall, 1),
    ("open-POLYLINE wall", path_wall, 1),
    ("WallPier profile", wall_pier, 1),
    ("structural host", structural, 2),
):
    calls = text.count("CadVerticalPlacementResolver.Resolve(")
    if calls < minimum_calls:
        errors.append(f"{label} must resolve Level-aware native placement before generation (found {calls}, expected >= {minimum_calls}).")

for label, text in (
    ("LINE wall", wall),
    ("open-POLYLINE wall", path_wall),
    ("WallPier profile", wall_pier),
):
    resolve = text.find("CadVerticalPlacementResolver.Resolve(")
    replace = text.find("GeneratedGeometryService.PrepareReplacement")
    if resolve < 0 or replace < 0 or resolve >= replace:
        errors.append(label + " must validate resolved placement before replacing native ownership.")

if "category == ElementCategory.Slab || category == ElementCategory.Foundation || category == ElementCategory.Column" not in structural:
    errors.append("Closed structural Level placement must stay limited to the quantity-qualified Slab/Foundation/Column host categories.")
if "category == ElementCategory.Railing" not in structural or 'case ElementCategory.Earthwork:' not in structural:
    errors.append("Railing/Stair/Earthwork must retain their legacy placement until their quantities and dependents share the Level resolver.")
if "placement.BottomDrawingUnits, polyline.Elevation" not in structural:
    errors.append("Closed structural footprints must translate from source elevation to the resolved absolute bottom.")
if 'update.Element.Properties["HeightM"] = update.HeightM' not in wall:
    errors.append("LINE wall must preserve the legacy HeightM property so clearing Level references restores legacy geometry.")
if 'update.Element.Properties["HeightM"] = update.HeightM' not in path_wall:
    errors.append("POLYLINE wall must preserve the legacy HeightM property so clearing Level references restores legacy geometry.")
if 'update.Element.Properties["HeightM"] = update.HeightM' not in wall_pier:
    errors.append("WallPier must preserve the legacy HeightM property so clearing Level references restores legacy geometry.")

if "return false;" not in policy:
    errors.append("Level native integration policy must stay release-blocked until opening, curtain and rebar dependents share the resolver.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: first-wave wall and structural host builders consume the canonical Level resolver, preserve legacy dimensions, validate before replacement, and remain release-blocked pending dependent integration.")
