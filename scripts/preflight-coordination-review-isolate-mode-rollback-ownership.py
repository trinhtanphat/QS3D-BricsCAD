#!/usr/bin/env python3
from pathlib import Path
import re

SOURCE = Path("src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void Isolate(IReadOnlyList<ObjectId> ids)")
end = text.find("public void RestoreIsolation()", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL isolate mode rollback ownership: Isolate method not found")
body = text[start:end]

required = (
    'var modeBefore = Bricscad.ApplicationServices.Application.GetSystemVariable("OBJECTISOLATIONMODE");',
    'Application.SetSystemVariable("OBJECTISOLATIONMODE", 0);',
    'SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);',
    "RestoreImpliedSelectionBestEffort(impliedSelectionBefore);",
    "if (!TryRestoreObjectIsolationModeBestEffort(modeBefore))",
    "_objectIsolationModeBefore = modeBefore;",
    "throw;",
    "_isolationActive = true;",
)
for token in required:
    if token not in body:
        raise SystemExit("FAIL isolate mode rollback ownership: missing " + token)

if not re.search(
    r"catch\s*\{\s*RestoreImpliedSelectionBestEffort\(impliedSelectionBefore\);\s*"
    r"if\s*\(\s*!TryRestoreObjectIsolationModeBestEffort\(modeBefore\)\s*\)\s*"
    r"_objectIsolationModeBefore\s*=\s*modeBefore;\s*throw;\s*\}",
    body,
    re.S,
):
    raise SystemExit("FAIL isolate mode rollback ownership: failed launch must retain prior mode iff compensation is unconfirmed, then bare rethrow")

queue_at = body.find('SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);')
catch_at = body.find("catch", queue_at)
compensate_at = body.find("TryRestoreObjectIsolationModeBestEffort(modeBefore)", catch_at)
failed_transfer_at = body.find("_objectIsolationModeBefore = modeBefore;", compensate_at)
throw_at = body.find("throw;", failed_transfer_at)
success_transfer_at = body.rfind("_objectIsolationModeBefore = modeBefore;")
success_active_at = body.find("_isolationActive = true;", success_transfer_at)
if not (0 <= queue_at < catch_at < compensate_at < failed_transfer_at < throw_at):
    raise SystemExit("FAIL isolate mode rollback ownership: failed compensation ordering is not retry-safe")
if not (queue_at < success_transfer_at < success_active_at):
    raise SystemExit("FAIL isolate mode rollback ownership: successful ownership publication must remain post-queue")

session_start = text.find("private sealed class TransientReviewSession : IDisposable")
session = text[session_start:]
if "public bool HasIsolation => _isolationActive || _objectIsolationModeBefore != null;" not in session:
    raise SystemExit("FAIL isolate mode rollback ownership: mode-only cleanup debt must remain observable")

restore_start = text.find("public void RestoreIsolation()", session_start)
restore_end = text.find("public void ApplySectionFocus", restore_start)
restore = text[restore_start:restore_end]
if "if (!_isolationActive)" not in restore or "RestoreObjectIsolationModeBestEffort();" not in restore:
    raise SystemExit("FAIL isolate mode rollback ownership: mode-only debt must be retryable without UNISOLATE")

helper_start = text.find("private void RestoreObjectIsolationModeBestEffort()")
helper_end = text.find("private bool TryRestoreObjectIsolationModeBestEffort", helper_start)
helper = text[helper_start:helper_end]
if "if (TryRestoreObjectIsolationModeBestEffort(value))" not in helper or "_objectIsolationModeBefore = null;" not in helper:
    raise SystemExit("FAIL isolate mode rollback ownership: retry debt may clear only after native mode restore succeeds")

print("PASS coordination review failed-isolate mode rollback retry ownership")
