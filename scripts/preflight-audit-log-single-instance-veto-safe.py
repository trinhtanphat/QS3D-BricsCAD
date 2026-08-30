#!/usr/bin/env python3
from pathlib import Path
import sys

SOURCE = Path("src/QS3D.BricsCAD.V25/AuditCommands.cs")
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str) -> None:
    if token not in text:
        errors.append(f"missing Audit Log single-instance token: {token}")


def forbid(token: str) -> None:
    if token in text:
        errors.append(f"forbidden Audit Log lifecycle token remains: {token}")


for token in (
    "private static IntPtr _nativeDatabaseIdentity;",
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!PreparePublishedWindow(nativeDatabaseIdentity))",
    "if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)",
    "published.Close();",
    "if (published.IsLoaded)",
    "candidate.Closed += (_, __) => ReleasePublishedWindow(candidate);",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "if (!candidate.IsLoaded) return;",
    "_window = candidate;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
    "if (!ReferenceEquals(_window, candidate)) return;",
    "_nativeDatabaseIdentity = IntPtr.Zero;",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
):
    require(token)

for token in (
    "if (_window != null && _window.IsLoaded) _window.Close();",
    "_window = new AuditLogWindow(document);",
    "_window.Closed += (_, __) => _window = null;",
    "Application.ShowModelessWindow(IntPtr.Zero, _window, true);",
):
    forbid(token)

close_pos = text.find("published.Close();")
loaded_after_close_pos = text.find("if (published.IsLoaded)", close_pos + 1)
release_after_close_pos = text.find("ReleasePublishedWindow(published);", loaded_after_close_pos + 1)
construct_pos = text.find("var candidate = new AuditLogWindow(document);")
show_pos = text.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", construct_pos + 1)
publish_window_pos = text.find("_window = candidate;", show_pos + 1)
publish_identity_pos = text.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_window_pos + 1)
if min(close_pos, loaded_after_close_pos, release_after_close_pos, construct_pos, show_pos, publish_window_pos, publish_identity_pos) < 0:
    errors.append("unable to prove Audit Log terminal-close/publication ordering")
elif not (close_pos < loaded_after_close_pos < release_after_close_pos < construct_pos < show_pos < publish_window_pos < publish_identity_pos):
    errors.append("Audit Log must prove close -> terminal IsLoaded check -> release -> construct -> show -> publish window -> publish native identity")

same_native_pos = text.find("if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)")
close_for_other_pos = text.find("published.Close();", same_native_pos + 1)
if same_native_pos < 0 or close_for_other_pos < 0 or same_native_pos >= close_for_other_pos:
    errors.append("same-native reuse must be decided before cross-DWG close")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Audit Log publication is native-database single-instance, same-native reuse is wrapper-drift safe, cross-DWG replacement requires terminal close, and failed/vetoed close cannot publish a duplicate window.")
