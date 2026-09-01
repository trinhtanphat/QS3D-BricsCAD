#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallHubCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str) -> None:
    if token not in text:
        errors.append("missing Curtain Wall Hub single-instance token: " + token)


def forbid(token: str) -> None:
    if token in text:
        errors.append("forbidden Curtain Wall Hub lifecycle token remains: " + token)


for token in (
    "private static CurtainWallWindow? _window;",
    "private static Document? _document;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "private static CurtainWallWindow? _pendingWindow;",
    "private static Document? _pendingDocument;",
    "private static IntPtr _pendingNativeDatabaseIdentity;",
    "if (!PreparePublishedWindow(document, nativeDatabaseIdentity))",
    "candidate = new CurtainWallWindow(document);",
    "var ownedCandidate = candidate;",
    "candidate.Closed += (_, __) => ReleaseOwnedWindow(ownedCandidate);",
    "if (!ReservePendingWindow(candidate, document, nativeDatabaseIdentity))",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "if (!candidate.IsLoaded)",
    "if (!PromotePendingWindow(candidate, document, nativeDatabaseIdentity))",
    "_pendingWindow = candidate;",
    "_pendingDocument = document;",
    "_pendingNativeDatabaseIdentity = nativeDatabaseIdentity;",
    "_window = candidate;",
    "_document = document;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
    "if (ReferenceEquals(_pendingWindow, window))",
    "if (!ReferenceEquals(_window, window)) return;",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
):
    require(token)

for token in (
    "_window = new CurtainWallWindow(document);",
    "candidate.Closed += (_, __) => ReleaseOwnedWindow(candidate);",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);\n                if (!candidate.IsLoaded) return;",
):
    forbid(token)

prepare_start = text.find("private static bool PreparePublishedWindow")
reserve_start = text.find("private static bool ReservePendingWindow", prepare_start + 1)
prepare = text[prepare_start:reserve_start] if prepare_start >= 0 and reserve_start > prepare_start else ""
exact_owner_pos = prepare.find(
    "_nativeDatabaseIdentity == requestedNativeDatabaseIdentity && ReferenceEquals(_document, requestedDocument)"
)
close_pos = prepare.find("published.Close();", exact_owner_pos + 1)
loaded_after_close_pos = prepare.find("if (published.IsLoaded)", close_pos + 1)
release_after_close_pos = prepare.find("ReleaseOwnedWindow(published);", loaded_after_close_pos + 1)
if min(exact_owner_pos, close_pos, loaded_after_close_pos, release_after_close_pos) < 0 or not (
    exact_owner_pos < close_pos < loaded_after_close_pos < release_after_close_pos
):
    errors.append("Curtain Wall Hub must require exact published owner and terminal close before replacement release")

show_start = text.find("public void ShowCurtainWallHub()")
prepare_method_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_method_start] if show_start >= 0 and prepare_method_start > show_start else ""
construct_pos = show.find("candidate = new CurtainWallWindow(document);")
owner_pos = show.find("var ownedCandidate = candidate;", construct_pos + 1)
closed_pos = show.find("candidate.Closed += (_, __) => ReleaseOwnedWindow(ownedCandidate);", owner_pos + 1)
reserve_pos = show.find("if (!ReservePendingWindow(candidate, document, nativeDatabaseIdentity))", closed_pos + 1)
show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", reserve_pos + 1)
loaded_pos = show.find("if (!candidate.IsLoaded)", show_pos + 1)
promote_pos = show.find("if (!PromotePendingWindow(candidate, document, nativeDatabaseIdentity))", loaded_pos + 1)
if min(construct_pos, owner_pos, closed_pos, reserve_pos, show_pos, loaded_pos, promote_pos) < 0 or not (
    construct_pos < owner_pos < closed_pos < reserve_pos < show_pos < loaded_pos < promote_pos
):
    errors.append("Curtain Wall Hub must construct -> pin stable Closed owner -> attach Closed -> reserve pending -> host show -> confirm loaded -> promote")

reserve_method_start = text.find("private static bool ReservePendingWindow")
promote_method_start = text.find("private static bool PromotePendingWindow", reserve_method_start + 1)
reserve = text[reserve_method_start:promote_method_start] if reserve_method_start >= 0 and promote_method_start > reserve_method_start else ""
if reserve.find("if (_pendingWindow != null)") < 0 or reserve.find("_pendingWindow = candidate;") < 0:
    errors.append("pending reservation must reject an existing in-flight owner before assigning the candidate")

promote_method_end = text.find("private static void ReleaseOwnedWindow", promote_method_start + 1)
promote = text[promote_method_start:promote_method_end] if promote_method_start >= 0 and promote_method_end > promote_method_start else ""
owner_check = promote.find("!ReferenceEquals(_pendingWindow, candidate)")
clear_pending = promote.find("_pendingWindow = null;", owner_check + 1)
publish = promote.find("_window = candidate;", clear_pending + 1)
if min(owner_check, clear_pending, publish) < 0 or not (owner_check < clear_pending < publish):
    errors.append("publication must verify exact pending owner, clear pending state, then publish")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Curtain Wall Hub has stable exact Closed ownership, pending-first reentrancy ownership, terminal-close veto safety, and exact-owner cleanup.")
