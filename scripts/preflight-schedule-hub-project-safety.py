#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ScheduleHubWindow.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ClearSnapshotCounts();",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(previewProject)",
        "ProjectQuantityReportBuilder.Group(previewProject)",
        "RoomFinishScheduleBuilder.Build(previewProject)",
        "DoorOpeningScheduleBuilder.Build(previewProject)",
        "CurtainWallScheduleBuilder.Build(previewProject)",
        "MaterialUsageScheduleBuilder.Build(previewProject)",
    ):
        if token not in text:
            errors.append("ScheduleHubWindow.xaml.cs missing project-safety token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Schedule Hub is a reporting surface and must not create/cache replacement project state")
    for forbidden in (
        "ProjectQuantityReportBuilder.Group(project)",
        "RoomFinishScheduleBuilder.Build(project)",
        "DoorOpeningScheduleBuilder.Build(project)",
        "CurtainWallScheduleBuilder.Build(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
        "RegenerateDirty(project)",
    ):
        if forbidden in text:
            errors.append("Schedule Hub automatic preview must not regenerate/build schedules on the live project: " + forbidden)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Schedule Hub is source-DWG bound, existing-project-only, clears stale counts when project state disappears, and regenerates/builds automatic previews only on a detached semantic copy")
