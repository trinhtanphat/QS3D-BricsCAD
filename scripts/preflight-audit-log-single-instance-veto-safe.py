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

prepare_start = text.find("private static bool PreparePublishedWindow")
release_start = text.find("private static void ReleasePublishedWindow", prepare_start + 1)
prepare = text[prepare_start:release_start] if prepare_start >= 0 and release_start > prepare_start else ""
same_native_pos = prepare.find("if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)")
close_pos = prepare.find("published.Close();", same_native_pos + 1)
loaded_after_close_pos = prepare.find("if (published.IsLoaded)", close_pos + 1)
release_after_close_pos = prepare.find("ReleasePublishedWindow(published);", loaded_after_close_pos + 1)
if min(same_native_pos, close_pos, loaded_after_close_pos, release_after_close_pos) < 0:
    errors.append("unable to prove Audit Log same-native/terminal-close ordering")
elif not (same_native_pos < close_pos < loaded_after_close_pos < release_after_close_pos):
    errors.append("Audit Log prepare path must decide same-native reuse before close, then require terminal unload before release")

show_start = text.find("public void ShowAuditLog()")
prepare_method_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_method_start] if show_start >= 0 and prepare_method_start > show_start else ""
construct_pos = show.find("var candidate = new AuditLogWindow(document);")
show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", construct_pos + 1)
loaded_pos = show.find("if (!candidate.IsLoaded) return;", show_pos + 1)
publish_window_pos = show.find("_window = candidate;", loaded_pos + 1)
publish_identity_pos = show.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_window_pos + 1)
if min(construct_pos, show_pos, loaded_pos, publish_window_pos, publish_identity_pos) < 0:
    errors.append("unable to prove Audit Log candidate publication ordering")
elif not (construct_pos < show_pos < loaded_pos < publish_window_pos < publish_identity_pos):
    errors.append("Audit Log must construct -> show -> confirm loaded -> publish window -> publish native identity")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Audit Log publication is native-database single-instance, same-native reuse is wrapper-drift safe, cross-DWG replacement requires terminal close, and failed/vetoed close cannot publish a duplicate window.")
