#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_REL = "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"


def main():
    text = (ROOT / WINDOW_REL).read_text(encoding="utf-8")
    start = text.find("private void OnExportClick(object sender, RoutedEventArgs e)")
    end = text.find("private static void RestoreOrThrow", start)
    if start < 0 or end < 0:
        raise SystemExit("FAIL: could not isolate QuantitySummaryWindow.OnExportClick")
    method = text[start:end]

    active_guard = 'EnsureActive("xuất BQ XLSX");'
    project_guard = 'EnsureCurrentProject("xuất BQ XLSX");'
    dialog = "new SaveFileDialog"
    show = "dialog.ShowDialog(this)"
    refresh = "RefreshRowsForCurrentMode(false);"
    export = "XlsxQuantityExporter.Export(dialog.FileName, visibleRows);"

    if method.count(active_guard) != 1:
        raise SystemExit("FAIL: BQ XLSX export must validate the active bound DWG exactly once before dialog interaction")
    if method.count(project_guard) != 1:
        raise SystemExit("FAIL: BQ XLSX export must validate the current bound project exactly once after Save confirmation")

    active_guard_pos = method.find(active_guard)
    dialog_pos = method.find(dialog)
    show_pos = method.find(show)
    project_guard_pos = method.find(project_guard)
    refresh_pos = method.find(refresh)
    export_pos = method.find(export)

    if min(active_guard_pos, dialog_pos, show_pos, project_guard_pos, refresh_pos, export_pos) < 0:
        raise SystemExit("FAIL: BQ XLSX export ordering contract is incomplete")

    if not (active_guard_pos < dialog_pos < show_pos < project_guard_pos < refresh_pos < export_pos):
        raise SystemExit("FAIL: BQ XLSX export must guard active DWG before SaveFileDialog and revalidate the project before live recalc/export")

    print("PASS: BQ XLSX export validates active-DWG affinity before SaveFileDialog without reading project state, then revalidates the project before live recalculation/export.")


if __name__ == "__main__":
    main()
