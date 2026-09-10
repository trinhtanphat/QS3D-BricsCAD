#!/usr/bin/env python3
from pathlib import Path
import sys

# Reservation-v2 canonical carrier: issue-6321. This remains a RED-first source guard.
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawRepeatedCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

start = source.find("private sealed class RepeatedDocumentLifecycleGuard : IDisposable")
end = source.find("private sealed class RepeatedWholeCommandRollbackException", start if start >= 0 else 0)
body = source[start:end if end >= 0 else len(source)] if start >= 0 else ""
if start < 0:
    errors.append("missing RepeatedDocumentLifecycleGuard")
else:
    required = [
        "private bool _subscribed;",
        "private bool _disposeRequested;",
        "_documents.DocumentToBeDeactivated += OnDocumentToBeDeactivated;",
        "_subscribed = true;",
        "_disposeRequested = true;",
        "TryDetach();",
        "private void TryDetach()",
        "if (!_subscribed) return;",
        "_documents.DocumentToBeDeactivated -= OnDocumentToBeDeactivated;",
        "_subscribed = false;",
        "if (_disposeRequested)",
    ]
    for needle in required:
        if needle not in body:
            errors.append("repeated lifecycle detach contract missing: " + needle)

    dispose_start = body.find("public void Dispose()")
    detach_start = body.find("private void TryDetach()")
    callback_start = body.find("private void OnDocumentToBeDeactivated")
    dispose = body[dispose_start:detach_start if detach_start >= 0 else len(body)] if dispose_start >= 0 else ""
    detach = body[detach_start:callback_start if callback_start >= 0 else len(body)] if detach_start >= 0 else ""
    callback = body[callback_start:] if callback_start >= 0 else ""

    if "_disposed = true;" in dispose:
        errors.append("Dispose must not publish terminal disposed state before native detach succeeds")
    for needle in ["_disposeRequested = true;", "TryDetach();"]:
        if needle not in dispose:
            errors.append("Dispose must request and attempt retryable detach: " + needle)

    detach_remove = detach.find("_documents.DocumentToBeDeactivated -= OnDocumentToBeDeactivated;")
    detach_clear = detach.find("_subscribed = false;")
    detach_catch = detach.find("catch")
    if min(detach_remove, detach_clear, detach_catch) < 0 or not (detach_remove < detach_clear < detach_catch):
        errors.append("subscription ownership must clear only after successful native event removal")

    callback_retry = callback.find("if (_disposeRequested)")
    callback_flag = callback.find("_wasDeactivated = true;")
    if min(callback_retry, callback_flag) < 0 or callback_retry > callback_flag:
        errors.append("retained callback must retry detach before touching exact-document deactivation state")

    if "Application.DocumentManager.MdiActiveDocument" in body:
        errors.append("lifecycle guard must remain exact-start-document scoped; no active-document fallback")
    for forbidden in ["StartTransaction", "DocumentLock", "ExecuteDirect(", "RollbackWholeCommand(", "SendStringToExecute"]:
        if forbidden in body:
            errors.append("lifecycle guard must remain mutation-free: " + forbidden)

print("QS3D V25 repeated Direct Draw lifecycle detach preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: repeated Direct Draw lifecycle subscription detaches transactionally and remains retry-safe after native remove failure.")
