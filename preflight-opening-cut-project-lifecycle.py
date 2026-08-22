#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []

straight_path = ADAPTER / "OpeningBooleanCommands.cs"
curved_path = ADAPTER / "CurvedOpeningBooleanCommands.cs"

for path in (straight_path, curved_path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))

if straight_path.is_file():
    text = straight_path.read_text(encoding="utf-8")
    selection = "EntitySnapshotReader.ReadCurrentSelection(document)"
    empty = "if (snapshots.Count == 0)"
    selected_bind = 'ExistingProjectMutationContext.Require(document, "Selected physical opening cut")'
    execute_bind = "ExistingProjectMutationContext.Require(document, label)"
    for token in (selection, empty, selected_bind, execute_bind):
        if token not in text:
            errors.append("OpeningBooleanCommands missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("straight opening cut must never create/cache a project")
    positions = [text.find(selection), text.find(empty), text.find(selected_bind)]
    if all(x >= 0 for x in positions) and not positions[0] < positions[1] < positions[2]:
        errors.append("selected opening cut must be selection -> empty guard -> canonical existing-project bind")

if curved_path.is_file():
    text = curved_path.read_text(encoding="utf-8")
    token = 'ExistingProjectMutationContext.Require(document, "Curved physical opening cut")'
    if token not in text:
        errors.append("curved opening cut missing canonical existing-project bind")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("curved opening cut must never create/cache a project")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: straight and curved physical opening cuts mutate canonical existing project state; selected cut remains selection-first and side-effect free on cancel.")
