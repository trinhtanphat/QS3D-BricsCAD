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
        "private IReadOnlyList<DoorOpeningScheduleRow> BuildCurrentRows(out int regenerated)",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "RegenerateDirty(project)",
        "DoorOpeningScheduleBuilder.Build(project)",
        "var current = BuildCurrentRows(out var regenerated);",
        "DoorOpeningXlsxExporter.Export(dialog.FileName, current);",
    )
    for token in required:
        if token not in text:
            errors.append("Door/Opening schedule missing freshness token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Door/Opening modeless schedule must not create/cache replacement project state")

    export_pos = text.find("private void OnExportClick")
    refresh_pos = text.find("private void RefreshRows", export_pos)
    body = text[export_pos:refresh_pos] if export_pos >= 0 and refresh_pos > export_pos else ""
    dialog_pos = body.find("dialog.ShowDialog() != true")
    build_pos = body.find("BuildCurrentRows(out var regenerated)")
    exporter_pos = body.find("DoorOpeningXlsxExporter.Export(dialog.FileName, current)")
    stale_export_pos = body.find("DoorOpeningXlsxExporter.Export(dialog.FileName, _rows)")
    if min(dialog_pos, build_pos, exporter_pos) < 0 or not dialog_pos < build_pos < exporter_pos:
        errors.append("Door/Opening export must wait for Save confirmation before rebuilding current rows and exporting them")
    if stale_export_pos >= 0:
        errors.append("Door/Opening export must not export cached _rows after project reload/change")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Door/Opening XLSX export re-resolves existing project state after Save confirmation and never exports stale cached schedule data")
