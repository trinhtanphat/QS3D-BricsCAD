#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RebarMeshSetupWindow.xaml.cs"
errors = []

if not WINDOW.is_file():
    errors.append("missing RebarMeshSetupWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")
    bind = 'ExistingProjectMutationContext.Require(_document, "Lưu Rebar Mesh Setup")'
    identity = "if (!ReferenceEquals(project, _project))"
    resolve = "project.FindElement(_element.Id)"
    rollback = "ProjectStateSnapshot.Capture(project)"
    for token in (bind, identity, resolve, rollback, "DocumentBoundWindowLifetime.Attach(this, _document)"):
        if token not in text:
            errors.append("Rebar Mesh Setup missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Rebar Mesh Setup Save must not create/cache replacement project state")
    positions = [text.find(bind), text.find(identity), text.find(resolve), text.find(rollback)]
    if all(x >= 0 for x in positions) and not positions[0] < positions[1] < positions[2] < positions[3]:
        errors.append("Rebar Mesh Setup Save must bind canonical project -> reject reload replacement -> re-resolve stable ElementId -> capture rollback")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Rebar Mesh Setup is document-bound and Save fails closed on reload/replacement without creating project state.")
