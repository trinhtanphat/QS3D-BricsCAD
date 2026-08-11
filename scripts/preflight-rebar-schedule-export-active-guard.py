#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs"
errors = []

if not WINDOW.is_file():
    errors.append("missing RebarScheduleWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")

    export_start = text.find("private void OnExportClick")
    next_method = text.find("private RebarScheduleRow ResolveCurrentRow", export_start)
    export_body = text[export_start:next_method] if export_start >= 0 and next_method > export_start else ""

    pre_guard = export_body.find('EnsureActive("xuất BBS XLSX")')
    dialog_create = export_body.find("new SaveFileDialog")
    dialog_show = export_body.find("dialog.ShowDialog(this) != true")
    post_guard = export_body.find('EnsureActive("xuất BBS XLSX")', pre_guard + 1) if pre_guard >= 0 else -1
    rebuild = export_body.find("BuildCurrentRows()")
    exporter = export_body.find("XlsxRebarScheduleExporter.Export(dialog.FileName, _rows)")

    if min(pre_guard, dialog_create, dialog_show, post_guard, rebuild, exporter) < 0:
        errors.append("BBS export is missing one or more ownership/freshness/export tokens")
    elif not pre_guard < dialog_create < dialog_show < post_guard < rebuild < exporter:
        errors.append("BBS export ordering must be pre-guard -> Save dialog -> post-guard -> current-row rebuild -> XLSX export")

    locate_start = text.find("private void Locate()")
    export_method = text.find("private void OnExportClick", locate_start)
    locate_body = text[locate_start:export_method] if locate_start >= 0 and export_method > locate_start else ""
    locate_guard = locate_body.find('EnsureActive("định vị BBS")')
    resolve = locate_body.find("ResolveCurrentRow(row)")
    if min(locate_guard, resolve) < 0 or not locate_guard < resolve:
        errors.append("BBS Locate must keep its active-DWG guard before resolving the current row")

    build_start = text.find("private IReadOnlyList<RebarScheduleRow> BuildCurrentRows()")
    same_row = text.find("private static bool SameRow", build_start)
    build_body = text[build_start:same_row] if build_start >= 0 and same_row > build_start else ""
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "ProjectRebarScheduleBuilder.Build(snapshot)",
    ):
        if token not in build_body:
            errors.append("BBS current-row refresh missing detached read-only token: " + token)

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        ".Touch()",
        ".MarkDirty(",
    ):
        if forbidden in build_body:
            errors.append("BBS current-row refresh must remain detached/read-only: " + forbidden)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] BBS export validates active-DWG ownership before and after Save UI, then rebuilds detached current rows before XLSX export")
