from pathlib import Path

DIRECT_SOURCE = Path("src/QS3D.BricsCAD.V25/DirectDrawCommands.cs")
REPORTER_SOURCE = Path("src/QS3D.BricsCAD.V25/Services/DirectDrawUiFailureReporter.cs")
text = DIRECT_SOURCE.read_text(encoding="utf-8")
reporter = REPORTER_SOURCE.read_text(encoding="utf-8")

required_direct = [
    "DirectDrawUiFailureReporter.ReportOperationFailure(document, operation);",
    "DirectDrawUiFailureReporter.ReportPostCommitSuccess(document, status);",
    "DirectDrawUiFailureReporter.ReportPostCommitWarning(document);",
]
for token in required_direct:
    if token not in text:
        raise SystemExit(f"Direct Draw UI-truth/redaction contract missing: {token}")

required_reporter = [
    'var message = label + ": không thể hoàn tất thao tác. Vui lòng thử lại.";',
    'const string message = "Direct Draw đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.";',
    'try { document.Editor.WriteMessage("\\nQS3D " + message); }',
    "if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))",
    "PaletteCoordinator.SetStatus(message);",
]
for token in required_reporter:
    if token not in reporter:
        raise SystemExit(f"Direct Draw fenced reporter contract missing: {token}")

forbidden = [
    '" UI sync warning: " + ex.Message',
    'operation + " error: " + ex.Message',
    'operation + " lỗi: " + ex.Message',
]
for token in forbidden:
    if token in text or token in reporter:
        raise SystemExit(f"Direct Draw leaks raw exception detail: {token}")

finalize_start = text.find("private static void FinalizeUi(")
ensure_start = text.find("private static void EnsureActive(", finalize_start)
if finalize_start < 0 or ensure_start < 0:
    raise SystemExit("Direct Draw finalization helper structure changed")
finalize = text[finalize_start:ensure_start]
if "catch (Exception)" not in finalize:
    raise SystemExit("Direct Draw post-commit UI finalization must keep host failures exception-isolated")
if "DirectDrawUiFailureReporter.ReportPostCommitSuccess(document, status);" not in finalize:
    raise SystemExit("Direct Draw post-commit success must publish through the source-document-fenced reporter")
if "DirectDrawUiFailureReporter.ReportPostCommitWarning(document);" not in finalize:
    raise SystemExit("Direct Draw post-commit UI failure must preserve committed-state truth through the fenced reporter")
if "PaletteCoordinator.SetStatus(status);" in finalize:
    raise SystemExit("Direct Draw post-commit UI finalization must not publish unfenced process-wide palette status")

guard_start = text.find("private static void Guard(Document document, string operation, Action action)")
if guard_start < 0:
    raise SystemExit("Direct Draw operation guard structure changed")
guard = text[guard_start:]
if "catch (Exception)" not in guard or "DirectDrawUiFailureReporter.ReportOperationFailure(document, operation);" not in guard:
    raise SystemExit("Direct Draw operation failures must remain redacted and routed through the fenced reporter")
if "TrySetPaletteStatus(" in text:
    raise SystemExit("Direct Draw must not reintroduce the legacy unfenced palette helper")

write_start = reporter.find("private static void TryWriteEditor(")
palette_start = reporter.find("private static void TrySetPaletteForCurrentDocument(")
if write_start < 0 or palette_start < 0:
    raise SystemExit("Direct Draw reporter helper structure changed")
if "catch" not in reporter[write_start:palette_start]:
    raise SystemExit("Direct Draw editor reporting must remain exception-isolated")
if "catch" not in reporter[palette_start:]:
    raise SystemExit("Direct Draw palette status publication must remain exception-isolated")
if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in reporter[palette_start:]:
    raise SystemExit("Direct Draw palette status publication must remain fenced to the exact source document")

print("PASS: Direct Draw user-facing failures are redacted, post-commit UI truth is preserved, and process-wide palette publication is source-document fenced")
