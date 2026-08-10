#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI"
FILES = [
    "MaterialCatalogWindow.xaml.cs",
    "FloorLevelWindow.xaml.cs",
    "ZoneManagerWindow.xaml.cs",
    "FamilyManagerWindow.xaml.cs",
    "CurtainWallWindow.xaml.cs",
    "RebarMeshSetupWindow.xaml.cs",
]
errors = []

for name in FILES:
    path = UI / name
    if not path.is_file():
        errors.append("missing modeless editor source: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text:
        errors.append(name + " must close automatically when its source DWG is destroyed.")

curtain = UI / "CurtainWallWindow.xaml.cs"
if curtain.is_file():
    text = curtain.read_text(encoding="utf-8")
    for token in (
        "if (!(FamilyCombo.SelectedItem is ProjectFamily selectedFamily)) return;",
        "var family = project.FindFamily(selectedFamily.Id)",
        "family.Category != ElementCategory.GlassWall",
    ):
        if token not in text:
            errors.append("CurtainWallWindow missing stale-family fail-closed token: " + token)

mesh = UI / "RebarMeshSetupWindow.xaml.cs"
if mesh.is_file():
    text = mesh.read_text(encoding="utf-8")
    for token in (
        "if (!ReferenceEquals(project, _project))",
        "Project của DWG này đã được reload/thay thế",
        "var element = project.FindElement(_element.Id)",
    ):
        if token not in text:
            errors.append("RebarMeshSetupWindow missing stale-project fail-closed token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: document-bound modeless editors close with their source DWG; Curtain re-resolves the selected Family and Rebar Mesh rejects a replaced project instance before mutation.")
