#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND_ROOT = ROOT / "src/QS3D.BricsCAD.V25"
FILES = (
    "GeneratedGeometryHealthCommands.cs",
    "GeneratedHandleOwnershipHealthCommands.cs",
    "SafeGeneratedHandleOwnershipHealthCommands.cs",
)
LOCATE_FILES = FILES[1:]
errors = []

for filename in FILES:
    path = COMMAND_ROOT / filename
    if not path.is_file():
        errors.append("missing ownership/generated Health command file: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + ": read-only Health command must not create/cache project state")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(filename + ": initial Health inspection must use read-only project lookup")
    if "health check không tạo project mới" not in text:
        errors.append(filename + ": missing explicit blocked/no-project contract")

for filename in LOCATE_FILES:
    text = (COMMAND_ROOT / filename).read_text(encoding="utf-8")
    marker = "ModelHealthWindowPresenter.Show(document, issues, issue =>"
    start = text.find(marker)
    if start < 0:
        errors.append(filename + ": missing presenter-routed model-health Locate callback")
        continue
    callback = text[start:]
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)" not in callback:
        errors.append(filename + ": Locate callback must re-resolve current project at click time")
    if "currentProject.FindElement(issue.ElementId)" not in callback:
        errors.append(filename + ": Locate callback must resolve ElementId from current project")
    if "project.FindElement(issue.ElementId)" in callback:
        errors.append(filename + ": Locate callback still captures stale ProjectState")
    if "Application.ShowModelessWindow(" in text or "new ModelHealthWindow(" in text:
        errors.append(filename + ": ownership Health must not bypass transactional Model Health presenter")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated/ownership Health commands are read-only, presenter-routed, and modeless Locate re-resolves current semantic state.")
