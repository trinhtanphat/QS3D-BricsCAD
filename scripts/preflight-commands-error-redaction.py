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

    forbidden = (
        "uiError.Message",
        "ex.Message",
        'UI sync warning: " + uiError.Message',
        'cảnh báo UI: " + uiError.Message',
    )
    for token in forbidden:
        if token in text:
            errors.append("raw runtime exception detail is user-visible: " + token)

    required = (
        'private sealed class CommandUserException : InvalidOperationException',
        'catch (CommandUserException expected)',
        'ReportCommandFailure(document, operation, expected.Message);',
        'catch (System.Exception)\n            {\n                ReportCommandFailure(document, operation, "không thể hoàn tất thao tác.");',
        'private static void ReportCommandFailure(Document document, string operation, string message)',
        'try { document.Editor.WriteMessage("\\n" + operation + " error: " + message); }',
        'try { PaletteCoordinator.SetStatus(operation + " lỗi: " + message); }',
        '[QS3D] Export đã hoàn tất; cảnh báo UI: không thể hoàn tất cập nhật giao diện.',
        'đã hoàn tất; cảnh báo UI: không thể hoàn tất cập nhật giao diện.',
        'QS3D link host đã commit; UI sync warning: không thể hoàn tất cập nhật giao diện.',
        'throw new CommandUserException("BQ cần một QS3D project hiện hữu;',
        'throw new CommandUserException("ED2 cần một QS3D project hiện hữu;',
        'throw new CommandUserException("BBS cần một QS3D project hiện hữu;',
        '[CommandMethod("QS3DED2", CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DBBS", CommandFlags.Modal)]',
        'XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);',
        'XlsxRebarScheduleExporter.Export(dialog.FileName, rows);',
        'Application.ShowModelessWindow(IntPtr.Zero, new QuantitySummaryWindow(doc, rows, locate, recalculate), true);',
    )
    for token in required:
        if token not in text:
            errors.append("missing command redaction/invariant token: " + token)

    export = text.find('XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);')
    export_finalize = text.find('FinalizeExportUi(', export + 1)
    if export < 0 or export_finalize < 0 or export >= export_finalize:
        errors.append("ED2 export must remain committed before best-effort export UI finalization")

    bbs = text.find('XlsxRebarScheduleExporter.Export(dialog.FileName, rows);')
    bbs_finalize = text.find('FinalizeExportUi(doc, status);', bbs + 1)
    if bbs < 0 or bbs_finalize < 0 or bbs >= bbs_finalize:
        errors.append("BBS export must remain committed before best-effort export UI finalization")

print("QS3D Commands error-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Commands keeps authored validation actionable while redacting unexpected runtime/UI exception details.")
