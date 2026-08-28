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
release = clear.find("_highlighted.Clear();")
if commit < 0 or release < 0 or release < commit:
    raise SystemExit("ClearHighlight must release highlight ownership only after native cleanup commit")
if "if (_destroyed)" not in clear:
    raise SystemExit("ClearHighlight must preserve explicit destroyed-document abandon semantics")

restore_isolation = method_body(
    "public void RestoreIsolation()",
    "public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)",
)
queue = restore_isolation.find('SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);')
release = restore_isolation.find("_isolationActive = false;")
if queue < 0 or release < 0 or release < queue:
    raise SystemExit("RestoreIsolation must release isolation ownership only after native command queue success")
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
if "if (throwOnSectionRestoreFailure) throw;" not in reset:
    raise SystemExit("dispose path must remain retry-sensitive for section restore")

abandon = method_body(
    "public void AbandonDestroyedDocumentState()",
    "private void RestoreObjectIsolationModeBestEffort()",
)
for token in ("_destroyed = true;", "_highlighted.Clear();", "_isolationActive = false;", "_viewBeforeSection = null;"):
    if token not in abandon:
        raise SystemExit(f"destroyed-document abandon path missing: {token}")

print("PASS coordination review transient cleanup retry ownership")
