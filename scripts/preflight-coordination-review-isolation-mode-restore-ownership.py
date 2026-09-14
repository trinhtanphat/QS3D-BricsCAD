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

if "public bool HasIsolation => _isolationActive || _objectIsolationModeBefore != null || _impliedSelectionBeforeIsolation != null;" not in session:
    raise SystemExit("HasIsolation must retain command, mode and PICKFIRST cleanup ownership")
if "public bool IsOwnerNativeGenerationCurrent" not in session or "public bool IsOwnerGenerationActive" not in session:
    raise SystemExit("isolation ownership requires distinct native-generation and active-owner predicates")

restore = method_body(
    "public void RestoreIsolation()",
    "public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)",
)
for token in (
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "RequireOwnerGeneration(\"Isolation restore\")",
    "if (!_isolationActive)",
    "RestorePendingImpliedSelectionBestEffort();",
    "RestoreObjectIsolationModeBestEffort();",
    'SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);',
    "_isolationActive = false;",
):
    if token not in restore:
        raise SystemExit(f"RestoreIsolation missing generation/retry-owned cleanup token: {token}")
queue = restore.find('SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);')
release_command = restore.find("_isolationActive = false;", queue)
if queue < 0 or release_command < queue:
    raise SystemExit("RestoreIsolation must release command ownership only after queue success")

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

try_restore = method_body(
    "private bool TryRestoreObjectIsolationModeBestEffort(object? modeBefore)",
    "public void Dispose()",
)
for token in (
    "if (modeBefore == null) return true;",
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "if (!IsOwnerGenerationActive) return false;",
    'Application.SetSystemVariable("OBJECTISOLATIONMODE", modeBefore);',
    "return true;",
    "catch",
    "return false;",
):
    if token not in try_restore:
        raise SystemExit(f"native mode restore must report generation-safe success/failure: {token}")

isolate = method_body(
    "public void Isolate(IReadOnlyList<ObjectId> ids)",
    "public void RestoreIsolation()",
)
for token in (
    "if (!TryRestoreImpliedSelectionBestEffort(impliedSelectionBefore) && !_generationAbandoned)",
    "_impliedSelectionBeforeIsolation = impliedSelectionBefore;",
    "if (!TryRestoreObjectIsolationModeBestEffort(modeBefore) && !_generationAbandoned)",
    "_objectIsolationModeBefore = modeBefore;",
    "throw;",
):
    if token not in isolate:
        raise SystemExit(f"failed isolate launch rollback ownership missing: {token}")

abandon = method_body(
    "public void AbandonDestroyedDocumentState()",
    "private bool TryRestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)",
)
for token in ("_objectIsolationModeBefore = null;", "_impliedSelectionBeforeIsolation = null;"):
    if token not in abandon:
        raise SystemExit("destroyed-document path must abandon terminal isolation cleanup debt: " + token)
if "RestoreObjectIsolationModeBestEffort();" in abandon:
    raise SystemExit("destroyed-document path must not restore isolation mode through another active host context")

print("PASS coordination review isolation restore and failed-launch cleanup ownership is native-generation safe")
