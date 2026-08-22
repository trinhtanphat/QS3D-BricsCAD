#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    cases = (
        (
            "ED2",
            '[CommandMethod("QS3DED2"',
            '[CommandMethod("QS3DBBS"',
            "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);",
            "FinalizeExportUi(\n                    doc,",
        ),
        (
            "BBS XLSX",
            '[CommandMethod("QS3DBBS"',
            '[CommandMethod("QS3DREGEN"',
            "XlsxRebarScheduleExporter.Export(dialog.FileName, rows);",
            "FinalizeExportUi(doc, status);",
        ),
    )

    for label, start_token, end_token, export_token, finalize_token in cases:
        start = text.find(start_token)
        end = text.find(end_token, start + 1)
        if start < 0 or end < 0:
            errors.append(label + " missing command boundary")
            continue
        body = text[start:end]
        export = body.find(export_token)
        finalize = body.find(finalize_token, export + 1)
        if export < 0 or finalize < 0 or export >= finalize:
            errors.append(label + " must commit the XLSX before best-effort FinalizeExportUi")
            continue
        between = body[export + len(export_token):finalize]
        if "PaletteCoordinator." in between or "Editor.WriteMessage" in between:
            errors.append(label + " performs fallible UI work after persistent export but before FinalizeExportUi")

    for token in (
        "private static void FinalizeExportUi(Document document, string status, string extra = \"\")",
        "catch (System.Exception)",
        "Export đã hoàn tất; cảnh báo UI: không thể hoàn tất cập nhật giao diện.",
    ):
        if token not in text:
            errors.append("Commands export finalizer missing token: " + token)

print("QS3D command export UI isolation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2/BBS persistent export success is separated from best-effort Palette/Editor reporting.")
