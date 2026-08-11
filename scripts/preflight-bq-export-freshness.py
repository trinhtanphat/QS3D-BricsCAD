#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private void OnExportClick")
    end = text.find("private static void RestoreOrThrow", start + 1)
    if start < 0 or end < 0:
        errors.append("QuantitySummaryWindow missing export method boundary")
    else:
        body = text[start:end]
        dialog = body.find("var dialog = new SaveFileDialog")
        confirmed = body.find("if (dialog.ShowDialog(this) != true) return;", dialog + 1)
        recalc = body.find("_rows = _recalculate()", confirmed + 1)
        visible = body.find("var visibleRows", confirmed + 1)
        export = body.find("XlsxQuantityExporter.Export(dialog.FileName, visibleRows)", visible + 1)

        if min(dialog, confirmed, recalc, visible, export) < 0:
            errors.append("QuantitySummaryWindow export missing save/recalculate/filter/export contract token")
        elif not dialog < confirmed < recalc < visible < export:
            errors.append("BQ XLSX must confirm Save before recalculation/regeneration, fresh filtering, and export")

        if "_recalculate()" in body[:confirmed if confirmed >= 0 else 0]:
            errors.append("BQ XLSX Cancel path must not recalculate/regenerate project state")

print("QS3D BQ export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BQ XLSX confirms the destination before recalculation/regeneration and exports freshly filtered rows.")
