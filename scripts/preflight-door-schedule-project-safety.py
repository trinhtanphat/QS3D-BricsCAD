#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs"
errors = []

for path in (WINDOW, COMMAND):
    if not path.is_file():
        errors.append("missing Door/Opening schedule contract file: " + str(path.relative_to(ROOT)))

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" not in text:
        errors.append("Door/Opening modeless refresh/export must resolve an existing project read-only")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Door/Opening modeless schedule must not create/cache replacement project state")
    if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text:
        errors.append("Door/Opening modeless schedule must remain source-DWG bound")

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append("QS3DDOORXLSX must require an existing QS3D project")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DDOORXLSX must not create/cache a project as an export side effect")
    if "DoorOpeningScheduleBuilder.Build(project)" not in text:
        errors.append("Door/Opening export must continue using the authoritative schedule builder")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Door/Opening schedule refresh and export require existing project state and remain DWG-bound")
