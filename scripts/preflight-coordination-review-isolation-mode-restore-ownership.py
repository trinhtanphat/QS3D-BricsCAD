from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs")
text = SOURCE.read_text(encoding="utf-8")


def method_body(signature: str, next_signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise SystemExit(f"missing method: {signature}")
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise SystemExit(f"missing following method boundary: {next_signature}")
    return text[start:end]


session_start = text.find("private sealed class TransientReviewSession : IDisposable")
if session_start < 0:
    raise SystemExit("TransientReviewSession was not found")
session = text[session_start:]

# A successful UNISOLATE queue and a failed OBJECTISOLATIONMODE restore are
# independent cleanup outcomes. UI/session ownership must remain visible while
# either native obligation is outstanding.
if "public bool HasIsolation => _isolationActive || _objectIsolationModeBefore != null;" not in session:
    raise SystemExit("HasIsolation must retain UI/session ownership while isolation-mode restore is still owed")

restore = method_body(
    "public void RestoreIsolation()",
    "public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)",
)
for token in (
    "if (!_isolationActive)",
    "RestoreObjectIsolationModeBestEffort();",
    'SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);',
    "_isolationActive = false;",
):
    if token not in restore:
        raise SystemExit(f"RestoreIsolation missing independently retry-owned cleanup token: {token}")
retry_without_command = restore.find("if (!_isolationActive)")
mode_retry = restore.find("RestoreObjectIsolationModeBestEffort();", retry_without_command)
queue = restore.find('SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);')
release_command = restore.find("_isolationActive = false;", queue)
if not (0 <= retry_without_command < mode_retry < queue < release_command):
    raise SystemExit("RestoreIsolation must retry pending mode compensation without re-queueing UNISOLATE and release command ownership only after queue success")

mode_restore = method_body(
    "private void RestoreObjectIsolationModeBestEffort()",
    "private bool TryRestoreObjectIsolationModeBestEffort(object? modeBefore)",
)
for token in (
    "if (_objectIsolationModeBefore == null) return;",
    "var value = _objectIsolationModeBefore;",
    "if (TryRestoreObjectIsolationModeBestEffort(value))",
    "_objectIsolationModeBefore = null;",
):
    if token not in mode_restore:
        raise SystemExit(f"mode restore ownership helper missing: {token}")
attempt = mode_restore.find("TryRestoreObjectIsolationModeBestEffort(value)")
release = mode_restore.find("_objectIsolationModeBefore = null;")
if attempt < 0 or release < attempt:
    raise SystemExit("OBJECTISOLATIONMODE retry ownership may clear only after a successful native restore attempt")
if mode_restore.find("_objectIsolationModeBefore = null;") < mode_restore.find("TryRestoreObjectIsolationModeBestEffort(value)"):
    raise SystemExit("OBJECTISOLATIONMODE retry ownership must not clear before native restore success")

try_restore = method_body(
    "private bool TryRestoreObjectIsolationModeBestEffort(object? modeBefore)",
    "public void Dispose()",
)
for token in (
    "if (modeBefore == null) return true;",
    'Application.SetSystemVariable("OBJECTISOLATIONMODE", modeBefore);',
    "return true;",
    "catch",
    "return false;",
):
    if token not in try_restore:
        raise SystemExit(f"native mode restore attempt must report success/failure without throwing: {token}")

isolate = method_body(
    "public void Isolate(IReadOnlyList<ObjectId> ids)",
    "public void RestoreIsolation()",
)
if "TryRestoreObjectIsolationModeBestEffort(modeBefore);" not in isolate:
    raise SystemExit("failed isolate launch must use attempt-local mode compensation without publishing persistent ownership")

abandon = method_body(
    "public void AbandonDestroyedDocumentState()",
    "private void RestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)",
)
restore_attempt = abandon.find("RestoreObjectIsolationModeBestEffort();")
explicit_abandon = abandon.find("_objectIsolationModeBefore = null;", restore_attempt)
if restore_attempt < 0 or explicit_abandon < restore_attempt:
    raise SystemExit("destroyed-document path must attempt mode restore before explicitly abandoning remaining mode ownership")

print("PASS coordination review isolation mode restore retry ownership")
