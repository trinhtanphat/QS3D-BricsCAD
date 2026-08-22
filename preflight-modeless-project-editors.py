#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs": {
        "guard": "EnsureActive",
        "handlers": ["OnSaveClick", "OnDeleteClick", "OnActivateClick", "OnAssignClick", "OnInspectClick"],
    },
    "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs": {
        "guard": "EnsureActive",
        "handlers": ["OnDuplicateClick", "OnRenameClick", "OnDeleteClick", "OnSavePropertyClick", "OnRemovePropertyClick", "OnAssignClick"],
    },
    "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.Active.cs": {
        "guard": "EnsureActive",
        "handlers": ["OnActivateClick"],
    },
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs": {
        "guard": "EnsureActive",
        "handlers": ["OnExportClick", "OnSaveClick", "OnDeleteClick", "OnApplyClick"],
    },
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs": {
        "guard": "EnsureBoundDrawingIsActive",
        "handlers": ["OnSaveFloorClick", "OnDeleteFloorClick", "OnActivateClick", "OnAssignClick", "OnInspectSelectionClick"],
    },
    "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs": {
        "guard": "EnsureActive",
        "handlers": ["OnSaveClick", "OnRecalculateClick", "OnCommandClick"],
    },
    "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs": {
        "guard": "EnsureActive",
        "handlers": ["OnSave"],
    },
}

for relative, contract in contracts.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing modeless project editor: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    guard = contract["guard"]
    guard_text = text
    if relative.endswith("FamilyManagerWindow.Active.cs"):
        companion = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs"
        if companion.is_file():
            guard_text += "\n" + companion.read_text(encoding="utf-8")
    if "MdiActiveDocument" not in guard_text:
        errors.append(relative + " must compare its bound document to MdiActiveDocument")
    if relative.endswith("CurtainWallWindow.xaml.cs"):
        if "private readonly Document _document" not in text or "CurtainWallWindow(Document document)" not in text:
            errors.append(relative + " must be constructed with and retain its source drawing")
    if relative.endswith("RebarMeshSetupWindow.xaml.cs"):
        if "private readonly Document _document" not in text or "RebarMeshSetupWindow(Document document" not in text:
            errors.append(relative + " must be constructed with and retain its source drawing")
        if "project.FindElement(_element.Id)" not in text:
            errors.append(relative + " must re-resolve its semantic target before saving")
    for handler in contract["handlers"]:
        match = re.search(r"private\s+void\s+" + re.escape(handler) + r"\s*\([^)]*\)\s*\{", text)
        if not match:
            errors.append(relative + " missing handler: " + handler)
            continue
        start = match.end()
        next_handler = re.search(r"\n\s*private\s+void\s+", text[start:])
        end = start + next_handler.start() if next_handler else len(text)
        body = text[start:end]
        guard_pos = body.find(guard + "(")
        project_pos = body.find("ProjectContextCoordinator.GetOrCreate(_document)")
        mutation_markers = (
            "ProjectZoneService.", "ProjectFamilyService.", "ProjectFamilyActivationService.",
            "ProjectMaterialCatalog.", "element.SetProperty(", "ProjectFloorService.", "SendStringToExecute(",
            "ApplyFamilyValue(", "RegenerateDirty("
        )
        first_mutation = min((body.find(token) for token in mutation_markers if body.find(token) >= 0), default=-1)
        if guard_pos < 0:
            errors.append(relative + "/" + handler + " must guard the bound drawing before mutation")
        elif project_pos >= 0 and guard_pos > project_pos:
            errors.append(relative + "/" + handler + " must guard before resolving the bound project")
        elif first_mutation >= 0 and guard_pos > first_mutation:
            errors.append(relative + "/" + handler + " must guard before project/CAD mutation")

curtain_hub = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallHubCommands.cs"
if not curtain_hub.is_file():
    errors.append("missing CurtainWallHubCommands.cs")
elif "new CurtainWallWindow(document)" not in curtain_hub.read_text(encoding="utf-8"):
    errors.append("Curtain Wall Hub must pass the source drawing into its modeless editor")

mesh_command = ROOT / "src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs"
if not mesh_command.is_file():
    errors.append("missing RebarMeshSetupCommands.cs")
elif "new RebarMeshSetupWindow(document, project, element" not in mesh_command.read_text(encoding="utf-8"):
    errors.append("Rebar Mesh Setup command must pass the source drawing into its modeless editor")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: modeless Floor/Zone/Family/Material/Curtain/Rebar Mesh editors are bound to their source DWG and guard project/CAD mutation.")
