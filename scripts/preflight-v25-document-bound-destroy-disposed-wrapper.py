#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/DocumentBoundNativeLifecycleCoordinator.cs"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + ": missing " + token)


def forbid(text, token, label):
    if token in text:
        errors.append(label + ": forbidden " + token)


if not SOURCE.is_file():
    errors.append("missing document-bound lifecycle source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

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

# Pin the safety ordering inside the destroy handler itself: exact lifecycle reference lookup must
# happen before any IsDisposed/Database access. This prevents a future refactor from keeping the
# helper names while reintroducing the fail-open early return.
handler_start = source.find("private static void OnDocumentToBeDestroyed")
handler_end = source.find("private static void Unregister", handler_start + 1)
handler = source[handler_start:handler_end] if handler_start >= 0 and handler_end > handler_start else ""
if not handler:
    errors.append("destroy handler block not found")
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

print("PASS: document-bound modeless destroy cleanup is reference-first, disposed-wrapper safe, and wrapper-drift compatible.")
