#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    if not TARGET.exists():
        return fail("DocumentBoundWindowLifetime.cs is missing")

    text = TARGET.read_text(encoding="utf-8")
    required = [
        "ConditionalWeakTable<Window, Registration>",
        "Registrations.GetValue(window, key => new Registration(key, document))",
        "registration.Attach(document);",
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "if (!MatchesNativeDatabase(document))",
        "if (_attached) return;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "_window.Closed += OnWindowClosed;",
        "_window.Closed -= OnWindowClosed;",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        return fail("modeless lifetime idempotence invariant is incomplete: " + ", ".join(missing))

    for legacy in (
        "ReferenceEquals(e.Document, _document)",
        "ReferenceEquals(document, _document)",
    ):
        if legacy in text:
            return fail("modeless lifetime idempotence must not depend on managed Document wrapper identity: " + legacy)

    if "new Registration(window, document).Attach();" in text:
        return fail("Attach still creates an untracked Registration on every call")

    print("PASS: modeless windows use one native-document lifetime registration per Window instance and remain idempotent across managed-wrapper drift.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
