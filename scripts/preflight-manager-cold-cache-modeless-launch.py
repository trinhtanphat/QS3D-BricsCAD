#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
UI = SRC / "UI"
errors = []

contracts = [
    (
        "FloorLevelCommands.cs",
        "FloorLevelWindow.xaml.cs",
        "new FloorLevelWindow(document)",
        "!ReferenceEquals(project, _boundProject)",
    ),
    (
        "FamilyManagerCommands.cs",
        "FamilyManagerWindow.xaml.cs",
        "new FamilyManagerWindow(document)",
        "!ReferenceEquals(currentProject, _boundProject)",
    ),
    (
        "ZoneManagerCommands.cs",
        "ZoneManagerWindow.xaml.cs",
        "new ZoneManagerWindow(document)",
        "!ReferenceEquals(currentProject, _boundProject)",
    ),
]

warm_bind = "ExistingProjectMutationContext.TryGet(document, out _);"
for command_name, window_name, constructor_token, stale_guard in contracts:
    command_path = SRC / command_name
    if not command_path.is_file():
        errors.append("missing manager command: " + command_name)
        continue

    command = command_path.read_text(encoding="utf-8")
    if warm_bind not in command:
        errors.append(command_name + " must warm-bind an existing project before opening its modeless window")
    if "ProjectContextCoordinator.GetOrCreate(document)" in command:
        errors.append(command_name + " must not bootstrap project state while opening the manager")
    if constructor_token not in command:
        errors.append(command_name + " missing document-bound modeless constructor")
    elif warm_bind in command and command.index(warm_bind) > command.index(constructor_token):
        errors.append(command_name + " constructs the modeless window before existing-project warm bind")

    window_path = UI / window_name
    if not window_path.is_file():
        errors.append("missing manager window: " + window_name)
        continue

    window = window_path.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly(_document" not in window:
        errors.append(window_name + " must keep modeless refresh/read paths non-creating")
    if "ExistingProjectMutationContext.Require(_document" not in window:
        errors.append(window_name + " must keep writes on the canonical existing-project boundary")
    if stale_guard not in window:
        errors.append(window_name + " lost its exact-instance stale-project guard")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in window:
        errors.append(window_name + " must not directly create/replace project state from modeless callbacks")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Level/Family/Zone launchers warm-bind only existing projects before modeless construction; reads remain non-creating and exact-instance stale guards remain fail-closed.")
