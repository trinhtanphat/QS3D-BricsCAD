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

    guard = 'EnsureCurrentProject("xuất BQ XLSX");'
    dialog = "new SaveFileDialog"
    show = "dialog.ShowDialog(this)"
    refresh = "RefreshRowsForCurrentMode(false);"
    export = "XlsxQuantityExporter.Export(dialog.FileName, visibleRows);"

    if method.count(guard) < 2:
        raise SystemExit("FAIL: BQ XLSX export must validate the bound DWG both before dialog interaction and again before live export")

    first_guard = method.find(guard)
    dialog_pos = method.find(dialog)
    show_pos = method.find(show)
    second_guard = method.find(guard, first_guard + len(guard))
    refresh_pos = method.find(refresh)
    export_pos = method.find(export)

    if min(first_guard, dialog_pos, show_pos, second_guard, refresh_pos, export_pos) < 0:
        raise SystemExit("FAIL: BQ XLSX export ordering contract is incomplete")

    if not (first_guard < dialog_pos < show_pos < second_guard < refresh_pos < export_pos):
        raise SystemExit("FAIL: BQ XLSX export must guard before SaveFileDialog and re-guard before live recalc/export")

    print("PASS: BQ XLSX export validates the exact bound DWG/project before SaveFileDialog interaction and revalidates before live recalculation/export.")


if __name__ == "__main__":
    main()
