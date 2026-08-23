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
        "if (!IsSameDocument(document))",
        "if (_attached) return;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "_window.Closed += OnWindowClosed;",
        "_window.Closed -= OnWindowClosed;",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        return fail("modeless lifetime idempotence invariant is incomplete: " + ", ".join(missing))

    if "new Registration(window, document).Attach();" in text:
        return fail("Attach still creates an untracked Registration on every call")

    print("PASS: modeless windows use one document lifetime registration per Window instance.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
