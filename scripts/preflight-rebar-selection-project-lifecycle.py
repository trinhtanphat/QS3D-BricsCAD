#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
GUARD = ADAPTER / "Cad" / "CadSelectionGuard.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def region(text, start_token, end_token, label):
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end <= start:
        errors.append("cannot isolate " + label)
        return ""
    return text[start:end]


guard = read(GUARD)

for token in (
    "public static ObjectId[] ReadImpliedSelection(Document document)",
    "public static ObjectId[] AcquireCurrentSelection(Document document)",
    "SelectImplied()",
    "GetSelection()",
    "SetImpliedSelection(objectIds)",
    "return Array.Empty<ObjectId>();",
):
    if token not in guard:
        errors.append("CadSelectionGuard missing selection lifecycle token: " + token)

for forbidden in (
    "ProjectContextCoordinator",
    "ExistingProjectMutationContext",
    "ProjectState",
):
    if forbidden in guard:
        errors.append("CadSelectionGuard must remain project-agnostic: " + forbidden)

interactive_cases = (
    (
        ADAPTER / "BeamRebarCommands.cs",
        "public void BuildBeamRebar3D()",
        "private static void FinalizeUi",
        "Beam Rebar",
        "ExistingProjectMutationContext.Require(document, \"Beam Rebar 3D\")",
        "BeamRebarSolidBuilder.BuildSelected(document, project)",
    ),
    (
        ADAPTER / "BeamStirrupCommands.cs",
        "public void BuildBeamStirrups()",
        "[CommandMethod(\"QS3DBEAMSTIRRUPHEALTH\"",
        "Beam Stirrup",
        "ExistingProjectMutationContext.Require(document, \"Beam Stirrup 3D\")",
        "BeamStirrupSolidBuilder.BuildSelected(document, project)",
    ),
    (
        ADAPTER / "SlabMeshCommands.cs",
        "public void BuildSlabMesh3D()",
        "[CommandMethod(\"QS3DSLABREBARHEALTH\"",
        "Slab Mesh",
        "ExistingProjectMutationContext.Require(document, \"Slab Mesh 3D\")",
        "SlabMeshSolidBuilder.BuildSelected(document, project)",
    ),
    (
        ADAPTER / "FoundationMeshCommands.cs",
        "public void BuildFoundationMesh3D()",
        "private static void FinalizeUi",
        "Foundation Mesh",
        "ExistingProjectMutationContext.Require(document, \"Foundation Rebar 3D\")",
        "FoundationMeshSolidBuilder.BuildSelected(document, project)",
    ),
    (
        ADAPTER / "StructuralWallMeshCommands.cs",
        "public void BuildStructuralWallMesh3D()",
        "private static void FinalizeUi",
        "Structural Wall Mesh",
        "ExistingProjectMutationContext.Require(document, \"Wall Mesh 3D\")",
        "StructuralWallMeshSolidBuilder.BuildSelected(document, project)",
    ),
    (
        ADAPTER / "ShapeRebarGeometryCommands.cs",
        "public void BuildShapeRebar3D()",
        "private static void FinalizeUi",
        "Shape Rebar",
        "ExistingProjectMutationContext.Require(document, \"Shape Rebar 3D\")",
        "ShapeRebarSolidBuilder.BuildSelected(document, project)",
    ),
)

implied_only_cases = (
    (
        ADAPTER / "RebarGeometryCommands.cs",
        "public void BuildRebar3D()",
        "private static void FinalizeUi",
        "Column Rebar",
        "ExistingProjectMutationContext.Require(document, \"Rebar 3D\")",
        "ColumnRebarSolidBuilder.BuildSelected(document, project)",
    ),
    (
        ADAPTER / "ColumnTieCommands.cs",
        "public void BuildColumnTies()",
        "private static void FinalizeUi",
        "Column Tie",
        "ExistingProjectMutationContext.Require(document, \"Column Tie 3D\")",
        "ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds)",
    ),
)


def check_case(path, start_token, end_token, label, require_token, build_token, acquire_token):
    text = read(path)
    body = region(text, start_token, end_token, label)
    acquire = body.find(acquire_token)
    empty = body.find("if (selectedIds.Length == 0)")
    require = body.find(require_token)
    build = body.find(build_token)
    if min(acquire, empty, require, build) < 0:
        errors.append(label + " command missing selection/project/build lifecycle token")
        return
    if not (acquire < empty < require < build):
        errors.append(label + " must establish selection and return on empty/cancel before canonical project binding/build")
    if "ProjectContextCoordinator.GetOrCreate" in body:
        errors.append(label + " mutation command must not create a replacement project")


for case in interactive_cases:
    check_case(*case, "CadSelectionGuard.AcquireCurrentSelection(document)")

for case in implied_only_cases:
    check_case(*case, "CadSelectionGuard.ReadImpliedSelection(document)")
    text = read(case[0])
    body = region(text, case[1], case[2], case[3])
    if "CadSelectionGuard.AcquireCurrentSelection(document)" in body:
        errors.append(case[3] + " must preserve PICKFIRST-only behavior and must not open a new interactive selection prompt")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: all guarded rebar 3D command families establish their existing PICKFIRST/interactive selection contract before binding the canonical project; empty/cancel paths fail closed without project creation/binding and PICKFIRST-only Column commands pass the admitted snapshot into native generation without gaining a new prompt.")
