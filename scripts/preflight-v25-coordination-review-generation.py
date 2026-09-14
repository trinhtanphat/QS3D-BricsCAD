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


# Transient review state is native-generation-bound: ObjectIds, highlight/isolation,
# implied selection and view snapshots must never cross a same-wrapper DB replacement.
for token, message in (
    ("private readonly IntPtr _nativeDatabaseIdentity;", "review session must capture native database identity"),
    ("_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);", "review session constructor must capture native database identity"),
    ("private bool IsOwnerGenerationCurrent", "review session needs an exact managed/native generation predicate"),
    ("document.Database.UnmanagedObject == _nativeDatabaseIdentity", "generation predicate must compare exact native database identity"),
    ("private void AbandonStaleGenerationState()", "stale generation state needs an abandon-without-native-write path"),
):
    require(token, message)

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
if "if (!IsOwnerGenerationCurrent)" not in reset or "AbandonStaleGenerationState();" not in reset:
    errors.append("cleanup retry must abandon generation-A ownership instead of restoring into generation B")

restore_selection = method("private void RestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)", "private void RestoreObjectIsolationModeBestEffort()")
if "IsOwnerGenerationCurrent" not in restore_selection:
    errors.append("implied selection compensation must not write into a successor native generation")

restore_mode = method("private bool TryRestoreObjectIsolationModeBestEffort(object? modeBefore)", "public void Dispose()")
if "IsOwnerGenerationCurrent" not in restore_mode:
    errors.append("OBJECTISOLATIONMODE restore must be generation-bound")

# Controller admission/status/cleanup decisions must treat same-wrapper native drift as
# stale ownership, not as an active owner merely because MDI reference equality holds.
require("private bool IsOwnerDocumentGenerationActive", "controller must expose generation-aware active-owner predicate")
for legacy in (
    "private bool IsOwnerDocumentActive =>\n                ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document);",
):
    if legacy in text:
        errors.append("controller must not use managed-wrapper-only owner activity")

if errors:
    print("ERROR: V25 Coordination review generation preflight failed:", file=sys.stderr)
    for error in errors:
        print(" - " + error, file=sys.stderr)
    raise SystemExit(1)

print("V25 Coordination review native-generation preflight passed.")
