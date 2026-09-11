#!/usr/bin/env python3
from pathlib import Path
import sys

SOURCE = Path("src/QS3D.BricsCAD.V25/ScheduleHubCommands.cs")
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str) -> None:
    if token not in text:
        errors.append(f"missing Schedule Hub single-instance token: {token}")


def forbid(token: str) -> None:
    if token in text:
        errors.append(f"forbidden Schedule Hub lifecycle token remains: {token}")


for token in (
    "private static PublishedManager? _pending;",
    "private static PublishedManager? _published;",
    "PublishedManager? owner = null;",
    "nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!PreparePublishedWindow(document, nativeDatabaseIdentity))",
    "owner = new PublishedManager(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "published.NativeDatabaseIdentity == requestedNativeDatabaseIdentity",
    "ReferenceEquals(published.Document, requestedDocument)",
    "try { published.Window.Close(); }",
    "if (published.Window.IsLoaded) return false;",
    "ReleaseOwnedWindow(published);",
    "if (ReferenceEquals(_pending, owner)) _pending = null;",
    "if (ReferenceEquals(_published, owner)) _published = null;",
    "database.UnmanagedObject",
    "if (nativeDatabaseIdentity == IntPtr.Zero) return false;",
):
    require(token)

for token in (
    "private static ScheduleHubWindow? _window;",
    "private static Document? _document;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "_window = window;",
    "_document = document;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
):
    forbid(token)

prepare_start = text.find("private static bool PreparePublishedWindow")
close_pending_start = text.find("private static void ClosePendingOnFailure", prepare_start + 1)
prepare = text[prepare_start:close_pending_start] if prepare_start >= 0 and close_pending_start > prepare_start else ""
exact_native = prepare.find("published.NativeDatabaseIdentity == requestedNativeDatabaseIdentity")
exact_wrapper = prepare.find("ReferenceEquals(published.Document, requestedDocument)", exact_native + 1)
close_pos = prepare.find("published.Window.Close();", exact_wrapper + 1)
loaded_pos = prepare.find("if (published.Window.IsLoaded) return false;", close_pos + 1)
release_pos = prepare.find("ReleaseOwnedWindow(published);", loaded_pos + 1)
if min(exact_native, exact_wrapper, close_pos, loaded_pos, release_pos) < 0:
    errors.append("unable to prove Schedule Hub exact-owner/terminal-close ordering")
elif not (exact_native < exact_wrapper < close_pos < loaded_pos < release_pos):
    errors.append(
        "Schedule Hub must reuse exact native+managed owner, then require terminal unload before release"
    )

show_start = text.find("public void ShowScheduleHub()")
prepare_method_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_method_start] if show_start >= 0 and prepare_method_start > show_start else ""
construct_pos = show.find("var window = new ScheduleHubWindow(document);")
owner_pos = show.find("owner = new PublishedManager(window, document, nativeDatabaseIdentity);", construct_pos + 1)
release_owner_pos = show.find("var releaseOwner = owner;", owner_pos + 1)
closed_pos = show.find("window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);", release_owner_pos + 1)
pending_pos = show.find("_pending = owner;", closed_pos + 1)
pre_show_fence = show.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", pending_pos + 1)
show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pre_show_fence + 1)
post_show_fence = show.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show_pos + 1)
loaded_pos = show.find("if (!window.IsLoaded)", post_show_fence + 1)
pending_owner_pos = show.find("if (!ReferenceEquals(_pending, owner))", loaded_pos + 1)
clear_pending_pos = show.find("_pending = null;", pending_owner_pos + 1)
publish_pos = show.find("_published = owner;", clear_pending_pos + 1)
if min(
    construct_pos,
    owner_pos,
    release_owner_pos,
    closed_pos,
    pending_pos,
    pre_show_fence,
    show_pos,
    post_show_fence,
    loaded_pos,
    pending_owner_pos,
    clear_pending_pos,
    publish_pos,
) < 0:
    errors.append("unable to prove Schedule Hub pending-first publication ordering")
elif not (
    construct_pos
    < owner_pos
    < release_owner_pos
    < closed_pos
    < pending_pos
    < pre_show_fence
    < show_pos
    < post_show_fence
    < loaded_pos
    < pending_owner_pos
    < clear_pending_pos
    < publish_pos
):
    errors.append(
        "Schedule Hub must bind owner -> attach Closed -> publish pending -> fence/show/refence -> loaded/exact-pending -> publish"
    )

cleanup_start = text.find("private static void ClosePendingOnFailure")
release_start = text.find("private static void ReleaseOwnedWindow", cleanup_start + 1)
cleanup = text[cleanup_start:release_start] if cleanup_start >= 0 and release_start > cleanup_start else ""
close_cleanup = cleanup.find("owner.Window.Close();")
terminal_cleanup = cleanup.find("if (!owner.Window.IsLoaded) ReleaseOwnedWindow(owner);", close_cleanup + 1)
if min(close_cleanup, terminal_cleanup) < 0 or close_cleanup >= terminal_cleanup:
    errors.append(
        "Schedule Hub failure cleanup must retain ownership unless close reaches terminal unload"
    )

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: Schedule Hub uses pending/published generation ownership, exact native+managed reuse, terminal-close replacement, pending-first publication, and veto/failure-safe cleanup."
)
