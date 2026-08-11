#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml.cs"
errors = []

if not WINDOW.is_file():
    errors.append("missing DoorOpeningScheduleWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")
    required = (
        "using QS3D.Core.Persistence;",
        "private IReadOnlyList<DoorOpeningScheduleRow> BuildCurrentRows(out int regenerated)",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "DoorOpeningScheduleBuilder.Build(snapshot)",
        "var current = BuildCurrentRows(out var regenerated);",
        "DoorOpeningXlsxExporter.Export(dialog.FileName, current);",
    )
    for token in required:
        if token not in text:
            errors.append("Door/Opening schedule missing detached freshness token: " + token)

    build_start = text.find("private IReadOnlyList<DoorOpeningScheduleRow> BuildCurrentRows")
    build_end = text.find("private void ApplyFilter", build_start)
    body = text[build_start:build_end] if build_start >= 0 and build_end > build_start else ""
    for forbidden in (
        "ExistingProjectMutationContext",
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "RegenerateDirty(project)",
        "DoorOpeningScheduleBuilder.Build(project)",
    ):
        if forbidden in body:
            errors.append("Door/Opening read-only refresh must not mutate/bind live project state: " + forbidden)

    lookup = body.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)")
    snapshot = body.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
    regen = body.find("RegenerateDirty(snapshot)")
    build = body.find("DoorOpeningScheduleBuilder.Build(snapshot)")
    if min(lookup, snapshot, regen, build) < 0 or not lookup < snapshot < regen < build:
        errors.append("Door/Opening refresh order must be read-only lookup -> detached copy -> regenerate -> schedule build")

    export_start = text.find("private void OnExportClick")
    refresh_start = text.find("private void RefreshRows", export_start)
    export_body = text[export_start:refresh_start] if export_start >= 0 and refresh_start > export_start else ""
    dialog = export_body.find("dialog.ShowDialog() != true")
    current = export_body.find("BuildCurrentRows(out var regenerated)")
    exporter = export_body.find("DoorOpeningXlsxExporter.Export(dialog.FileName, current)")
    if min(dialog, current, exporter) < 0 or not dialog < current < exporter:
        errors.append("Door/Opening export must confirm Save before detached fresh-row build and export")
    if "DoorOpeningXlsxExporter.Export(dialog.FileName, _rows)" in export_body:
        errors.append("Door/Opening export must not export stale cached _rows")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Door/Opening modeless refresh/export regenerates only a detached read-only snapshot and never mutates/binds live project state")
