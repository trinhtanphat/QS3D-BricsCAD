#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ProjectFileUiService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

helper_start = source.find("private static void ShowError(string operation, Exception exception)")
helper_body = source[helper_start:] if helper_start >= 0 else ""
if helper_start < 0:
    errors.append("missing ProjectFileUiService.ShowError helper")
else:
    cancel_guard = helper_body.find("exception is OperationCanceledException")
    message_box = helper_body.find("System.Windows.MessageBox.Show(")
    stable_message = helper_body.find('"Không thể hoàn tất thao tác " + operation + ". Vui lòng thử lại."')
    if cancel_guard < 0:
        errors.append("ShowError must keep OperationCanceledException silent")
    if message_box < 0:
        errors.append("ShowError must retain user-facing error presentation")
    if stable_message < 0:
        errors.append("ShowError must present a stable operation-scoped redacted message")
    if cancel_guard >= 0 and message_box >= 0 and cancel_guard > message_box:
        errors.append("cancellation must be suppressed before error presentation")

    for forbidden in [
        "exception.Message",
        "exception.ToString",
        "exception.StackTrace",
        "exception.InnerException",
        "ex.Message",
        "ex.ToString",
        "ex.StackTrace",
        "ex.InnerException",
    ]:
        if forbidden in helper_body:
            errors.append("ShowError must not surface raw exception details: " + forbidden)

    for forbidden in [
        "ProjectContextCoordinator",
        "GetOrCreate",
        "DocumentLock",
        "StartTransaction",
        "SetImpliedSelection",
        "SendStringToExecute",
        "MdiActiveDocument",
        "Dispatcher.BeginInvoke",
    ]:
        if forbidden in helper_body:
            errors.append("ShowError must remain presentation-only/context-neutral: " + forbidden)

    # The UI presenter itself must be best-effort. A WPF presentation failure should not
    # escape from an error-reporting helper and mask the original operation result.
    try_pos = helper_body.find("try")
    catch_pos = helper_body.find("catch", try_pos if try_pos >= 0 else 0)
    if try_pos < 0 or catch_pos < 0 or message_box < 0 or not (try_pos < message_box < catch_pos):
        errors.append("ShowError MessageBox presentation must be contained by best-effort try/catch")

print("QS3D V25 ProjectFile UI error-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: ProjectFile UI errors are stable/redacted, cancellation-safe, and presentation-only.")
