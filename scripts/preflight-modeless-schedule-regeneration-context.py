#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"

MUTATING_SCHEDULES = {
    "DoorOpeningScheduleWindow.xaml.cs": "DoorOpeningScheduleBuilder.Build(project)",
    "RoomFinishScheduleWindow.xaml.cs": "RoomFinishScheduleBuilder.Build(project)",
    "RebarScheduleWindow.xaml.cs": "ProjectRebarScheduleBuilder.Build(project)",
}
PREVIEW_HUB = UI / "ScheduleHubWindow.xaml.cs"

errors = []
for filename, build_token in MUTATING_SCHEDULES.items():
    path = UI / filename
    if not path.is_file():
        errors.append("missing modeless schedule source: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    bind = text.find("ExistingProjectMutationContext.TryGet(_document, out var project)")
    regen = text.find("RegenerateDirty(project)")
    build = text.find(build_token)
    if bind < 0:
        errors.append(filename + " regeneration must bind canonical existing project state.")
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" in text:
        errors.append(filename + " must not regenerate a potentially detached read-only ProjectState.")
    if regen < 0 or build < 0:
        errors.append(filename + " missing expected regeneration/build path.")
    elif bind < 0 or not bind < regen < build:
        errors.append(filename + " lifecycle must be canonical bind -> regenerate -> schedule build.")

if not PREVIEW_HUB.is_file():
    errors.append("missing ScheduleHubWindow.xaml.cs")
else:
    text = PREVIEW_HUB.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(previewProject)",
    ):
        if token not in text:
            errors.append("Schedule Hub read-only preview missing token: " + token)
    if "RegenerateDirty(project)" in text:
        errors.append("Schedule Hub must never regenerate the observed read-only project directly.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: modeless schedules that regenerate bind canonical existing project state, while Schedule Hub keeps regeneration on a detached preview copy.")
