#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml.cs"
errors = []

if not WINDOW.is_file():
    errors.append("missing RoomFinishScheduleWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")
    canonical = "ExistingProjectMutationContext.TryGet(_document, out var project)"
    required = (
        "private IReadOnlyList<RoomFinishScheduleRow> BuildCurrentRows(out int regenerated)",
        canonical,
        "RegenerateDirty(project)",
        "RoomFinishScheduleBuilder.Build(project)",
        "var current = BuildCurrentRows(out var regenerated);",
        "RoomFinishXlsxExporter.Export(dialog.FileName, current);",
    )
    for token in required:
        if token not in text:
            errors.append("HT_PHÒNG schedule missing freshness token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("HT_PHÒNG modeless schedule must not create/cache replacement project state")

    build_current = text.find("private IReadOnlyList<RoomFinishScheduleRow> BuildCurrentRows")
    apply_filter = text.find("private void ApplyFilter", build_current)
    refresh_body = text[build_current:apply_filter] if build_current >= 0 and apply_filter > build_current else ""
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" in refresh_body:
        errors.append("HT_PHÒNG refresh/export regeneration must not mutate a detached read-only project")

    export_pos = text.find("private void OnExportClick")
    refresh_pos = text.find("private void RefreshRows", export_pos)
    body = text[export_pos:refresh_pos] if export_pos >= 0 and refresh_pos > export_pos else ""
    dialog_pos = body.find("dialog.ShowDialog() != true")
    build_pos = body.find("BuildCurrentRows(out var regenerated)")
    exporter_pos = body.find("RoomFinishXlsxExporter.Export(dialog.FileName, current)")
    stale_export_pos = body.find("RoomFinishXlsxExporter.Export(dialog.FileName, _rows)")
    if min(dialog_pos, build_pos, exporter_pos) < 0 or not dialog_pos < build_pos < exporter_pos:
        errors.append("HT_PHÒNG export must wait for Save confirmation before rebuilding canonical current rows and exporting them")
    if stale_export_pos >= 0:
        errors.append("HT_PHÒNG export must not export cached _rows after project reload/change")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] HT_PHÒNG modeless refresh/export regenerates the canonical existing project after active-DWG/Save confirmation and never exports stale cached schedule data")
