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
        errors.append("forbidden Door/Opening Schedule publication shape remains: " + token)


for token in (
    "private static DoorOpeningScheduleWindow? _window;",
    "private static Document? _document;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "DoorOpeningScheduleWindow? candidate = null;",
    "GetNativeDatabaseIdentity(document)",
    "PreparePublishedWindow(document, nativeDatabaseIdentity)",
    "ReferenceEquals(_document, requestedDocument)",
    "published.Close();",
    "if (published.IsLoaded)",
    "candidate = new DoorOpeningScheduleWindow(document);",
    "var window = candidate;",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "_document = document;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
    "candidate = null;",
    "finally",
    "if (candidate != null) TryCloseUnpublishedWindow(candidate);",
    "if (!ReferenceEquals(_window, window)) return;",
    "if (ReferenceEquals(_window, window)) return;",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
):
    require(token)

for token in (
    "ShowModelessWindow(IntPtr.Zero, new DoorOpeningScheduleWindow(document)",
    "_window = new DoorOpeningScheduleWindow(document)",
):
    forbid(token)

show_start = text.find("public void ShowDoorOpeningSchedule()")
prepare_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_start] if show_start >= 0 and prepare_start > show_start else ""
publish_pos = show.find("_window = window;")
transfer_pos = show.find("candidate = null;", publish_pos + 1) if publish_pos >= 0 else -1
positions = [
    show.find("DoorOpeningScheduleWindow? candidate = null;"),
    show.find("candidate = new DoorOpeningScheduleWindow(document);"),
    show.find("var window = candidate;"),
    show.find("window.Closed += (_, __) => ReleasePublishedWindow(window);"),
    show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);"),
    show.find("if (!window.IsLoaded) return;"),
    publish_pos,
    show.find("_document = document;", publish_pos + 1),
    show.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_pos + 1),
    transfer_pos,
    show.find("finally"),
    show.find("if (candidate != null) TryCloseUnpublishedWindow(candidate);"),
]
if min(positions) < 0:
    errors.append("unable to prove Door/Opening Schedule show/load/publication/cleanup ordering")
elif positions != sorted(positions):
    errors.append("Door/Opening Schedule candidate must stay cleanup-owned through show/load and transfer only after full publication")

prepare = text[prepare_start:] if prepare_start >= 0 else ""
close_pos = prepare.find("published.Close();")
post_close_loaded_pos = prepare.find("if (published.IsLoaded)", close_pos + 1)
release_pos = prepare.find("ReleasePublishedWindow(published);", post_close_loaded_pos + 1)
if min(close_pos, post_close_loaded_pos, release_pos) < 0 or not (close_pos < post_close_loaded_pos < release_pos):
    errors.append("replacement must terminal-close before releasing the published owner")

release_start = text.find("private static void ReleasePublishedWindow", prepare_start + 1)
cleanup_start = text.find("private static void TryCloseUnpublishedWindow", release_start + 1)
release = text[release_start:cleanup_start] if release_start >= 0 and cleanup_start > release_start else ""
if release.find("if (!ReferenceEquals(_window, window)) return;") < 0:
    errors.append("published release must be exact-owner guarded")
cleanup_end = text.find("private static IntPtr GetNativeDatabaseIdentity", cleanup_start + 1)
cleanup = text[cleanup_start:cleanup_end] if cleanup_start >= 0 and cleanup_end > cleanup_start else ""
refuse_pos = cleanup.find("if (ReferenceEquals(_window, window)) return;")
close_unpublished_pos = cleanup.find("window.Close();", refuse_pos + 1)
if refuse_pos < 0 or close_unpublished_pos < 0 or refuse_pos >= close_unpublished_pos:
    errors.append("unpublished cleanup must refuse the authoritative owner before close")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Door/Opening Schedule has exact-document/native-DB single-owner publication, terminal replacement, and failure-clean unpublished candidate ownership.")
