#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "workspace_xaml": ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "workspace_code": ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs",
    "workspace_vm": ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs",
    "property_vm": ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs",
    "ribbon": ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "hub": ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "beam_rebar": ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs",
    "beam_stirrup": ROOT / "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs",
    "column_tie": ROOT / "src/QS3D.BricsCAD.V25/ColumnTieCommands.cs",
    "column_tie_health": ROOT / "src/QS3D.BricsCAD.V25/ColumnTieHealthCommands.cs",
    "slab_mesh": ROOT / "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs",
    "wall_mesh": ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs",
    "wall_mesh_health": ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshHealthCommands.cs",
    "curtain_build": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "curtain_health": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs",
}

for name, path in files.items():
    if not path.is_file():
        errors.append("missing BLT workspace file: " + str(path.relative_to(ROOT)))

for key in ("workspace_xaml", "hub"):
    path = files[key]
    if not path.is_file():
        continue
    try:
        ET.parse(path)
    except ET.ParseError as exc:
        errors.append(str(path.relative_to(ROOT)) + " is not well-formed XML/XAML: " + str(exc))

checks = {
    "workspace_xaml": [
        "PropertyBooleanEditor", "PropertyChoiceEditor", "PropertyScopes", "SelectedPropertyScope",
        "OnResetPropertyClick", "CanReset", "OnWallJunctionsClick", "OnWallSnapPreviewClick",
        "OnWallSnapApplyClick", "OnAutoHostClick", "OnFocusSelectedClick", "OnIsolateSelectedClick",
        "OnUnisolateClick", "Snap xem", "Snap áp", "Auto Host",
    ],
    "workspace_code": [
        "SemanticReferenceHandles.MatchesSelection", "SetSelectedElement(element)", "ShowFamilyProperties",
        "QS3DWALLJUNCTIONS", "QS3DWALLSNAPPREVIEW", "QS3DWALLSNAPAPPLY", "QS3DAUTOLINKHOSTS",
        "QS3DFOCUS", "QS3DISOLATE", "QS3DUNISOLATE", "OnResetPropertyClick",
    ],
    "workspace_vm": [
        "FamilyScope", "InstanceScope", "PropertyScopes", "SelectedPropertyScope", "SetSelectedElement",
        "LoadInstanceProperties", "ApplyInstanceProperty", "isInherited", "instance override", "row.CanReset",
        "Chọn một cấu kiện semantic trước khi chuyển sang thuộc tính Instance",
    ],
    "property_vm": [
        "TextEditor", "BooleanEditor", "ChoiceEditor", "BooleanValue", "Choices", "CanReset", "ResetValue",
    ],
    "ribbon": [
        '"QS3DWALLJUNCTIONS"', '"QS3DWALLSNAPPREVIEW"', '"QS3DWALLSNAPAPPLY"', '"QS3DAUTOLINKHOSTS"',
        '"QS3DFOCUS"', '"QS3DISOLATE"', '"QS3DUNISOLATE"', '"QS3DSECTIONBOX"',
        '"QS3DREBAR3DSHAPE"', '"QS3DBEAMREBAR3D"', '"QS3DREBARSTIRRUP3D"', '"QS3DREBARSTIRRUPHEALTH"',
        '"QS3DREBARTIES3D"', '"QS3DREBARTIEHEALTH"', '"QS3DSLABREBAR3D"', '"QS3DSLABREBARHEALTH"',
        '"QS3DWALLREBAR3D"', '"QS3DWALLREBARHEALTH"', '"QS3DCURTAIN"', '"QS3DCURTAIN3D"',
        '"QS3DCURTAINFRAMEHEALTH"', '"QS3DHEALTHALL"',
    ],
    "hub": [
        'Tag="QS3DWALLJUNCTIONS"', 'Tag="QS3DWALLSNAPPREVIEW"', 'Tag="QS3DWALLSNAPAPPLY"',
        'Tag="QS3DAUTOLINKHOSTS"', 'Tag="QS3DCUTOPENINGS"', 'Tag="QS3DSECTIONBOX"',
        'Tag="QS3DREBAR3DSHAPE"', 'Tag="QS3DBEAMREBAR3D"', 'Tag="QS3DREBARSTIRRUP3D"', 'Tag="QS3DREBARSTIRRUPHEALTH"',
        'Tag="QS3DREBARTIES3D"', 'Tag="QS3DREBARTIEHEALTH"', 'Tag="QS3DSLABREBAR3D"', 'Tag="QS3DSLABREBARHEALTH"',
        'Tag="QS3DWALLREBAR3D"', 'Tag="QS3DWALLREBARHEALTH"', 'Tag="QS3DCURTAIN"', 'Tag="QS3DCURTAIN3D"',
        'Tag="QS3DCURTAINFRAMEHEALTH"', 'Tag="QS3DHEALTHALL"',
    ],
    "beam_rebar": [
        'CommandMethod("QS3DBEAMREBAR3D"', "BeamRebarSolidBuilder.BuildSelected",
    ],
    "beam_stirrup": [
        'CommandMethod("QS3DREBARSTIRRUP3D"', 'CommandMethod("QS3DREBARSTIRRUPHEALTH"',
    ],
    "column_tie": [
        'CommandMethod("QS3DREBARTIES3D"', "ColumnTieSolidBuilder.BuildSelected",
    ],
    "column_tie_health": [
        'CommandMethod("QS3DREBARTIEHEALTH"', "GeneratedTieRebarHealthService().Inspect",
    ],
    "slab_mesh": [
        'CommandMethod("QS3DSLABREBAR3D"', 'CommandMethod("QS3DSLABREBARHEALTH"',
    ],
    "wall_mesh": [
        'CommandMethod("QS3DWALLREBAR3D"', "StructuralWallMeshSolidBuilder.BuildSelected",
    ],
    "wall_mesh_health": [
        'CommandMethod("QS3DWALLREBARHEALTH"', "GeneratedWallMeshHealthService().Inspect",
    ],
    "curtain_build": [
        'CommandMethod("QS3DCURTAIN3D"', "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
    ],
    "curtain_health": [
        'CommandMethod("QS3DCURTAINFRAMEHEALTH"', "GeneratedCurtainFrameHealthService().Inspect",
    ],
}

for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing BLT workspace token: " + needle)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BLT-style Workspace/Ribbon/Hub property scopes, typed editors, review/section actions, wall workflows, curtain host/frame workflow, slab/wall mesh rebar, health and major generated-geometry entry points are present; key XAML is well formed.")
