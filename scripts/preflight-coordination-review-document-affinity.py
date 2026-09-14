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
    "private bool IsOwnerDocumentGenerationActive",
    "var ownerActive = IsOwnerDocumentGenerationActive;",
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
    for needle in [
        "IsOwnerDocumentGenerationActive",
        "_session.HasTransientState",
        "if (!_session.IsOwnerNativeGenerationCurrent)",
        "AbandonStaleGenerationIfNeeded();",
        "UpdateActionState();",
    ]:
        if needle not in activated:
            errors.append("DocumentActivated handling missing generation-aware affinity token: " + needle)
    stale_at = activated.find("if (!_session.IsOwnerNativeGenerationCurrent)")
    foreign_at = activated.find("_cleanupBarrier = _session.HasTransientState;", stale_at)
    if stale_at < 0 or foreign_at < stale_at:
        errors.append("DocumentActivated must distinguish native-generation replacement before ordinary foreign-MDI handling")

abandon = method_body(
    "public void AbandonDestroyedDocumentState",
    "private bool TryRestoreImpliedSelectionBestEffort")
if abandon and "RestoreObjectIsolationModeBestEffort" in abandon:
    errors.append("destroyed-document abandon must not restore owner OBJECTISOLATIONMODE through a foreign active host context")
for needle in ["_objectIsolationModeBefore = null;", "_impliedSelectionBeforeIsolation = null;"]:
    if abandon and needle not in abandon:
        errors.append("destroyed-document abandon must explicitly discard terminal cleanup debt: " + needle)

restore_mode = method_body(
    "private bool TryRestoreObjectIsolationModeBestEffort",
    "public void Dispose")
if restore_mode:
    if "IsOwnerGenerationActive" not in restore_mode:
        errors.append("OBJECTISOLATIONMODE restoration must require current native generation and active owner")
    if "Application.SetSystemVariable(\"OBJECTISOLATIONMODE\"" not in restore_mode:
        errors.append("expected encapsulated OBJECTISOLATIONMODE restoration is missing")

# Application-level OBJECTISOLATIONMODE may only be captured by Isolate and written
# by the launch path plus the generation-aware guarded restoration helper.
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
print("PASS: coordination review keeps MDI activity separate from native-generation identity, retries same-generation cleanup only on the owner, and abandons terminal generation debt without cross-document publication.")
