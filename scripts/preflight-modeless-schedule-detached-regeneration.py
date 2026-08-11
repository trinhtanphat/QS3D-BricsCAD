#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"

CASES = {
    "DoorOpeningScheduleWindow.xaml.cs": "DoorOpeningScheduleBuilder.Build(snapshot)",
    "RoomFinishScheduleWindow.xaml.cs": "RoomFinishScheduleBuilder.Build(snapshot)",
    "RebarScheduleWindow.xaml.cs": "ProjectRebarScheduleBuilder.Build(snapshot)",
}

errors = []
for filename, build_token in CASES.items():
    path = UI / filename
    if not path.is_file():
        errors.append("missing modeless schedule source: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    lookup = "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)"
    snapshot = "ProjectStateSnapshot.CreateDetachedCopy(project)"
    regenerate = "RegenerateDirty(snapshot)"
    positions = {
        lookup: text.find(lookup),
        snapshot: text.find(snapshot),
        regenerate: text.find(regenerate),
        build_token: text.find(build_token),
    }
    for token, position in positions.items():
        if position < 0:
            errors.append(filename + " missing detached read-only schedule token: " + token)
    if all(position >= 0 for position in positions.values()) and not positions[lookup] < positions[snapshot] < positions[regenerate] < positions[build_token]:
        errors.append(filename + " lifecycle must be read-only lookup -> detached copy -> preview regen -> schedule build.")

    for forbidden in ("ExistingProjectMutationContext", "RegenerateDirty(project)"):
        if forbidden in text:
            errors.append(filename + " pure schedule path must not mutate/bind live project state: " + forbidden)

hub = UI / "ScheduleHubWindow.xaml.cs"
if not hub.is_file():
    errors.append("missing ScheduleHubWindow.xaml.cs")
else:
    text = hub.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(previewProject)",
    ):
        if token not in text:
            errors.append("Schedule Hub read-only preview missing token: " + token)
    if "RegenerateDirty(project)" in text or "ExistingProjectMutationContext" in text:
        errors.append("Schedule Hub must keep preview regeneration detached and read-only.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: modeless schedule refresh/export paths regenerate detached snapshots only, and Schedule Hub preserves detached preview regeneration.")
