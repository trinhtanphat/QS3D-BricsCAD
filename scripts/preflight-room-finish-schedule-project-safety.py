#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs"
errors = []

for path in (WINDOW, COMMAND):
    if not path.is_file():
        errors.append("missing HT_Phòng schedule contract file: " + str(path.relative_to(ROOT)))

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
<<<<<<< HEAD
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" not in text:
        errors.append("HT_Phòng modeless refresh/export must require an existing project")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("HT_Phòng modeless schedule must not create/cache replacement project state")
    if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text:
        errors.append("HT_Phòng modeless schedule must remain source-DWG bound")
    if "RoomFinishScheduleBuilder.Build(snapshot)" not in text:
        errors.append("HT_Phòng modeless schedule must use the authoritative builder")
=======
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "RoomFinishScheduleBuilder.Build(snapshot)",
        "DocumentBoundWindowLifetime.Attach(this, _document);",
    ):
        if token not in text:
            errors.append("HT_Phòng modeless schedule missing detached/read-only token: " + token)
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "RoomFinishScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("HT_Phòng modeless schedule must not create/bind/regenerate live project state: " + forbidden)
>>>>>>> origin/main

    for token in (
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
    ):
        if token not in text:
            errors.append("HT_Phong modeless schedule missing detached read-only token: " + token)
    if "RegenerateDirty(project)" in text or "RoomFinishScheduleBuilder.Build(project)" in text:
        errors.append("HT_Phong modeless schedule must not mutate or build from the live project")

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "RoomFinishScheduleBuilder.Build(snapshot)",
    ):
        if token not in text:
            errors.append("QS3DFINISHXLSX missing read-only detached-export token: " + token)
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "RoomFinishScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("QS3DFINISHXLSX must not mutate/bind the live project during export: " + forbidden)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] HT_Phòng modeless and command exports remain source-DWG/existing-project bound and regenerate detached read-only snapshots")
