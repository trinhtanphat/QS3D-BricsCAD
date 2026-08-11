#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TABLE_COMMANDS = (
    "BqNativeTableCommands.cs",
    "BbsNativeTableCommands.cs",
    "DoorOpeningNativeTableCommands.cs",
    "RoomFinishNativeTableCommands.cs",
    "MaterialUsageNativeTableCommands.cs",
    "SemanticElementTableCommands.cs",
)
COMMAND_ROOT = ROOT / "src/QS3D.BricsCAD.V25"
errors = []

for filename in TABLE_COMMANDS:
    path = COMMAND_ROOT / filename
    if not path.is_file():
        errors.append("missing native documentation table command file: " + filename)
        continue

    text = path.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + ": native documentation table command must not create/cache an empty project")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(filename + ": missing existing-project/read-only lookup contract")
    if "RequireExistingProject" not in text:
        errors.append(filename + ": mutating native table entrypoints must use an explicit existing-project guard")

    health_index = text.find("TABLEHEALTH")
    if health_index >= 0:
        health_tail = text[health_index:]
        if "TryGetReadOnly(document, out var project)" not in health_tail:
            errors.append(filename + ": health path must remain read-only and must not create project state")

for filename in ("BqNativeTableCommands.cs", "SemanticElementTableCommands.cs"):
    path = COMMAND_ROOT / filename
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for operation_token in (
        "var project = RequireExistingProject(document",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    ):
        if operation_token not in text:
            errors.append(filename + ": regression guard missing token: " + operation_token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: native BQ/BBS/Door-Opening/Room-Finish/Material/Semantic tables require an existing QS3D project and health remains read-only.")
