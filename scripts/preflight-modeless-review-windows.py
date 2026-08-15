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
    for needle in ('EnsureCurrentProject("tính lại BQ")', 'EnsureCurrentProject("định vị BQ")', 'EnsureCurrentProject("xuất BQ XLSX")'):
        if needle not in text:
            errors.append("BQ modeless callback missing current-project/source-DWG guard: " + needle)
    helper = text.find("private void EnsureCurrentProject(string operation)")
    helper_active = text.find("EnsureActive(operation);", helper)
    helper_project = text.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", helper)
    helper_identity = text.find("EnsureProjectIdentity(project, operation);", helper)
    if min(helper, helper_active, helper_project, helper_identity) < 0 or not (helper < helper_active < helper_project < helper_identity):
        errors.append("BQ EnsureCurrentProject must verify the bound DWG, inspect the existing project, then verify project identity")
    export = text.find("private void OnExportClick")
    export_guard = text.find('EnsureCurrentProject("xuất BQ XLSX")', export)
    refresh = text.find("RefreshRowsForCurrentMode(false);", export)
    exporter = text.find("XlsxQuantityExporter.Export", export)
    refresh_helper = text.find("private void RefreshRowsForCurrentMode(bool requireLiveSummarySource)")
    refresh_assign = text.find("_rows = RecalculateRowsForCurrentMode(requireLiveSummarySource);", refresh_helper)
    if export < 0 or export_guard < 0 or refresh < 0 or exporter < 0 or not (export_guard < refresh < exporter):
        errors.append("BQ XLSX export must bind to the source DWG/current project and recalculate before writing cached rows")
    if refresh_helper < 0 or refresh_assign < 0:
        errors.append("BQ refresh helper must rebuild rows for the active Summary/Detail mode")

recognition = windows["Recognition"]
if recognition.is_file():
    text = recognition.read_text(encoding="utf-8")
    for needle in (
        "EnsureActiveDocument();",
        "Func<IReadOnlyList<RecognitionResult>, bool, int>? _apply",
        "Apply(IEnumerable<RecognitionResult> rows, bool requireLiveConfidence)",
        "batch = rows.Where(x => x != null && x.TopCandidate != null).ToList().AsReadOnly();",
        "var applied = _apply(batch, requireLiveConfidence);",
        "RefreshStatus(applied, 0, null);",
        'RefreshStatus(0, batch.Count, "Apply batch: " + ex.Message);',
    ):
        if needle not in text:
            errors.append("Recognition atomic review UI missing bound-DWG/batch error token: " + needle)
    if "catch {" in text:
        errors.append("Recognition review must not silently swallow apply/locate failures")

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
if revision.is_file():
    text = revision.read_text(encoding="utf-8")
    ensure = text.find("EnsureActiveAndCurrent();")
    callback = text.find("_locate?.Invoke(row);")
    if min(ensure, callback) < 0 or ensure > callback:
        errors.append("Revision Locate must verify its source DWG before invoking the CAD callback")

health = windows["Model Health"]
if health.is_file() and "EnsureActiveAndCurrent();\n                _locate(issue);" not in health.read_text(encoding="utf-8"):
    errors.append("Model Health Locate must verify its source DWG and current project snapshot before invoking the CAD callback")

print("QS3D modeless review-window preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Recognition/BQ/BBS/Revision/Health modeless actions stay bound to their source DWG; Recognition surfaces atomic batch failures, BQ refreshes before export, and BBS totals remain checked.")
