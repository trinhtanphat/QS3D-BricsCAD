from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/BasicDrawingCommands.cs")
text = SOURCE.read_text(encoding="utf-8")

required = [
    'private const string OperationFailureSuffix = ": không thể hoàn tất thao tác. Vui lòng thử lại.";',
    'private const string UiSyncWarning = "UI sync warning: CAD đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.";',
    'Report(document, operation + OperationFailureSuffix);',
    'Report(document, status + " " + UiSyncWarning);',
]
for token in required:
    if token not in text:
        raise SystemExit(f"Basic Drawing exception-redaction contract missing: {token}")

forbidden = [
    'ex.Message',
    'uiError.Message',
    'catch (Exception ex)',
    'catch (Exception uiError)',
]
for token in forbidden:
    if token in text:
        raise SystemExit(f"Basic Drawing leaks host exception detail: {token}")

append_at = text.find("var id = AppendEntity(")
finalize_at = text.find("FinalizeSuccess(document, id, context")
if append_at < 0 or finalize_at < 0 or finalize_at < append_at:
    raise SystemExit("Basic Drawing post-commit UI finalization ordering contract changed")

finalize_start = text.find("private static void FinalizeSuccess(")
report_start = text.find("private static void Report(", finalize_start)
if finalize_start < 0 or report_start < 0:
    raise SystemExit("Basic Drawing finalization/report helpers not found")
finalize = text[finalize_start:report_start]
if "try" not in finalize or "catch (Exception)" not in finalize:
    raise SystemExit("Basic Drawing post-commit UI finalization must remain exception-isolated")
if "Report(document, status + \" \" + UiSyncWarning);" not in finalize:
    raise SystemExit("Basic Drawing UI-sync failure must report a stable redacted warning")

print("PASS: Basic Drawing command and post-commit UI failures are redacted and UI sync remains best-effort")
