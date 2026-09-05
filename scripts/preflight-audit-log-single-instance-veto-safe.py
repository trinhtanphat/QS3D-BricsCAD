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
    "private static AuditLogWindow? _unpublishedCandidate;",
    "private static AuditLogWindow? _publicationInFlightCandidate;",
    "private static AuditLogWindow? _cleanupInFlightCandidate;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!PrepareUnpublishedCandidate())",
    "if (!PreparePublishedWindow(nativeDatabaseIdentity))",
    "if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)",
    "published.Close();",
    "if (published.IsLoaded)",
    "candidate.Closed += (_, __) => ReleaseCandidate(candidate);",
    "_unpublishedCandidate = candidate;",
    "_publicationInFlightCandidate = candidate;",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "if (!candidate.IsLoaded)",
    "if (candidate.IsLoaded)",
    "_window = candidate;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
    "if (ReferenceEquals(_window, candidate))",
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
    "candidate.Closed += (_, __) => ReleasePublishedWindow(candidate);",
):
    forbid(token)

# Reentry must fail closed before inspecting/closing singleton candidates while
# either native publication or terminal cleanup is in flight.
unpublished_start = text.find("private static bool PrepareUnpublishedCandidate")
published_start = text.find("private static bool PreparePublishedWindow", unpublished_start + 1)
unpublished = text[unpublished_start:published_start] if unpublished_start >= 0 and published_start > unpublished_start else ""
cleanup_guard_pos = unpublished.find("if (_cleanupInFlightCandidate != null)")
publication_guard_pos = unpublished.find("if (_publicationInFlightCandidate != null)")
unpublished_read_pos = unpublished.find("var candidate = _unpublishedCandidate;")
if min(cleanup_guard_pos, publication_guard_pos, unpublished_read_pos) < 0:
    errors.append("unable to prove Audit Log unpublished reentrancy guards")
elif not (cleanup_guard_pos < publication_guard_pos < unpublished_read_pos):
    errors.append("Audit Log must reject cleanup/publication reentry before reading unpublished singleton state")

# Cross-DWG replacement must reserve exact cleanup ownership before Close,
# accept only proven terminal closure, and release singleton authority exactly.
published_end = text.find("private static bool CloseUnpublishedCandidate", published_start + 1)
published = text[published_start:published_end] if published_start >= 0 and published_end > published_start else ""
same_native_pos = published.find("if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)")
cleanup_set_pos = published.find("_cleanupInFlightCandidate = published;", same_native_pos + 1)
close_pos = published.find("published.Close();", cleanup_set_pos + 1)
catch_pos = published.find("catch", close_pos + 1)
terminal_in_catch_pos = published.find("if (!published.IsLoaded)", catch_pos + 1)
release_in_catch_pos = published.find("ReleaseCandidate(published);", terminal_in_catch_pos + 1)
finally_pos = published.find("finally", catch_pos + 1)
cleanup_clear_pos = published.find("ReferenceEquals(_cleanupInFlightCandidate, published)", finally_pos + 1)
loaded_after_close_pos = published.find("if (published.IsLoaded)", finally_pos + 1)
release_after_close_pos = published.find("ReleaseCandidate(published);", loaded_after_close_pos + 1)
if min(
    same_native_pos,
    cleanup_set_pos,
    close_pos,
    catch_pos,
    terminal_in_catch_pos,
    release_in_catch_pos,
    finally_pos,
    cleanup_clear_pos,
    loaded_after_close_pos,
    release_after_close_pos,
) < 0:
    errors.append("unable to prove Audit Log same-native/terminal-close cleanup ordering")
elif not (
    same_native_pos
    < cleanup_set_pos
    < close_pos
    < catch_pos
    < terminal_in_catch_pos
    < release_in_catch_pos
    < finally_pos
    < cleanup_clear_pos
    < loaded_after_close_pos
    < release_after_close_pos
):
    errors.append(
        "Audit Log prepare path must decide same-native reuse -> reserve cleanup -> close -> reconcile terminal catch -> unwind cleanup reservation -> require terminal unload -> release"
    )

# Publication authority must be reserved before native ShowModelessWindow can
# reenter and may move to _window only after an explicit loaded confirmation.
show_start = text.find("public void ShowAuditLog()")
unpublished_method_start = text.find("private static bool PrepareUnpublishedCandidate", show_start + 1)
show = text[show_start:unpublished_method_start] if show_start >= 0 and unpublished_method_start > show_start else ""
construct_pos = show.find("var candidate = new AuditLogWindow(document);")
closed_pos = show.find("candidate.Closed += (_, __) => ReleaseCandidate(candidate);", construct_pos + 1)
unpublished_reserve_pos = show.find("_unpublishedCandidate = candidate;", closed_pos + 1)
inflight_reserve_pos = show.find("_publicationInFlightCandidate = candidate;", unpublished_reserve_pos + 1)
show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", inflight_reserve_pos + 1)
loaded_reject_pos = show.find("if (!candidate.IsLoaded)", show_pos + 1)
loaded_publish_pos = show.find("if (candidate.IsLoaded)", loaded_reject_pos + 1)
publish_window_pos = show.find("_window = candidate;", loaded_publish_pos + 1)
publish_identity_pos = show.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_window_pos + 1)
if min(
    construct_pos,
    closed_pos,
    unpublished_reserve_pos,
    inflight_reserve_pos,
    show_pos,
    loaded_reject_pos,
    loaded_publish_pos,
    publish_window_pos,
    publish_identity_pos,
) < 0:
    errors.append("unable to prove Audit Log atomic candidate publication ordering")
elif not (
    construct_pos
    < closed_pos
    < unpublished_reserve_pos
    < inflight_reserve_pos
    < show_pos
    < loaded_reject_pos
    < loaded_publish_pos
    < publish_window_pos
    < publish_identity_pos
):
    errors.append(
        "Audit Log must construct -> attach Closed -> reserve unpublished -> reserve publication-in-flight -> show -> reject non-loaded -> confirm loaded -> publish window -> publish native identity"
    )

# ReleaseCandidate may clear published/unpublished singleton ownership, but the
# separate publication/cleanup in-flight reservations must survive synchronous
# Closed reentrancy until their owning stack frames unwind.
release_start = text.find("private static void ReleaseCandidate")
release_end = text.find("private static IntPtr GetNativeDatabaseIdentity", release_start + 1)
release = text[release_start:release_end] if release_start >= 0 and release_end > release_start else ""
if not release:
    errors.append("unable to bound exact Audit Log candidate release helper")
else:
    if "_publicationInFlightCandidate" in release:
        errors.append("Closed release must not clear publication-in-flight reservation")
    if "_cleanupInFlightCandidate" in release:
        errors.append("Closed release must not clear cleanup-in-flight reservation")
    if "ReferenceEquals(_window, candidate)" not in release:
        errors.append("published singleton release must remain exact-candidate identity bound")
    if "ReferenceEquals(_unpublishedCandidate, candidate)" not in release:
        errors.append("unpublished singleton release must remain exact-candidate identity bound")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Audit Log publication is native-database single-instance, publication/cleanup reservations are reentrancy-safe, cross-DWG replacement requires terminal close, and failed/vetoed close cannot publish a duplicate window.")
