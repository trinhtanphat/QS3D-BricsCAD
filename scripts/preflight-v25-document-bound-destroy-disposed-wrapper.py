#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/UI/DocumentBoundNativeLifecycleCoordinator.cs"
WINDOW_LIFETIME = ROOT / "src/QS3D.BricsCAD.V25/UI/DocumentBoundWindowLifetime.cs"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + ": missing " + token)


def forbid(text, token, label):
    if token in text:
        errors.append(label + ": forbidden " + token)


def read_source(path, label):
    if not path.is_file():
        errors.append("missing " + label + ": " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read_source(COORDINATOR, "document-bound lifecycle coordinator")
window_source = read_source(WINDOW_LIFETIME, "document-bound window lifetime")

# DocumentToBeDestroyed can surface the exact registered managed Document after its native wrapper
# has already entered disposed state. Reference identity is managed-only and must therefore be tried
# before IsDisposed/Database dereference. Native database identity remains the wrapper-drift fallback.
for token, label in (
    ("TrySnapshotDestroyByLifecycleDocument", "destroy path must provide reference-first lookup"),
    ("ReferenceEquals(candidate.LifecycleDocument, document)", "reference-first lookup must use managed wrapper identity"),
    ("TrySnapshotDestroyByNativeIdentity", "destroy path must retain native-identity fallback"),
    ("if (document.IsDisposed) return;", "disposed wrapper must block native fallback dereference"),
    ("entry.MarkCloseStarted();", "matched entry must enter close-started state"),
    ("callbacks = entry.SnapshotLiveCallbacks();", "matched entry must snapshot live callbacks"),
    ("Entries.Remove(entry.NativeDatabaseIdentity);", "destroy cleanup must remove the exact entry key"),
    ("entry.ClearCallbacks();", "destroy cleanup must clear callbacks"),
):
    require(source, token, label)

handler_start = source.find("private static void OnDocumentToBeDestroyed")
handler_end = source.find("private static void Unregister", handler_start + 1)
handler = source[handler_start:handler_end] if handler_start >= 0 and handler_end > handler_start else ""
if not handler:
    errors.append("coordinator destroy handler block not found")
else:
    reference_lookup = handler.find("TrySnapshotDestroyByLifecycleDocument(document")
    disposed_guard = handler.find("document.IsDisposed")
    database_read = handler.find("document.Database")
    native_lookup = handler.find("TrySnapshotDestroyByNativeIdentity")
    if reference_lookup < 0 or disposed_guard < 0 or reference_lookup >= disposed_guard:
        errors.append("managed lifecycle-reference lookup must precede disposed-wrapper guard")
    if reference_lookup < 0 or database_read < 0 or reference_lookup >= database_read:
        errors.append("managed lifecycle-reference lookup must precede native Database dereference")
    if native_lookup < 0 or database_read < 0 or native_lookup <= database_read:
        errors.append("native-identity lookup must occur only after native identity is read safely")

for token, label in (
    ("if (document == null || document.IsDisposed) return;", "destroy path must not reject disposed wrapper before managed reference lookup"),
    ("identity = database.UnmanagedObject;\n                if (identity == IntPtr.Zero) return;\n            }\n            catch\n            {\n                return;\n            }\n\n            Entry? entry;", "legacy native-first destroy lookup must not return before reference matching"),
):
    forbid(source, token, label)

# The per-window registration must apply the same managed-first rule. Coordinator cleanup alone is
# insufficient: a disposed exact wrapper must still invalidate and close the window even if no
# BeginDocumentClose callback reached it first.
for token, label in (
    ("var destroyingDocument = e.Document;", "window destroy callback must capture event wrapper once"),
    ("ReferenceEquals(destroyingDocument, _lifecycleDocument)", "window destroy callback must accept exact lifecycle wrapper by managed identity"),
    ("MatchesNativeDatabase(destroyingDocument)", "window destroy callback must retain live wrapper-drift fallback"),
    ("Interlocked.Exchange(ref _invalidated, 1)", "window destroy callback must remain fail-closed"),
    ("TryCloseWindow(deferForFinalDocument);", "window destroy callback must still close the bound window"),
):
    require(window_source, token, label)

window_handler_start = window_source.find("private void OnDocumentToBeDestroyed")
window_handler_end = window_source.find("private void OnDocumentCloseAborted", window_handler_start + 1)
window_handler = window_source[window_handler_start:window_handler_end] if window_handler_start >= 0 and window_handler_end > window_handler_start else ""
if not window_handler:
    errors.append("window destroy handler block not found")
else:
    exact_match = window_handler.find("ReferenceEquals(destroyingDocument, _lifecycleDocument)")
    native_match = window_handler.find("MatchesNativeDatabase(destroyingDocument)")
    if exact_match < 0 or native_match < 0 or exact_match >= native_match:
        errors.append("window destroy callback must try managed lifecycle identity before native fallback")

for token, label in (
    ("if (!MatchesNativeDatabase(e.Document)) return;", "window destroy callback must not native-dereference exact wrapper before managed matching"),
):
    forbid(window_source, token, label)

# Preserve global quiescence and one-shot exact-entry cleanup semantics.
for token, label in (
    ("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;", "host-quiescence barrier must remain"),
    ("ReferenceEquals(current, entry)", "cleanup must verify exact entry ownership"),
):
    require(source, token, label)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: document-bound modeless destroy cleanup is reference-first, disposed-wrapper safe, window-affine, and wrapper-drift compatible.")
