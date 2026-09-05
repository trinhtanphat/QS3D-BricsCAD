from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs")
text = SOURCE.read_text(encoding="utf-8")

required = [
    'private const string OpeningReadFailure = "bỏ qua — không thể đọc/đánh giá source CAD của opening.";',
    'private const string OperationFailure = "QS3DAUTOLINKHOSTS lỗi: không thể hoàn tất thao tác. Vui lòng thử lại.";',
    'private const string PostCommitUiWarning = "[QS3D] Auto Host đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.";',
    'document.Editor.WriteMessage("\\n  " + opening.Id + ": " + OpeningReadFailure);',
    'document.Editor.WriteMessage("\\n" + PostCommitUiWarning);',
    'var message = OperationFailure;',
]
for token in required:
    if token not in text:
        raise SystemExit(f"Auto Host UI-truth/redaction contract missing: {token}")

forbidden = [
    'ex.Message',
    'warning.Message',
    'error.Message',
    '" + error.Message',
    '" + warning.Message',
]
for token in forbidden:
    if token in text:
        raise SystemExit(f"Auto Host leaks raw exception detail: {token}")

finalize_start = text.find("private static void FinalizeAutoHostUi(")
report_start = text.find("private static void ReportAutoHostError(", finalize_start)
link_single_start = text.find("internal static string LinkSingleOpening(", report_start)
if finalize_start < 0 or report_start < 0 or link_single_start < 0:
    raise SystemExit("Auto Host finalization/report helper structure changed")
finalize = text[finalize_start:report_start]
report = text[report_start:link_single_start]
if "catch (System.Exception)" not in finalize:
    raise SystemExit("Auto Host post-commit UI finalization must keep host failures exception-isolated")
if 'document.Editor.WriteMessage("\\n" + PostCommitUiWarning);' not in finalize:
    raise SystemExit("Auto Host post-commit UI failure must preserve committed-state truth")
if "System.Exception error" in report:
    raise SystemExit("Auto Host user-facing error reporter must not accept a raw host exception")

print("PASS: Auto Host user-facing failures are redacted and post-commit UI truth is preserved")
