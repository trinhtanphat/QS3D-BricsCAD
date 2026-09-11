#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DoorOpeningScheduleWindowCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str) -> None:
    if token not in text:
        errors.append("missing Door/Opening Schedule publication token: " + token)


def forbid(token: str) -> None:
    if token in text:
        errors.append("forbidden legacy Door/Opening Schedule publication shape remains: " + token)


for token in (
    "private static PublishedWindow? _pending;",
    "private static PublishedWindow? _published;",
    "PublishedWindow? owner = null;",
    "nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "PreparePublishedWindow(document, nativeDatabaseIdentity)",
    "var window = new DoorOpeningScheduleWindow(document);",
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "owner = null;",
    "private static void ReleaseOwnedWindow(PublishedWindow owner)",
    "if (ReferenceEquals(_pending, owner)) _pending = null;",
    "if (ReferenceEquals(_published, owner)) _published = null;",
    "database.UnmanagedObject",
    "if (nativeDatabaseIdentity == IntPtr.Zero) return false;",
):
    require(token)

for token in (
    "private static DoorOpeningScheduleWindow? _window;",
    "private static Document? _document;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "ShowModelessWindow(IntPtr.Zero, new DoorOpeningScheduleWindow(document)",
):
    forbid(token)

show_start = text.find("public void ShowDoorOpeningSchedule()")
prepare_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_start] if show_start >= 0 and prepare_start > show_start else ""

positions = [
    show.find("owner = new PublishedWindow(window, document, nativeDatabaseIdentity);"),
    show.find("var releaseOwner = owner;"),
    show.find("window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);"),
    show.find("_pending = owner;"),
    show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);"),
    show.find("if (!window.IsLoaded)"),
    show.find("if (!ReferenceEquals(_pending, owner))"),
    show.find("_pending = null;"),
    show.find("_published = owner;"),
    show.find("owner = null;", show.find("_published = owner;") + 1),
]
if min(positions) < 0 or positions != sorted(positions):
    errors.append("Door/Opening Schedule ownership must be pending-first, loaded/exact-owner proven, and only then published")

for token in (
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
):
    if show.count(token) < 2:
        errors.append("Door/Opening Schedule must revalidate exact document generation across destructive/host pumping boundaries")

prepare = text[prepare_start:] if prepare_start >= 0 else ""
close_pos = prepare.find("published.Window.Close();")
post_close_loaded_pos = prepare.find("if (published.Window.IsLoaded) return false;", close_pos + 1)
release_pos = prepare.find("ReleaseOwnedWindow(published);", post_close_loaded_pos + 1)
if min(close_pos, post_close_loaded_pos, release_pos) < 0 or not (close_pos < post_close_loaded_pos < release_pos):
    errors.append("replacement must retain published ownership until Close is confirmed terminal")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Door/Opening Schedule keeps pending-first exact document/native-DB ownership, terminal-safe replacement, and loaded/exact-owner publication.")
