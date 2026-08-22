#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

windows = {
    "Recognition": ROOT / "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml.cs",
    "BQ": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs",
    "BBS": ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs",
    "Revision": ROOT / "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml.cs",
    "Model Health": ROOT / "src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml.cs",
}

for label, path in windows.items():
    if not path.is_file():
        errors.append("missing modeless review window: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    for needle in ("Document", "BcadApplication.DocumentManager.MdiActiveDocument", "ReferenceEquals"):
        if needle not in text:
            errors.append(label + " window missing bound-DWG guard token: " + needle)

bq = windows["BQ"]
if bq.is_file():
    text = bq.read_text(encoding="utf-8")
    persist = text.find("private void PersistColumnPreferences()")
    persist_guard = text.find('EnsureActive("lưu cấu hình cột BQ")', persist)
    mutation = text.find("VisibleBqColumnsKey", persist)
    if persist < 0 or persist_guard < 0 or mutation < 0 or persist_guard > mutation:
        errors.append("BQ column preference mutation must require the source DWG before project metadata changes")
    for needle in ('EnsureActive("tính lại BQ")', 'EnsureActive("định vị BQ")', 'EnsureActive("xuất BQ XLSX")'):
        if needle not in text:
            errors.append("BQ modeless callback missing active-DWG guard: " + needle)
    export = text.find("private void OnExportClick")
    export_guard = text.find('EnsureActive("xuất BQ XLSX")', export)
    recalc = text.find("_rows = _recalculate()", export)
    exporter = text.find("XlsxQuantityExporter.Export", export)
    if export < 0 or export_guard < 0 or recalc < 0 or exporter < 0 or not (export_guard < recalc < exporter):
        errors.append("BQ XLSX export must bind to the source DWG and recalculate before writing cached rows")

recognition = windows["Recognition"]
if recognition.is_file():
    text = recognition.read_text(encoding="utf-8")
    if "EnsureActiveDocument();" not in text:
        errors.append("Recognition apply/locate must verify its source DWG")
    if "catch (Exception ex)" not in text or "firstError = ex.Message" not in text:
        errors.append("Recognition review must surface manual apply failures instead of swallowing them")

bbs = windows["BBS"]
if bbs.is_file():
    text = bbs.read_text(encoding="utf-8")
    for needle in ('EnsureActive("định vị BBS")', 'EnsureActive("xuất BBS XLSX")'):
        if needle not in text:
            errors.append("BBS modeless action missing active-DWG guard: " + needle)
    if "QuantityReportMath.AddCount" not in text or "QuantityReportMath.Add" not in text:
        errors.append("BBS modeless totals must use checked finite aggregation")
    if ".Sum(" in text:
        errors.append("BBS modeless totals must not use unchecked LINQ Sum")

revision = windows["Revision"]
if revision.is_file() and "EnsureActive();\n                _locate(row);" not in revision.read_text(encoding="utf-8"):
    errors.append("Revision Locate must verify its source DWG before invoking the CAD callback")

health = windows["Model Health"]
if health.is_file() and "EnsureActive();\n                _locate(issue);" not in health.read_text(encoding="utf-8"):
    errors.append("Model Health Locate must verify its source DWG before invoking the CAD callback")

print("QS3D modeless review-window preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Recognition/BQ/BBS/Revision/Health modeless CAD/project/export actions are bound to their source DWG; BQ refreshes before XLSX and BBS totals are checked.")
