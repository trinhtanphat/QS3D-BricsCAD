#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing coordination review source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

required = [
    "DocumentToBeDeactivated = 1 <<",
    "Application.DocumentManager.DocumentToBeDeactivated += OnDocumentToBeDeactivated;",
    "Application.DocumentManager.DocumentToBeDeactivated -= OnDocumentToBeDeactivated",
    "private void OnDocumentToBeDeactivated(object sender, DocumentCollectionEventArgs e)",
    "private bool IsOwnerDocumentActive",
    "var ownerActive = IsOwnerDocumentActive;",
    "_highlight.IsEnabled = ownerActive && mutationsAllowed;",
    "_clearHighlight.IsEnabled = ownerActive && _session.HasHighlight;",
    "_restoreIsolation.IsEnabled = ownerActive && _session.HasIsolation;",
    "_restoreView.IsEnabled = ownerActive && _session.HasSectionView;",
]
for needle in required:
    if needle not in text:
        errors.append("CoordinationManagerReviewUi missing affinity token: " + needle)


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

pre_deactivate = method_body(
    "private void OnDocumentToBeDeactivated",
    "private void OnDocumentActivated")
if pre_deactivate:
    for needle in [
        "ReferenceEquals(e.Document, _document)",
        "TryResetTransientStateBestEffort()",
        "_cleanupBarrier",
        "UpdateActionState();",
    ]:
        if needle not in pre_deactivate:
            errors.append("pre-deactivation cleanup missing owner-safe token: " + needle)

activated = method_body(
    "private void OnDocumentActivated",
    "private void OnDocumentToBeDestroyed")
if activated:
    # Owner reactivation may retry retained cleanup debt. Only the foreign-active branch
    # is forbidden from touching owner transient CAD state.
    foreign_marker = activated.find("The new active document is foreign to this controller")
    if foreign_marker < 0:
        errors.append("DocumentActivated must mark the foreign-document branch explicitly")
    elif "ResetTransientStateBestEffort" in activated[foreign_marker:]:
        errors.append("foreign DocumentActivated handling must not clean owner-document transient CAD state")
    for needle in [
        "IsOwnerDocumentActive",
        "_session.HasTransientState",
        "UpdateActionState();",
    ]:
        if needle not in activated:
            errors.append("DocumentActivated handling missing fail-closed affinity token: " + needle)

abandon = method_body(
    "public void AbandonDestroyedDocumentState",
    "private void RestoreImpliedSelectionBestEffort")
if abandon and "RestoreObjectIsolationModeBestEffort" in abandon:
    errors.append("destroyed-document abandon must not restore owner OBJECTISOLATIONMODE through the foreign active host context")
if abandon and "_objectIsolationModeBefore = null;" not in abandon:
    errors.append("destroyed-document abandon must explicitly discard OBJECTISOLATIONMODE cleanup debt")

restore_mode = method_body(
    "private bool TryRestoreObjectIsolationModeBestEffort",
    "public void Dispose")
if restore_mode:
    if "IsOwnerDocumentActive" not in restore_mode:
        errors.append("OBJECTISOLATIONMODE restoration must refuse to mutate when the owner document is inactive")
    if "Application.SetSystemVariable(\"OBJECTISOLATIONMODE\"" not in restore_mode:
        errors.append("expected encapsulated OBJECTISOLATIONMODE restoration is missing")

# The owner-session may read/write this per-document host variable only inside Isolate
# and the guarded restoration helper. Any extra call site re-opens an A->B mutation path.
get_count = text.count('Application.GetSystemVariable("OBJECTISOLATIONMODE")')
set_count = text.count('Application.SetSystemVariable("OBJECTISOLATIONMODE"')
if get_count != 1:
    errors.append("OBJECTISOLATIONMODE must have exactly one captured read site; found %d" % get_count)
if set_count != 2:
    errors.append("OBJECTISOLATIONMODE must have exactly two write sites (isolate + guarded restore); found %d" % set_count)

print("QS3D coordination review document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: coordination review cleans owner transient state before MDI deactivation, never restores owner isolation mode through a foreign active document, disables CAD actions while inactive, and abandons destroyed-document mode debt without cross-document publication.")
