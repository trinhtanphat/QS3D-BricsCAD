from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/DirectDrawCommands.cs")
text = SOURCE.read_text(encoding="utf-8")

required = [
    'private const string OperationFailureSuffix = " lỗi: không thể hoàn tất thao tác. Vui lòng thử lại.";',
    'private const string PostCommitUiWarning = "Direct Draw đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.";',
    'document.Editor.WriteMessage("\\nQS3D " + PostCommitUiWarning);',
    'TrySetPaletteStatus(operation + OperationFailureSuffix);',
]
for token in required:
    if token not in text:
        raise SystemExit(f"Direct Draw UI-truth/redaction contract missing: {token}")

forbidden = [
    '" UI sync warning: " + ex.Message',
    'operation + " error: " + ex.Message',
    'operation + " lỗi: " + ex.Message',
]
for token in forbidden:
    if token in text:
        raise SystemExit(f"Direct Draw leaks raw exception detail: {token}")

finalize_start = text.find("private static void FinalizeUi(")
ensure_start = text.find("private static void EnsureActive(", finalize_start)
if finalize_start < 0 or ensure_start < 0:
    raise SystemExit("Direct Draw finalization helper structure changed")
finalize = text[finalize_start:ensure_start]
if "catch (Exception)" not in finalize:
    raise SystemExit("Direct Draw post-commit UI finalization must keep host failures exception-isolated")
if 'document.Editor.WriteMessage("\\nQS3D " + PostCommitUiWarning);' not in finalize:
    raise SystemExit("Direct Draw post-commit UI failure must preserve committed-state truth")

helper_start = text.find("private static void TrySetPaletteStatus(", ensure_start)
if helper_start < 0 or "catch" not in text[helper_start:]:
    raise SystemExit("Direct Draw palette status publication must be exception-isolated")

print("PASS: Direct Draw user-facing failures are redacted and post-commit UI truth is preserved")
