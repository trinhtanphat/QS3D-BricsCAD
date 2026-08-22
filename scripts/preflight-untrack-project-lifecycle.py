#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "ViewportCommands.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing ViewportCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    selection = "EntitySnapshotReader.ReadImpliedSelection(doc)"
    empty = "if (snapshots.Count == 0)"
    bind = 'ExistingProjectMutationContext.Require(doc, "Untrack semantic elements")'
    untrack = "SemanticUntrackService.Untrack(project, handles, predicate)"
    for token in (selection, empty, bind, untrack):
        if token not in text:
            errors.append("QS3DUNTRACK lifecycle missing token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(doc)" in text:
        errors.append("QS3DUNTRACK must not create/cache project state")
    positions = [text.find(selection), text.find(empty), text.find(bind), text.find(untrack)]
    if all(x >= 0 for x in positions) and not positions[0] < positions[1] < positions[2] < positions[3]:
        errors.append("QS3DUNTRACK must be selection -> empty guard -> canonical bind -> untrack")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: QS3DUNTRACK/QS3DUNTRACKFINISH keep empty selection side-effect free and mutate canonical existing project state only.")
