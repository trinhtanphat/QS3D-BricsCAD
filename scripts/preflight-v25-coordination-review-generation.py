#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str, message: str) -> None:
    if token not in text:
        errors.append(message)


def method(signature: str, next_signature: str) -> str:
    start = text.find(signature)
    end = text.find(next_signature, start + 1) if start >= 0 else -1
    return text[start:end] if start >= 0 and end > start else ""


for token, message in (
    ("private readonly IntPtr _nativeDatabaseIdentity;", "review session must capture native database identity"),
    ("_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);", "review session constructor must capture native database identity"),
    ("public bool IsOwnerNativeGenerationCurrent", "review session needs a native-generation predicate independent of MDI activity"),
    ("public bool IsOwnerGenerationActive", "review session needs a separate active-owner predicate"),
    ("document.Database.UnmanagedObject == _nativeDatabaseIdentity", "generation predicate must compare exact native database identity"),
    ("private void AbandonStaleGenerationState()", "stale generation state needs an abandon-without-native-write path"),
):
    require(token, message)

native_predicate = method("public bool IsOwnerNativeGenerationCurrent", "public bool IsOwnerGenerationActive")
if "MdiActiveDocument" in native_predicate:
    errors.append("native generation freshness must not depend on MDI activity")
active_predicate = method("public bool IsOwnerGenerationActive", "private static IntPtr GetNativeDatabaseIdentity")
for token in ("IsOwnerNativeGenerationCurrent", "MdiActiveDocument"):
    if token not in active_predicate:
        errors.append("active-owner predicate must combine native generation and MDI activity: " + token)

for signature, next_signature, action in (
    ("public void Highlight(IReadOnlyList<ObjectId> ids)", "private IReadOnlyList<ObjectId> UnhighlightAttemptBestEffort", "highlight"),
    ("public void ClearHighlight()", "public void Isolate", "highlight cleanup"),
    ("public void Isolate(IReadOnlyList<ObjectId> ids)", "public void RestoreIsolation", "isolation"),
    ("public void RestoreIsolation()", "public void ApplySectionFocus", "isolation restore"),
    ("public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)", "public void RestoreSectionView", "section/focus"),
    ("public void RestoreSectionView()", "private bool TryRestoreSectionViewBestEffort", "view restore"),
):
    body = method(signature, next_signature)
    if "RequireOwnerGeneration(" not in body and "EnsureOwnerGenerationOrAbandon(" not in body:
        errors.append(f"{action} must generation-fence before native state access")

reset = method("private Exception? ResetTransientStateBestEffort(bool throwOnSectionRestoreFailure)", "public void AbandonDestroyedDocumentState()")
if "if (!IsOwnerNativeGenerationCurrent)" not in reset or "AbandonStaleGenerationState();" not in reset:
    errors.append("cleanup retry must abandon generation-A ownership only for actual native-generation drift")

restore_selection = method("private void RestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)", "private void RestoreObjectIsolationModeBestEffort()")
if "IsOwnerGenerationActive" not in restore_selection:
    errors.append("implied selection compensation requires both current generation and active owner")
restore_mode = method("private bool TryRestoreObjectIsolationModeBestEffort(object? modeBefore)", "public void Dispose()")
if "IsOwnerGenerationActive" not in restore_mode:
    errors.append("OBJECTISOLATIONMODE restore requires both current generation and active owner")

require("private bool IsOwnerDocumentGenerationActive", "controller must expose generation-aware active-owner predicate")
abandon = method("private void AbandonStaleGenerationIfNeeded()", "public void Attach()")
if "IsOwnerNativeGenerationCurrent" not in abandon or "IsOwnerGenerationActive" in abandon:
    errors.append("controller must abandon ownership only on native-generation drift, never merely because another MDI document is active")

activated = method("private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)", "private void OnDocumentToBeDestroyed")
for token in ("if (!_session.IsOwnerNativeGenerationCurrent)", "AbandonStaleGenerationIfNeeded();", "_cleanupBarrier = _session.HasTransientState;"):
    if token not in activated:
        errors.append("DocumentActivated must distinguish successor generation from ordinary foreign MDI activation: " + token)

closing = method("private void OnWindowClosing(object sender, CancelEventArgs e)", "private void OnWindowClosed")
if "if (!_session.IsOwnerNativeGenerationCurrent)" not in closing or "e.Cancel = true;" not in closing:
    errors.append("window close must abandon only stale native generation but retain same-generation cleanup debt while owner MDI is inactive")

if errors:
    print("ERROR: V25 Coordination review generation preflight failed:", file=sys.stderr)
    for error in errors:
        print(" - " + error, file=sys.stderr)
    raise SystemExit(1)

print("V25 Coordination review native-generation preflight passed.")
