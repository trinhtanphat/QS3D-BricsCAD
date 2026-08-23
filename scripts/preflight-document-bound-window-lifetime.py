#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DocumentBoundWindowLifetime.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "var identity = database.UnmanagedObject;",
        "identity == IntPtr.Zero",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "if (!MatchesNativeDatabase(document))",
        "if (!MatchesNativeDatabase(e.Document)) return;",
        "Interlocked.Exchange(ref _invalidated, 1)",
        "DetachDocumentManagerHandler();",
        "TryCloseWindow();",
    )
    for token in required:
        if token not in text:
            errors.append("native document lifetime guard drift; missing token: " + token)

    forbidden = (
        "ReferenceEquals(e.Document, _document)",
        "ReferenceEquals(document, _document)",
    )
    for token in forbidden:
        if token in text:
            errors.append("managed Document wrapper identity must not own modeless lifetime: " + token)

    helper_start = text.find("private bool MatchesNativeDatabase(Document document)")
    helper_end = text.find("private void BindProjectAffinityIfPresent()", helper_start)
    close_start = text.find("private void OnDocumentToBeDestroyed")
    close_end = text.find("private void TryCloseWindow()", close_start)
    if min(helper_start, helper_end, close_start, close_end) < 0:
        errors.append("cannot isolate native document identity/lifetime helpers")
    else:
        helper = text[helper_start:helper_end]
        close = text[close_start:close_end]
        if "database.UnmanagedObject != IntPtr.Zero" not in helper or "database.UnmanagedObject == _nativeDatabaseIdentity" not in helper:
            errors.append("same-native/different-wrapper positive match and different-database negative match must use the captured native pointer")
        close_positions = (
            close.find("if (!MatchesNativeDatabase(e.Document)) return;"),
            close.find("Interlocked.Exchange(ref _invalidated, 1)"),
            close.find("DetachDocumentManagerHandler();"),
            close.find("TryCloseWindow();"),
        )
        if min(close_positions) < 0 or tuple(sorted(close_positions)) != close_positions:
            errors.append("document destruction must validate native identity, invalidate once, detach global handler, then close")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: modeless window lifetime is keyed to the stable native database pointer, accepts managed wrapper drift, rejects different databases, and remains close-once.")
