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
for token in ("RequireOwnerGeneration", "transaction.Commit();", "released.Add(id);", "_highlighted.Remove(id);"):
    if token not in clear:
        raise SystemExit("ClearHighlight generation-safe cleanup missing: " + token)
commit = clear.find("transaction.Commit();")
release = clear.find("_highlighted.Remove(id);")
if commit < 0 or release < commit:
    raise SystemExit("ClearHighlight must release confirmed ownership only after native cleanup commit")
if "if (cleanupFailure != null)" not in clear or "throw new InvalidOperationException" not in clear:
    raise SystemExit("ClearHighlight must surface incomplete live cleanup so failed entity ownership remains retryable")

restore_isolation = method_body(
    "public void RestoreIsolation()",
    "public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)",
)
for token in (
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "RestorePendingImpliedSelectionBestEffort();",
    "RestoreObjectIsolationModeBestEffort();",
    'SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);',
    "_isolationActive = false;",
):
    if token not in restore_isolation:
        raise SystemExit("RestoreIsolation generation-safe retry ownership missing: " + token)
queue = restore_isolation.find('SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);')
release = restore_isolation.find("_isolationActive = false;", queue)
if queue < 0 or release < queue:
    raise SystemExit("RestoreIsolation must release live command ownership only after native queue success")
if "finally" in restore_isolation:
    raise SystemExit("RestoreIsolation must not erase retry ownership from an unconditional finally block")

try_reset = method_body(
    "public Exception? TryResetTransientStateBestEffort()",
    "private Exception? ResetTransientStateBestEffort(bool throwOnSectionRestoreFailure)",
)
if "ResetTransientStateBestEffort(false)" not in try_reset:
    raise SystemExit("TryResetTransientStateBestEffort must surface aggregate cleanup result")

reset = method_body(
    "private Exception? ResetTransientStateBestEffort(bool throwOnSectionRestoreFailure)",
    "public void AbandonDestroyedDocumentState()",
)
for token in (
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "Exception? cleanupFailure = null;",
    "cleanupFailure = cleanupFailure ?? ex;",
    "if (HasTransientState && cleanupFailure == null)",
    "if (throwOnSectionRestoreFailure && cleanupFailure != null)",
    "throw cleanupFailure;",
    "return cleanupFailure;",
):
    if token not in reset:
        raise SystemExit(f"reset must preserve generation-aware aggregate retry ownership: {token}")
if "catch { _isolationActive = false;" in reset or "catch { _highlighted.Clear(); }" in reset:
    raise SystemExit("best-effort reset must not erase live retry ownership after cleanup failure")

abandon = method_body(
    "public void AbandonDestroyedDocumentState()",
    "private bool TryRestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)",
)
for token in (
    "_destroyed = true;",
    "_highlighted.Clear();",
    "_isolationActive = false;",
    "_viewBeforeSection = null;",
    "_objectIsolationModeBefore = null;",
    "_impliedSelectionBeforeIsolation = null;",
):
    if token not in abandon:
        raise SystemExit(f"destroyed-document terminal abandon path missing: {token}")

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
        raise SystemExit(f"TransientReviewSession.Dispose missing retry-safe token: {token}")
cleanup = dispose.find("ResetTransientStateBestEffort(true);")
publish = dispose.find("_disposed = true;")
release_guard = dispose.rfind("_disposeInProgress = false;")
if not (0 <= cleanup < publish < release_guard):
    raise SystemExit("session may publish terminal disposal only after cleanup succeeds, before releasing re-entry guard")
if "if (HasTransientState && cleanupFailure == null)" not in reset:
    raise SystemExit("Dispose relies on reset core to synthesize failure while transient debt remains")

print("PASS coordination review transient cleanup retry ownership is native-generation safe")
