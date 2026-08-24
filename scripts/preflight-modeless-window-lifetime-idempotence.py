#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
TARGET = UI / "DocumentBoundWindowLifetime.cs"
NATIVE = UI / "DocumentBoundNativeLifecycleCoordinator.cs"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    if not TARGET.exists():
        return fail("DocumentBoundWindowLifetime.cs is missing")
    if not NATIVE.exists():
        return fail("DocumentBoundNativeLifecycleCoordinator.cs is missing")

    text = TARGET.read_text(encoding="utf-8")
    native = NATIVE.read_text(encoding="utf-8")

    required = [
        "ConditionalWeakTable<Window, Registration>",
        "Registrations.GetValue(window, key => new Registration(key, document))",
        "registration.Attach(document);",
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "if (!MatchesNativeDatabase(document))",
        "if (_attached) return;",
        "private IDisposable? _nativeLifecycleSubscription;",
        "_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(",
        "DetachNativeLifecycleSubscription();",
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

    for forbidden in (
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    ):
        if forbidden in text:
            return fail("per-window lifetime must not directly own native lifecycle reactors: " + forbidden)

    native_required = [
        "private static readonly Dictionary<IntPtr, Entry> Entries",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
        "new WeakReference<Callbacks>(callbacks)",
        "return new Subscription(entry, callbacks);",
    ]
    native_missing = [needle for needle in native_required if needle not in native]
    if native_missing:
        return fail("shared native lifecycle ownership invariant is incomplete: " + ", ".join(native_missing))

    if native.count("BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;") != 1:
        return fail("shared coordinator must own exactly one global DocumentToBeDestroyed subscription site")

    if "new Registration(window, document).Attach();" in text:
        return fail("Attach still creates an untracked Registration on every call")

    print("PASS: each modeless Window owns one managed subscription token, while native document reactors are centralized by native database identity with weak per-window callbacks and remain idempotent across managed-wrapper drift.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
