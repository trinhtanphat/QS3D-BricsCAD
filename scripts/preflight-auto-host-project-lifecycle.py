#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DAUTOLINKHOSTS must not create/cache an empty QS3D project")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append("QS3DAUTOLINKHOSTS must require an existing project with read-only lookup")
    if "Auto Host không tạo project mới" not in text:
        errors.append("missing fail-closed user-facing project lifecycle message")
    if "ProjectStateSnapshot.Capture(project)" not in text:
        errors.append("Auto Host semantic mutation batch must keep rollback snapshot coverage")

    selected_index = text.find("var selected = ReadSelectedHandles(document);")
    empty_selection_index = text.find("if (selected.Count == 0)")
    project_guard_index = text.find("if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))")
    if selected_index < 0 or empty_selection_index < 0 or project_guard_index < 0:
        errors.append("missing expected selection/project lifecycle ordering tokens")
    elif not (selected_index < empty_selection_index < project_guard_index):
        errors.append("Auto Host must reject empty selection before resolving existing project state")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Auto Host is side-effect free without selection/project state and preserves semantic rollback coverage.")
