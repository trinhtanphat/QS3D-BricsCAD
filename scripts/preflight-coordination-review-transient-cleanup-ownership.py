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


clear = method_body(
    "public void ClearHighlight()",
    "public void Isolate(IReadOnlyList<ObjectId> ids)",
)
if "var pending = _highlighted.ToArray();" not in clear:
    raise SystemExit("ClearHighlight must snapshot current highlight ownership")
commit = clear.find("transaction.Commit();")
release = clear.rfind("_highlighted.Clear();")
if commit < 0 or release < 0 or release < commit:
    raise SystemExit("ClearHighlight must release live highlight ownership only after native cleanup commit")
if "if (_destroyed)" not in clear:
    raise SystemExit("ClearHighlight must preserve explicit destroyed-document abandon semantics")

restore_isolation = method_body(
    "public void RestoreIsolation()",
    "public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)",
)
queue = restore_isolation.find('SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);')
release = restore_isolation.rfind("_isolationActive = false;")
if queue < 0 or release < 0 or release < queue:
    raise SystemExit("RestoreIsolation must release live isolation ownership only after native command queue success")
if "finally" in restore_isolation:
    raise SystemExit("RestoreIsolation must not erase retry ownership from an unconditional finally block")

reset = method_body(
    "private void ResetTransientStateBestEffort(bool throwOnSectionRestoreFailure)",
    "public void AbandonDestroyedDocumentState()",
)
if "catch { _isolationActive = false;" in reset:
    raise SystemExit("best-effort reset must not erase isolation retry ownership after live cleanup failure")
if "catch { _highlighted.Clear(); }" in reset:
    raise SystemExit("best-effort reset must not erase highlight retry ownership after live cleanup failure")
for token in (
    "Exception? cleanupFailure = null;",
    "cleanupFailure = cleanupFailure ?? ex;",
    "if (throwOnSectionRestoreFailure && cleanupFailure != null)",
    "throw cleanupFailure;",
):
    if token not in reset:
        raise SystemExit(f"dispose cleanup must aggregate retry-sensitive cleanup failure: {token}")

abandon = method_body(
    "public void AbandonDestroyedDocumentState()",
    "private void RestoreObjectIsolationModeBestEffort()",
)
for token in ("_destroyed = true;", "_highlighted.Clear();", "_isolationActive = false;", "_viewBeforeSection = null;"):
    if token not in abandon:
        raise SystemExit(f"destroyed-document abandon path missing: {token}")

session_start = text.find("private sealed class TransientReviewSession : IDisposable")
if session_start < 0:
    raise SystemExit("TransientReviewSession was not found")
session = text[session_start:]
if "private bool _disposeInProgress;" not in session:
    raise SystemExit("TransientReviewSession must own an explicit dispose re-entry guard")
dispose_start = session.find("public void Dispose()")
dispose_end = session.find("private sealed class ViewSnapshot", dispose_start)
if dispose_start < 0 or dispose_end < 0:
    raise SystemExit("TransientReviewSession.Dispose boundary was not found")
dispose = session[dispose_start:dispose_end]
for token in (
    "if (_disposed || _disposeInProgress) return;",
    "_disposeInProgress = true;",
    "ResetTransientStateBestEffort(true);",
    "_disposed = true;",
    "finally",
    "_disposeInProgress = false;",
):
    if token not in dispose:
        raise SystemExit(f"TransientReviewSession.Dispose missing retry-safe re-entry token: {token}")
cleanup = dispose.find("ResetTransientStateBestEffort(true);")
publish = dispose.find("_disposed = true;")
release_guard = dispose.rfind("_disposeInProgress = false;")
if not (0 <= cleanup < publish < release_guard):
    raise SystemExit("TransientReviewSession may publish terminal disposal only after cleanup, before releasing the re-entry guard")

print("PASS coordination review transient cleanup retry ownership")
