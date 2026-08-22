#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "Build3DCommands.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing Build3DCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    selection = "EntitySnapshotReader.ReadCurrentSelection(document)"
    empty = "if (snapshots.Count == 0)"
    bind = 'ExistingProjectMutationContext.Require(document, "Build 3D")'
    for token in (selection, empty, bind):
        if token not in text:
            errors.append("QS3DBUILD3D missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DBUILD3D must not create/cache project state during rebuild")
    positions = [text.find(selection), text.find(empty), text.find(bind)]
    if all(x >= 0 for x in positions) and not positions[0] < positions[1] < positions[2]:
        errors.append("QS3DBUILD3D lifecycle must be selection -> empty guard -> canonical existing-project bind")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: QS3DBUILD3D keeps empty selection side-effect free and rebuilds only against canonical existing project state.")
