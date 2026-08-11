#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"

COMMANDS = {
    "RebarHealthCommands.cs": True,
    "ColumnTieHealthCommands.cs": True,
    "ShapeRebarHealthCommands.cs": True,
    "FoundationMeshHealthCommands.cs": True,
    "StructuralWallMeshHealthCommands.cs": False,
}

errors = []

for name, has_modeless_locate in COMMANDS.items():
    path = SRC / name
    if not path.is_file():
        errors.append("missing Health command source: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(name + " must resolve the inspection snapshot with TryGetReadOnly.")
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append(name + " must not create/cache project state merely to run a Health inspection.")
    if "lệnh kiểm tra không tạo project mới" not in text:
        errors.append(name + " must explain that a blocked read-only Health inspection does not create a project.")

    if has_modeless_locate:
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)" not in text:
            errors.append(name + " Locate callback must re-resolve the current read-only project at click time.")
        if "currentProject.FindElement(issue.ElementId)" not in text:
            errors.append(name + " Locate callback must resolve the selected element from the current project.")
        if "var element = project.FindElement(issue.ElementId);" in text:
            errors.append(name + " Locate callback must not use the ProjectState captured when the window opened.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: focused Health inspections are read-only, and modeless Locate callbacks re-resolve current project state by stable element identity.")
