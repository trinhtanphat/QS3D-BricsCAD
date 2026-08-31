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
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!PreparePublishedWindow(document, nativeDatabaseIdentity))",
    "_nativeDatabaseIdentity == requestedNativeDatabaseIdentity && ReferenceEquals(_document, requestedDocument)",
    "published.Close();",
    "if (published.IsLoaded)",
    "var window = new CurtainWallWindow(document);",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "_document = document;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
    "if (!ReferenceEquals(_window, window)) return;",
    "_document = null;",
    "_nativeDatabaseIdentity = IntPtr.Zero;",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
):
    require(token)

for token in (
    "var window = new CurtainWallWindow(document);\n                Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "_window = new CurtainWallWindow(document);",
):
    forbid(token)

prepare_start = text.find("private static bool PreparePublishedWindow")
release_start = text.find("private static void ReleasePublishedWindow", prepare_start + 1)
prepare = text[prepare_start:release_start] if prepare_start >= 0 and release_start > prepare_start else ""
exact_owner_pos = prepare.find(
    "_nativeDatabaseIdentity == requestedNativeDatabaseIdentity && ReferenceEquals(_document, requestedDocument)"
)
close_pos = prepare.find("published.Close();", exact_owner_pos + 1)
loaded_after_close_pos = prepare.find("if (published.IsLoaded)", close_pos + 1)
release_after_close_pos = prepare.find("ReleasePublishedWindow(published);", loaded_after_close_pos + 1)
if min(exact_owner_pos, close_pos, loaded_after_close_pos, release_after_close_pos) < 0:
    errors.append("unable to prove Curtain Wall Hub exact-owner/terminal-close ordering")
elif not (exact_owner_pos < close_pos < loaded_after_close_pos < release_after_close_pos):
    errors.append(
        "Curtain Wall Hub must reuse only exact native+wrapper owner, then require terminal unload before replacement release"
    )

show_start = text.find("public void ShowCurtainWallHub()")
prepare_method_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_method_start] if show_start >= 0 and prepare_method_start > show_start else ""
construct_pos = show.find("var window = new CurtainWallWindow(document);")
closed_pos = show.find("window.Closed += (_, __) => ReleasePublishedWindow(window);", construct_pos + 1)
show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);", closed_pos + 1)
loaded_pos = show.find("if (!window.IsLoaded) return;", show_pos + 1)
publish_window_pos = show.find("_window = window;", loaded_pos + 1)
publish_document_pos = show.find("_document = document;", publish_window_pos + 1)
publish_identity_pos = show.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_document_pos + 1)
if min(
    construct_pos,
    closed_pos,
    show_pos,
    loaded_pos,
    publish_window_pos,
    publish_document_pos,
    publish_identity_pos,
) < 0:
    errors.append("unable to prove Curtain Wall Hub window publication ordering")
elif not (
    construct_pos
    < closed_pos
    < show_pos
    < loaded_pos
    < publish_window_pos
    < publish_document_pos
    < publish_identity_pos
):
    errors.append(
        "Curtain Wall Hub must construct -> attach exact Closed owner -> show -> confirm loaded -> publish window -> wrapper -> native identity"
    )

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: Curtain Wall Hub reuses only the exact native+managed document owner, treats wrapper drift/cross-DWG as replacement, requires terminal close, and cannot publish a duplicate after close veto/failure."
)
