#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RebarHealthAllCommands.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

for token in (
    'CommandMethod("QS3DREBARHEALTHALL"',
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "ModelHealthWindowPresenter.Show(document, issues, issue =>",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
    "currentProject.FindElement(issue.ElementId)",
):
    if token not in source:
        errors.append("Rebar Health All locate contract missing token: " + token)

callback_start = source.find("ModelHealthWindowPresenter.Show(document, issues, issue =>")
if callback_start >= 0:
    callback = source[callback_start:]
    if "var element = project.FindElement(issue.ElementId);" in callback:
        errors.append("Rebar Health All Locate must not use ProjectState captured when the modeless window opened")

if "Application.ShowModelessWindow(" in source or "new ModelHealthWindow(" in source:
    errors.append("Rebar Health All must route Model Health publication through the transactional presenter")
if "ProjectContextCoordinator.GetOrCreate(document)" in source:
    errors.append("Rebar Health All must remain read-only and must not create/cache project state")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Rebar Health All remains read-only, presenter-routed, and Locate re-resolves the current project/element after reload lifecycle changes.")
