#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []

cases = [
    ("CurtainWallBuildCommands.cs", 'ExistingProjectMutationContext.Require(document, "Curtain 3D")'),
    ("CurtainWallFrameCommands.cs", 'ExistingProjectMutationContext.Require(document, "Curtain Frames 3D")'),
]

for filename, bind in cases:
    path = ADAPTER / filename
    if not path.is_file():
        errors.append("missing source: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    selection = "EntitySnapshotReader.ReadCurrentSelection(document)"
    empty = "if (selected.Count == 0)"
    for token in (selection, empty, bind):
        if token not in text:
            errors.append(filename + " missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + " must not create/cache a project as a generated-geometry side effect")
    positions = [text.find(selection), text.find(empty), text.find(bind)]
    if all(x >= 0 for x in positions) and not positions[0] < positions[1] < positions[2]:
        errors.append(filename + " lifecycle must be selection -> empty guard -> canonical existing-project bind")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Curtain host/frame generated-geometry commands keep empty selection side-effect free and bind canonical existing project state before mutation.")
